using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nuri.PreviewHost;

namespace Nuri.Duxel.PreviewHost;

internal sealed class PreviewCaptureServer : IDisposable
{
    private readonly Func<byte[]?> _captureFrame;
    private readonly Action _focusWindow;
    private readonly Func<PreviewCaptureStatus> _getStatus;
    private readonly string _connectionFilePath;
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly System.Threading.Timer _captureTimer;
    private readonly object _frameGate = new();
    private readonly string _authToken;
    private readonly int _captureIntervalMilliseconds;
    private readonly int _idleUnchangedFrameThreshold;
    private byte[] _latestFrame = Array.Empty<byte>();
    private long _frameVersion;
    private int _encodeInFlight;
    private int _unchangedCaptureCount;
    private volatile bool _disposed;

    public PreviewCaptureServer(
        Func<byte[]?> captureFrame,
        Action focusWindow,
        Func<PreviewCaptureStatus> getStatus,
        string connectionFilePath,
        int framesPerSecond = 15)
    {
        _captureFrame = captureFrame ?? throw new ArgumentNullException(nameof(captureFrame));
        _focusWindow = focusWindow ?? throw new ArgumentNullException(nameof(focusWindow));
        _getStatus = getStatus ?? throw new ArgumentNullException(nameof(getStatus));
        _connectionFilePath = connectionFilePath ?? throw new ArgumentNullException(nameof(connectionFilePath));
        _authToken = CreateToken();

        framesPerSecond = Math.Clamp(framesPerSecond, 1, 30);
        _captureIntervalMilliseconds = Math.Max(1, (int)Math.Round(1000.0 / framesPerSecond));
        _idleUnchangedFrameThreshold = Math.Max(3, framesPerSecond / 2);
        _captureTimer = new System.Threading.Timer(_ => CaptureFrame(), null, Timeout.Infinite, Timeout.Infinite);

        Port = FindFreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
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

        lock (_frameGate)
        {
            if (_disposed)
                return;

            Interlocked.Exchange(ref _unchangedCaptureCount, 0);
            _captureTimer.Change(0, _captureIntervalMilliseconds);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_frameGate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _captureTimer.Dispose();
            Monitor.PulseAll(_frameGate);
        }

        _shutdown.Cancel();

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

    private void CaptureFrame()
    {
        if (_disposed || Interlocked.CompareExchange(ref _encodeInFlight, 1, 0) != 0)
            return;

        try
        {
            var nextFrame = _captureFrame();
            if (nextFrame is not { Length: > 0 })
                return;

            lock (_frameGate)
            {
                if (_disposed)
                    return;

                if (_latestFrame.AsSpan().SequenceEqual(nextFrame))
                {
                    if (Interlocked.Increment(ref _unchangedCaptureCount) >= _idleUnchangedFrameThreshold
                        && !_disposed)
                    {
                        _captureTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
                else
                {
                    Interlocked.Exchange(ref _unchangedCaptureCount, 0);
                    _latestFrame = nextFrame;
                    _frameVersion++;
                    Monitor.PulseAll(_frameGate);
                }
            }
        }
        catch
        {
            // The native surface can be unavailable during resize or shutdown.
        }
        finally
        {
            Interlocked.Exchange(ref _encodeInFlight, 0);
        }
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
                    _focusWindow();
                    WriteJson(response, "{\"ok\":true}");
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

        var presented = Encoding.UTF8.GetBytes(authorization[prefix.Length..].Trim());
        var expected = Encoding.UTF8.GetBytes(_authToken);
        return presented.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(presented, expected);
    }

    private void ServeFrame(HttpListenerRequest request, HttpListenerResponse response)
    {
        lock (_frameGate)
        {
            if (_latestFrame.Length == 0)
                RequestCapture();
        }

        var afterVersion = long.TryParse(request.QueryString["after"], out var requestedVersion)
            ? requestedVersion
            : -1L;
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
        if (frame.Length == 0 || frameVersion <= afterVersion)
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
        WriteJson(response, json);
    }

    private static void WriteJson(HttpListenerResponse response, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.Headers["Cache-Control"] = "no-store";
        response.OutputStream.Write(bytes, 0, bytes.Length);
        response.Close();
    }

    private static void Close(HttpListenerResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        response.Close();
    }
}
