using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalProxy.Services;

namespace LocalProxy.Tests;

public class ProxyServiceTests
{
    [Fact]
    public async Task StartTcpProxy_ForwardsData_ClientReceivesResponse()
    {
        // Start a local TCP echo server
        var echoPort = GetAvailablePort();
        using var echoCts = new CancellationTokenSource();
        var echoTask = StartEchoServerAsync(echoPort, echoCts.Token);

        // Start proxy forwarding to the echo server
        var proxyPort = GetAvailablePort();
        using var proxyCts = new CancellationTokenSource();
        var proxyTask = ProxyService.StartTcpProxyAsync(proxyPort, "localhost", echoPort, proxyCts.Token);

        // Wait for proxy to be ready
        await Task.Delay(500);

        // Send data through the proxy
        using var client = new TcpClient();
        await client.ConnectAsync("localhost", proxyPort);

        var message = "Hello, Proxy!"u8.ToArray();
        await client.GetStream().WriteAsync(message);
        await client.GetStream().FlushAsync();

        // Read echo response
        var buffer = new byte[1024];
        var read = await client.GetStream().ReadAsync(buffer);

        var response = Encoding.UTF8.GetString(buffer, 0, read);
        Assert.Equal("Hello, Proxy!", response);

        // Cleanup
        client.Close();
        proxyCts.Cancel();
        echoCts.Cancel();

        try { await proxyTask; } catch (OperationCanceledException) { }
        try { await echoTask; } catch (OperationCanceledException) { }
    }

    private static async Task StartEchoServerAsync(int port, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = EchoClientAsync(client, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task EchoClientAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];
            var read = await stream.ReadAsync(buffer, ct);
            await stream.WriteAsync(buffer.AsMemory(0, read), ct);
            await stream.FlushAsync(ct);
        }
        catch (OperationCanceledException) { }
        finally
        {
            client.Dispose();
        }
    }

    [Fact]
    public async Task StartHttpProxy_ForwardsRequest_ClientReceivesResponse()
    {
        // Start a TCP echo server
        var echoPort = GetAvailablePort();
        using var echoCts = new CancellationTokenSource();
        var echoTask = StartEchoServerAsync(echoPort, echoCts.Token);

        // Start HTTP proxy forwarding to echo server
        var proxyPort = GetAvailablePort();
        using var proxyCts = new CancellationTokenSource();
        var proxyTask = ProxyService.StartHttpProxyAsync(proxyPort, "localhost", echoPort, proxyCts.Token);

        await Task.Delay(500);

        // Send HTTP GET request through proxy
        using var client = new TcpClient();
        await client.ConnectAsync("localhost", proxyPort);

        // Send HTTP-like request that the HTTP handler parses first line
        var request = "GET /test HTTP/1.1\r\n"u8.ToArray();
        await client.GetStream().WriteAsync(request);
        await client.GetStream().FlushAsync();

        // Wait for proxy to process and echo server to respond
        await Task.Delay(100);

        // Read echoed response
        var buffer = new byte[1024];
        var read = await client.GetStream().ReadAsync(buffer);

        var response = Encoding.UTF8.GetString(buffer, 0, read);
        Assert.NotEmpty(response);

        client.Close();
        proxyCts.Cancel();
        echoCts.Cancel();

        try { await proxyTask; } catch (OperationCanceledException) { }
        try { await echoTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task StartUdpProxy_ForwardsData_ClientReceivesResponse()
    {
        // Start a local UDP echo server
        var echoPort = GetAvailableUdpPort();
        using var echoCts = new CancellationTokenSource();
        var echoTask = StartUdpEchoServerAsync(echoPort, echoCts.Token);

        // Start UDP proxy forwarding to the echo server
        var proxyPort = GetAvailableUdpPort();
        using var proxyCts = new CancellationTokenSource();
        var proxyTask = ProxyService.StartUdpProxyAsync(proxyPort, "127.0.0.1", echoPort, proxyCts.Token);

        await Task.Delay(500);

        // Send data through the UDP proxy
        using var client = new UdpClient();
        var message = "Hello, UDP!"u8.ToArray();
        await client.SendAsync(message, new IPEndPoint(IPAddress.Loopback, proxyPort));

        // Receive echo response
        var response = await client.ReceiveAsync();
        Assert.Equal("Hello, UDP!", Encoding.UTF8.GetString(response.Buffer));

        proxyCts.Cancel();
        echoCts.Cancel();

        try { await proxyTask; } catch (OperationCanceledException) { }
        try { await echoTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task StartUdpProxy_UnresolvableHost_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ProxyService.StartUdpProxyAsync(
                12345, "this-host-definitely-does-not-exist.invalid", 53, CancellationToken.None);
        });

        Assert.Contains("无法解析主机地址", ex.Message);
    }

    private static async Task StartUdpEchoServerAsync(int port, CancellationToken ct)
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await server.ReceiveAsync(ct);
                await server.SendAsync(result.Buffer, result.RemoteEndPoint, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static int GetAvailableUdpPort()
    {
        using var udp = new UdpClient(0);
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }
}
