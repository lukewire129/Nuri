using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Nuri.SimplyShare.Features.Discovery;
using Nuri.SimplyShare.Features.Settings;

namespace Nuri.SimplyShare.Features.Transfers;

public sealed class SecureTransferService : IAsyncDisposable
{
    private const int ChunkSize = 64 * 1024;
    private const int MaxFrameSize = ChunkSize + 4096;
    private readonly AppSettings _settings;
    private readonly ConcurrentDictionary<int, TcpClient> _clients = new();
    private readonly ConcurrentDictionary<int, Task> _connectionTasks = new();
    private CancellationTokenSource? _lifetime;
    private TcpListener? _listener;
    private Task? _acceptTask;
    private int _nextConnectionId;

    public SecureTransferService(AppSettings settings) => _settings = settings;

    public event Action<TransferItem>? TransferChanged;
    public event Action<string, string, string>? TextReceived;
    public event Action<string, string, string>? FileReceived;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_settings.DownloadPath);
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Any, _settings.TransferPort);
        _listener.Start();
        _acceptTask = AcceptLoopAsync(_lifetime.Token);
        return Task.CompletedTask;
    }

    public async Task SendTextAsync(DeviceInfo target, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var id = Guid.NewGuid().ToString("N")[..12];
        var transfer = new TransferItem(id, "Text message", target.Nickname, TransferDirection.Sending, 0, text.Length, TransferStatus.Connecting);
        TransferChanged?.Invoke(transfer);

        try
        {
            using var operation = CreateOperationToken(cancellationToken);
            using var client = CreateTrackedClient();
            await client.Value.ConnectAsync(target.IpAddress, target.Port, operation.Token);
            await using var stream = client.Value.GetStream();
            using var cipher = await ExchangeKeysAsClientAsync(stream, operation.Token);
            var header = new TransferHeader("text", id, _settings.DeviceId, _settings.Nickname, null, text.Length, text);
            await WriteEncryptedJsonAsync(stream, cipher, header, operation.Token);
            await ReadEncryptedJsonAsync<TransferResponse>(stream, cipher, operation.Token);
            TransferChanged?.Invoke(transfer with { BytesTransferred = text.Length, Status = TransferStatus.Completed });
        }
        catch (Exception exception)
        {
            TransferChanged?.Invoke(transfer with { Status = TransferStatus.Failed, Error = exception.Message });
            throw;
        }
    }

    public async Task SendFilesAsync(DeviceInfo target, IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        foreach (var path in paths.Where(File.Exists))
            await SendFileAsync(target, path, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var lifetime = _lifetime;
        if (lifetime is null)
            return;

        _lifetime = null;
        lifetime.Cancel();
        var listener = _listener;
        _listener = null;
        listener?.Stop();
        foreach (var client in _clients.Values)
            client.Dispose();

        try
        {
            if (_acceptTask is not null)
                await _acceptTask;
            foreach (var client in _clients.Values)
                client.Dispose();
            await Task.WhenAll(_connectionTasks.Values);
        }
        catch
        {
        }

        lifetime.Dispose();
        _acceptTask = null;
        _clients.Clear();
        _connectionTasks.Clear();
    }

    private async Task SendFileAsync(DeviceInfo target, string path, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        var id = Guid.NewGuid().ToString("N")[..12];
        var transfer = new TransferItem(id, info.Name, target.Nickname, TransferDirection.Sending, 0, info.Length, TransferStatus.Connecting);
        TransferChanged?.Invoke(transfer);

        try
        {
            using var operation = CreateOperationToken(cancellationToken);
            using var client = CreateTrackedClient();
            await client.Value.ConnectAsync(target.IpAddress, target.Port, operation.Token);
            await using var stream = client.Value.GetStream();
            using var cipher = await ExchangeKeysAsClientAsync(stream, operation.Token);
            var header = new TransferHeader("file", id, _settings.DeviceId, _settings.Nickname, info.Name, info.Length, null);
            await WriteEncryptedJsonAsync(stream, cipher, header, operation.Token);
            var response = await ReadEncryptedJsonAsync<TransferResponse>(stream, cipher, operation.Token);
            if (!response.Accepted)
                throw new IOException(response.Error ?? "The receiving device rejected the file.");

            await using var file = File.OpenRead(path);
            var buffer = new byte[ChunkSize];
            long sent = 0;
            while (true)
            {
                var read = await file.ReadAsync(buffer, operation.Token);
                if (read == 0)
                    break;

                await WriteEncryptedFrameAsync(stream, cipher, buffer.AsMemory(0, read), operation.Token);
                sent += read;
                TransferChanged?.Invoke(transfer with
                {
                    BytesTransferred = sent,
                    Status = sent == info.Length ? TransferStatus.Completed : TransferStatus.Transferring
                });
            }

            if (info.Length == 0)
                TransferChanged?.Invoke(transfer with { Status = TransferStatus.Completed });
        }
        catch (Exception exception)
        {
            TransferChanged?.Invoke(transfer with { Status = TransferStatus.Failed, Error = exception.Message });
            throw;
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(cancellationToken);
                var connectionId = TrackClient(client);
                var task = HandleClientAsync(connectionId, client, cancellationToken);
                _connectionTasks[connectionId] = task;
                _ = task.ContinueWith(
                    completedTask => _connectionTasks.TryRemove(connectionId, out _),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private async Task HandleClientAsync(int connectionId, TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                using var cipher = await ExchangeKeysAsServerAsync(stream, cancellationToken);
                var header = await ReadEncryptedJsonAsync<TransferHeader>(stream, cipher, cancellationToken);
                if (header.Kind == "text")
                {
                    await WriteEncryptedJsonAsync(stream, cipher, new TransferResponse(true, null), cancellationToken);
                    TextReceived?.Invoke(header.SenderName, header.SenderId, header.Text ?? string.Empty);
                    return;
                }

                if (header.Kind != "file" || string.IsNullOrWhiteSpace(header.FileName) || header.Length < 0)
                {
                    await WriteEncryptedJsonAsync(stream, cipher, new TransferResponse(false, "Invalid transfer header."), cancellationToken);
                    return;
                }

                var safeName = Path.GetFileName(header.FileName);
                var destination = GetUniquePath(_settings.DownloadPath, safeName);
                await WriteEncryptedJsonAsync(stream, cipher, new TransferResponse(true, null), cancellationToken);

                var transfer = new TransferItem(header.TransferId, safeName, header.SenderName, TransferDirection.Receiving, 0, header.Length, TransferStatus.Transferring);
                TransferChanged?.Invoke(transfer);
                await using var file = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, ChunkSize, useAsync: true);
                long received = 0;
                while (received < header.Length)
                {
                    var chunk = await ReadEncryptedFrameAsync(stream, cipher, cancellationToken);
                    if (chunk.Length == 0 || received + chunk.Length > header.Length)
                        throw new IOException("The encrypted file stream ended unexpectedly.");
                    await file.WriteAsync(chunk, cancellationToken);
                    received += chunk.Length;
                    TransferChanged?.Invoke(transfer with
                    {
                        BytesTransferred = received,
                        Status = received == header.Length ? TransferStatus.Completed : TransferStatus.Transferring
                    });
                }

                if (header.Length == 0)
                    TransferChanged?.Invoke(transfer with { Status = TransferStatus.Completed });

                FileReceived?.Invoke(header.SenderName, header.SenderId, destination);
            }
            catch (Exception exception)
            {
                TransferChanged?.Invoke(new TransferItem(
                    Guid.NewGuid().ToString("N")[..12],
                    "Incoming transfer",
                    client.Client.RemoteEndPoint?.ToString() ?? "Unknown",
                    TransferDirection.Receiving,
                    0,
                    0,
                    TransferStatus.Failed,
                    exception.Message));
            }
            finally
            {
                _clients.TryRemove(connectionId, out _);
            }
        }
    }

    private CancellationTokenSource CreateOperationToken(CancellationToken cancellationToken)
    {
        var lifetimeToken = _lifetime?.Token ?? new CancellationToken(canceled: true);
        return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetimeToken);
    }

    private TrackedClient CreateTrackedClient()
    {
        var client = new TcpClient { NoDelay = true };
        return new TrackedClient(this, TrackClient(client), client);
    }

    private int TrackClient(TcpClient client)
    {
        var connectionId = Interlocked.Increment(ref _nextConnectionId);
        _clients[connectionId] = client;
        return connectionId;
    }

    private static async Task<AesGcm> ExchangeKeysAsClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        await WriteFrameAsync(stream, ecdh.ExportSubjectPublicKeyInfo(), cancellationToken);
        var remoteKey = await ReadFrameAsync(stream, 1024, cancellationToken);
        return CreateCipher(ecdh, remoteKey);
    }

    private static async Task<AesGcm> ExchangeKeysAsServerAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var remoteKey = await ReadFrameAsync(stream, 1024, cancellationToken);
        await WriteFrameAsync(stream, ecdh.ExportSubjectPublicKeyInfo(), cancellationToken);
        return CreateCipher(ecdh, remoteKey);
    }

    private static AesGcm CreateCipher(ECDiffieHellman ech, byte[] remoteKey)
    {
        using var remote = ECDiffieHellman.Create();
        remote.ImportSubjectPublicKeyInfo(remoteKey, out _);
        var secret = ech.DeriveKeyMaterial(remote.PublicKey);
        var key = SHA256.HashData(secret);
        CryptographicOperations.ZeroMemory(secret);
        return new AesGcm(key, 16);
    }

    private static async Task WriteEncryptedJsonAsync<T>(Stream stream, AesGcm cipher, T value, CancellationToken cancellationToken)
    {
        await WriteEncryptedFrameAsync(stream, cipher, JsonSerializer.SerializeToUtf8Bytes(value), cancellationToken);
    }

    private static async Task<T> ReadEncryptedJsonAsync<T>(Stream stream, AesGcm cipher, CancellationToken cancellationToken)
    {
        var payload = await ReadEncryptedFrameAsync(stream, cipher, cancellationToken);
        return JsonSerializer.Deserialize<T>(payload) ?? throw new IOException("Invalid encrypted message.");
    }

    private static async Task WriteEncryptedFrameAsync(Stream stream, AesGcm cipher, ReadOnlyMemory<byte> plaintext, CancellationToken cancellationToken)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var ciphertext = new byte[plaintext.Length];
        cipher.Encrypt(nonce, plaintext.Span, ciphertext, tag);
        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, nonce.Length);
        ciphertext.CopyTo(payload, nonce.Length + tag.Length);
        await WriteFrameAsync(stream, payload, cancellationToken);
    }

    private static async Task<byte[]> ReadEncryptedFrameAsync(Stream stream, AesGcm cipher, CancellationToken cancellationToken)
    {
        var payload = await ReadFrameAsync(stream, MaxFrameSize, cancellationToken);
        if (payload.Length < 28)
            throw new IOException("Invalid encrypted frame.");

        var plaintext = new byte[payload.Length - 28];
        cipher.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), plaintext);
        return plaintext;
    }

    private static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, payload.Length);
        await stream.WriteAsync(length, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
    }

    private static async Task<byte[]> ReadFrameAsync(Stream stream, int maximumLength, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes, cancellationToken);
        var length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);
        if (length <= 0 || length > maximumLength)
            throw new IOException("Invalid frame length.");
        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellationToken);
        return payload;
    }

    private static string GetUniquePath(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate))
            return candidate;

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var index = 2; ; index++)
        {
            candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            if (!File.Exists(candidate))
                return candidate;
        }
    }

    private sealed record TransferHeader(
        string Kind,
        string TransferId,
        string SenderId,
        string SenderName,
        string? FileName,
        long Length,
        string? Text);

    private sealed record TransferResponse(bool Accepted, string? Error);

    private sealed class TrackedClient : IDisposable
    {
        private SecureTransferService? _owner;
        private readonly int _connectionId;

        public TrackedClient(SecureTransferService owner, int connectionId, TcpClient value)
        {
            _owner = owner;
            _connectionId = connectionId;
            Value = value;
        }

        public TcpClient Value { get; }

        public void Dispose()
        {
            Value.Dispose();
            Interlocked.Exchange(ref _owner, null)?._clients.TryRemove(_connectionId, out _);
        }
    }
}
