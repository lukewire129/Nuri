using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Nuri.WPF.PreviewHost;

internal sealed class PreviewCaptureServer : IDisposable
{
    private readonly Window _window;
    private readonly FrameworkElement _captureTarget;
    private readonly Func<PreviewCaptureStatus> _getStatus;
    private readonly string _connectionFilePath;
    private readonly HttpListener _listener = new();
    private readonly DispatcherTimer _captureTimer;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _frameGate = new();
    private readonly string _authToken;
    private readonly int _idleUnchangedFrameThreshold;
    private byte[] _latestFrame = Array.Empty<byte>();
    private long _frameVersion;
    private int _captureStarted;
    private int _captureQueued;
    private int _encodeInFlight;
    private int _unchangedCaptureCount;
    private volatile bool _disposed;

    public PreviewCaptureServer(
        Window window,
        FrameworkElement captureTarget,
        Func<PreviewCaptureStatus> getStatus,
        string connectionFilePath,
        int framesPerSecond = 15)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _captureTarget = captureTarget ?? throw new ArgumentNullException(nameof(captureTarget));
        _getStatus = getStatus ?? throw new ArgumentNullException(nameof(getStatus));
        _connectionFilePath = connectionFilePath ?? throw new ArgumentNullException(nameof(connectionFilePath));
        _authToken = CreateToken();

        Port = FindFreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");

