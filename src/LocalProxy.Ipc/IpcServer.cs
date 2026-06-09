using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace LocalProxy.Ipc;

public sealed class IpcServer : IDisposable
{
    private readonly string _pipeName;
    private readonly Func<JsonRpcRequest, CancellationToken, Task<JsonRpcResponse>> _handler;
    private NamedPipeServerStream? _server;
    private CancellationTokenSource? _cts;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = IpcJsonContext.Default,
    };

    public IpcServer(string pipeName, Func<JsonRpcRequest, CancellationToken, Task<JsonRpcResponse>> handler)
    {
        _pipeName = pipeName;
        _handler = handler;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        while (!_cts.Token.IsCancellationRequested)
        {
            _server = new NamedPipeServerStream(_pipeName, PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            try
            {
                await _server.WaitForConnectionAsync(_cts.Token);
                _ = HandleConnectionAsync(_server);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe)
    {
        try
        {
            var buffer = new byte[65536];
            var bytesRead = await pipe.ReadAsync(buffer, _cts?.Token ?? default);
            if (bytesRead == 0) return;

            var requestJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestJson, JsonOptions);
            if (request == null) return;

            var response = await _handler(request, _cts?.Token ?? default);
            var responseJson = JsonSerializer.Serialize(response, JsonOptions);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson + "\n");

            await pipe.WriteAsync(responseBytes, _cts?.Token ?? default);
            await pipe.FlushAsync();
        }
        catch (Exception) { }
        finally
        {
            pipe.Dispose();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _server?.Dispose();
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _server?.Dispose();
    }
}
