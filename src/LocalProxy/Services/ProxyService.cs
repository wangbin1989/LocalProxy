using System.Net;
using System.Net.Sockets;
using LocalProxy.Infrastructure;

namespace LocalProxy.Services;

public static class ProxyService
{
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
