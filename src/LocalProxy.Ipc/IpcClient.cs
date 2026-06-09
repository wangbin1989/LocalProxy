using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace LocalProxy.Ipc;

public sealed class IpcClient : IDisposable
{
    private readonly string _pipeName;
    private readonly int _timeoutMs;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = IpcJsonContext.Default,
    };

    public IpcClient(string pipeName, int timeoutMs = 5000)
    {
        _pipeName = pipeName;
        _timeoutMs = timeoutMs;
    }

    public async Task<JsonRpcResponse> SendAsync(JsonRpcRequest request, CancellationToken ct = default)
    {
        using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await pipe.ConnectAsync(_timeoutMs, ct);

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
            await pipe.WriteAsync(requestBytes, ct);
            await pipe.FlushAsync(ct);

            var buffer = new byte[65536];
            var bytesRead = await pipe.ReadAsync(buffer, ct);
            var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            var response = JsonSerializer.Deserialize<JsonRpcResponse>(responseJson, JsonOptions);
            return response ?? new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32700, Message = "Parse error" }
            };
        }
        catch (TimeoutException)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = IpcProtocol.ErrorServiceNotAvailable, Message = "Service not available" }
            };
        }
    }

    public void Dispose() { }
}
