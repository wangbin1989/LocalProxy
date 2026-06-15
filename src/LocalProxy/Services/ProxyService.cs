using System.Net;
using System.Net.Sockets;
using LocalProxy.Infrastructure;

namespace LocalProxy.Services;

/// <summary>代理转发服务，实现 TCP/UDP/HTTP 端口转发</summary>
public static class ProxyService
{
    /// <summary>启动 TCP 代理，将本地端口流量转发到远程主机</summary>
    public static async Task StartTcpProxyAsync(
        int localPort, string remoteHost, int remotePort, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Loopback, localPort);
        listener.Start();

        try
        {
            ConsoleOutput.Info($"TCP 代理已启动，监听端口 :{localPort}");

            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = HandleTcpClientAsync(client, remoteHost, remotePort, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>启动 HTTP 代理，解析 HTTP 请求行后转发到远程主机</summary>
    public static async Task StartHttpProxyAsync(
        int localPort, string remoteHost, int remotePort, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Loopback, localPort);
        listener.Start();

        try
        {
            ConsoleOutput.Info($"HTTP 代理已启动，监听端口 :{localPort}");

            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = HandleHttpClientAsync(client, remoteHost, remotePort, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>处理 HTTP 客户端请求：解析请求行、连接远程、双向中继</summary>
    private static async Task HandleHttpClientAsync(
        TcpClient client, string remoteHost, int remotePort, CancellationToken ct)
    {
        using var remote = new TcpClient();

        try
        {
            var clientStream = client.GetStream();

            // 读取请求行用于日志
            var requestLine = await ReadLineAsync(clientStream, ct);
            if (string.IsNullOrWhiteSpace(requestLine))
                return;

            var parts = requestLine.Split(' ');
            var method = parts.Length > 0 ? parts[0] : "?";
            var path = parts.Length > 1 ? parts[1] : "/";

            // 连接远程
            await remote.ConnectAsync(remoteHost, remotePort, ct);
            var remoteStream = remote.GetStream();

            // 转发请求行到远程
            await WriteLineAsync(remoteStream, requestLine, ct);

            ConsoleOutput.ConnectionInfo($"HTTP {method} {path} → {remoteHost}:{remotePort}");

            // 双向中继：clientStream 中剩余数据（请求头+请求体）→ remoteStream
            //            remoteStream 返回数据（响应）→ clientStream
            var task1 = clientStream.CopyToAsync(remoteStream, ct);
            var task2 = remoteStream.CopyToAsync(clientStream, ct);

            await Task.WhenAny(task1, task2);
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
        catch (IOException) { }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>从网络流中逐字节读取一行（以 \n 结尾）</summary>
    private static async Task<string?> ReadLineAsync(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new List<byte>(256);
        var singleByte = new byte[1];

        while (true)
        {
            var read = await stream.ReadAsync(singleByte, ct);
            if (read == 0) return null;

            if (singleByte[0] == '\n')
                return System.Text.Encoding.UTF8.GetString(buffer.ToArray());

            if (singleByte[0] != '\r')
                buffer.Add(singleByte[0]);
        }
    }

    /// <summary>向网络流写入一行（自动追加 \r\n）</summary>
    private static async Task WriteLineAsync(NetworkStream stream, string line, CancellationToken ct)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(line + "\r\n");
        await stream.WriteAsync(data, ct);
        await stream.FlushAsync(ct);
    }

    /// <summary>处理 TCP 客户端连接：建立远程连接后双向转发数据</summary>
    private static async Task HandleTcpClientAsync(
        TcpClient client, string remoteHost, int remotePort, CancellationToken ct)
    {
        var clientEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "?";
        ConsoleOutput.ConnectionInfo($"新连接 {clientEndPoint} → {remoteHost}:{remotePort}");

        try
        {
            using var remote = new TcpClient();
            await remote.ConnectAsync(remoteHost, remotePort, ct);

            var clientStream = client.GetStream();
            var remoteStream = remote.GetStream();

            var task1 = clientStream.CopyToAsync(remoteStream, ct);
            var task2 = remoteStream.CopyToAsync(clientStream, ct);

            await Task.WhenAny(task1, task2);
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
        catch (IOException) { }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>启动 UDP 代理，将本地端口数据报转发到远程主机并回传响应</summary>
    public static async Task StartUdpProxyAsync(
        int localPort, string remoteHost, int remotePort, CancellationToken ct)
    {
        using var localUdp = new UdpClient(new IPEndPoint(IPAddress.Loopback, localPort));
        var remoteEndPoint = new IPEndPoint(Dns.GetHostAddresses(remoteHost).First(), remotePort);

        ConsoleOutput.Info($"UDP 代理已启动，监听端口 :{localPort}");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await localUdp.ReceiveAsync(ct);

                _ = HandleUdpDatagramAsync(localUdp, result.Buffer, result.RemoteEndPoint, remoteEndPoint, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }

    /// <summary>处理单个 UDP 数据报：发送到远程并回传响应</summary>
    private static async Task HandleUdpDatagramAsync(
        UdpClient localUdp, byte[] data, IPEndPoint clientEndPoint, IPEndPoint remoteEndPoint, CancellationToken ct)
    {
        try
        {
            using var remoteUdp = new UdpClient();
            await remoteUdp.SendAsync(data, remoteEndPoint, ct);

            var response = await remoteUdp.ReceiveAsync(ct);
            await localUdp.SendAsync(response.Buffer, clientEndPoint, ct);
        }
        catch (OperationCanceledException) { }
        catch (SocketException) { }
    }
}