        framesPerSecond = Math.Clamp(framesPerSecond, 1, 30);
        _idleUnchangedFrameThreshold = Math.Max(3, framesPerSecond / 2);
        var interval = TimeSpan.FromMilliseconds(1000.0 / framesPerSecond);
        _captureTimer = new DispatcherTimer(DispatcherPriority.Background, _window.Dispatcher)
        {
            Interval = interval
        };
        _captureTimer.Tick += OnCaptureTimerTick;
    }

    public int Port { get; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _listener.Start();
        WriteConnectionFile();
        _ = ListenAsync(_shutdown.Token);
    }

    public void RequestCapture()
    {
        if (_disposed)
            return;

        Interlocked.Exchange(ref _unchangedCaptureCount, 0);
        StartCaptureLoop();
        QueueCapture();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _captureTimer.Stop();
        _shutdown.Cancel();
        lock (_frameGate)
            Monitor.PulseAll(_frameGate);

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
        }

        try
        {
            File.Delete(_connectionFilePath);
        }
        catch
        {
        }

        _shutdown.Dispose();
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string CreateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private void WriteConnectionFile()
    {
        var directory = Path.GetDirectoryName(_connectionFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(new
        {
            protocol = PreviewProtocol.Name,
            port = Port,
            token = _authToken,
            processId = Environment.ProcessId
        });
        var temporaryPath = _connectionFilePath + ".tmp";
        File.WriteAllText(temporaryPath, json, Encoding.UTF8);
        File.Move(temporaryPath, _connectionFilePath, overwrite: true);
    }

    private void OnCaptureTimerTick(object? sender, EventArgs args)
    {
        CaptureFrame();
    }

    private void StartCaptureLoop()
    {
        if (Interlocked.Exchange(ref _captureStarted, 1) != 0)
            return;

        _window.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
                return;

            _captureTimer.Start();
        });
    }

    private void QueueCapture()
    {
        if (Interlocked.Exchange(ref _captureQueued, 1) != 0)
            return;

        _window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            Interlocked.Exchange(ref _captureQueued, 0);
            if (!_disposed)
                CaptureFrame();
        });
    }

    private void CaptureFrame()
    {
        if (_disposed || Interlocked.CompareExchange(ref _encodeInFlight, 1, 0) != 0)
            return;

        try
        {
            var width = (int)Math.Ceiling(_captureTarget.ActualWidth);
            var height = (int)Math.Ceiling(_captureTarget.ActualHeight);
            if (width <= 0 || height <= 0)
            {
                Interlocked.Exchange(ref _encodeInFlight, 0);
                return;
            }

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(_captureTarget);
            bitmap.Freeze();

            _ = Task.Run(() => EncodeAndPublish(bitmap));
        }
        catch
        {
            Interlocked.Exchange(ref _encodeInFlight, 0);
            // The surface can be transiently unavailable while a new assembly is applied.
        }
    }

    private void EncodeAndPublish(BitmapSource bitmap)
    {
        try
        {
            var encoder = new JpegBitmapEncoder { QualityLevel = 80 };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            var nextFrame = stream.ToArray();
            var shouldStopCaptureLoop = false;

            lock (_frameGate)
            {
                if (_disposed)
                    return;

                if (_latestFrame.AsSpan().SequenceEqual(nextFrame))
                {
                    shouldStopCaptureLoop = Interlocked.Increment(ref _unchangedCaptureCount)
                        >= _idleUnchangedFrameThreshold;
                }
                else
                {
                    Interlocked.Exchange(ref _unchangedCaptureCount, 0);
                    _latestFrame = nextFrame;
                    _frameVersion++;
                    Monitor.PulseAll(_frameGate);
                }
            }

            if (shouldStopCaptureLoop)
                QueueStopCaptureLoopIfIdle();
        }
        catch
        {
            // Encoding can fail while the host is shutting down.
        }
        finally
        {
            Interlocked.Exchange(ref _encodeInFlight, 0);
        }
    }

    private void QueueStopCaptureLoopIfIdle()
    {
        _window.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed
                || Volatile.Read(ref _unchangedCaptureCount) < _idleUnchangedFrameThreshold)
            {
                return;
            }

            _captureTimer.Stop();
            Interlocked.Exchange(ref _captureStarted, 0);
        });
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequest(context), cancellationToken);
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (!IsAllowedHost(request.Headers["Host"]))
            {
                Close(response, 421);
                return;
            }

            var origin = request.Headers["Origin"];
            var originAllowed = string.IsNullOrWhiteSpace(origin)
                || origin.StartsWith("vscode-webview://", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(origin) && originAllowed)
            {
                response.Headers["Access-Control-Allow-Origin"] = origin;
                response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type";
                response.Headers["Access-Control-Expose-Headers"] = "X-Nuri-Frame-Version, ETag";
            }

            if (string.Equals(request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                Close(response, originAllowed ? 204 : 403);
                return;
            }

            if (!originAllowed)
            {
                Close(response, 403);
                return;
            }

            if (!IsAuthorized(request.Headers["Authorization"]))
            {
                response.Headers["WWW-Authenticate"] = "Bearer realm=\"nuri-preview\"";
                Close(response, 401);
                return;
            }

            switch (request.Url?.AbsolutePath)
            {
                case "/frame" when request.HttpMethod == "GET":
                    ServeFrame(request, response);
                    break;
                case "/status" when request.HttpMethod == "GET":
                    ServeStatus(response);
                    break;
                case "/focus" when request.HttpMethod == "POST":
                    FocusWindow(response);
                    break;
                default:
                    Close(response, 404);
                    break;
            }
        }
        catch
        {
            try
            {
                Close(response, 500);
            }
            catch
            {
            }
        }
    }

    private bool IsAllowedHost(string? host)
    {
        return string.Equals(host, $"127.0.0.1:{Port}", StringComparison.Ordinal)
            || string.Equals(host, $"localhost:{Port}", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAuthorized(string? authorization)
    {
        const string prefix = "Bearer ";
        if (authorization == null || !authorization.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var presented = Encoding.UTF8.GetBytes(authorization.Substring(prefix.Length).Trim());
        var expected = Encoding.UTF8.GetBytes(_authToken);
        return presented.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(presented, expected);
    }

    private void ServeFrame(HttpListenerRequest request, HttpListenerResponse response)
    {
        bool needsInitialFrame;
        lock (_frameGate)
            needsInitialFrame = _latestFrame.Length == 0;

        if (needsInitialFrame)
        {
            StartCaptureLoop();
            QueueCapture();
        }

        var afterVersion = -1L;
        if (long.TryParse(request.QueryString["after"], out var requestedVersion))
            afterVersion = requestedVersion;

        byte[] frame;
        long frameVersion;
        lock (_frameGate)
        {
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (!_disposed && _frameVersion <= afterVersion)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                    break;

                Monitor.Wait(_frameGate, remaining);
            }

            frame = _latestFrame;
            frameVersion = _frameVersion;
        }

        response.Headers["X-Nuri-Frame-Version"] = frameVersion.ToString();
        if (frame.Length == 0)
        {
            Close(response, 204);
            return;
        }

        if (frameVersion <= afterVersion)
        {
            Close(response, 204);
            return;
        }

        response.StatusCode = 200;
        response.ContentType = "image/jpeg";
        response.ContentLength64 = frame.Length;
        response.Headers["Cache-Control"] = "no-store";
        response.Headers["ETag"] = $"\"{frameVersion}\"";
        response.OutputStream.Write(frame, 0, frame.Length);
        response.Close();
    }

    private void ServeStatus(HttpListenerResponse response)
    {
        var status = _getStatus();
        var json = JsonSerializer.Serialize(new
        {
            protocol = PreviewProtocol.Name,
            message = status.Message,
            isBuilding = status.IsBuilding,
            hasError = status.HasError
        });
        var bytes = Encoding.UTF8.GetBytes(json);

        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.Headers["Cache-Control"] = "no-store";
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    private void FocusWindow(HttpListenerResponse response)
    {
        _window.Dispatcher.BeginInvoke(() =>
        {
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;

            _window.Show();
            _window.Activate();
            _window.Topmost = true;
            _window.Topmost = false;
            _window.Focus();
        });

        WriteJson(response, "{\"ok\":true}");
    }

    private static void WriteJson(HttpListenerResponse response, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    private static void Close(HttpListenerResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        response.Close();
    }
}
