using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalProxy.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalProxy.Engine.Tests;

public class HttpForwardHandlerTests
{
    private readonly ILogger<HttpForwardHandler> _logger = NullLogger<HttpForwardHandler>.Instance;

    [Fact]
    public async Task ForwardsHttpGetRequest()
    {
        var remotePort = GetAvailablePort();
        var httpServer = StartSimpleHttpServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-http",
            Protocol = ProxyProtocol.Http,
            LocalPort = GetAvailablePort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 10,
            TimeoutSeconds = 5
        };

        using var handler = new HttpForwardHandler(config, _logger);
        _ = handler.StartAsync();
        await Task.Delay(200);

        // Raw HTTP request via TCP to exercise the forwarder
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", config.LocalPort);
        var stream = client.GetStream();

        var request = "GET / HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(request));

        using var reader = new StreamReader(stream);
        var response = await reader.ReadToEndAsync();

        Assert.Contains("200 OK", response);
        Assert.Contains("OK", response);

        var stats = handler.GetStats();
        Assert.Equal(TunnelStatus.Running, stats.Status);
        Assert.True(stats.BytesIn > 0);

        await handler.StopAsync();
    }

    [Fact]
    public async Task HandlesMultipleHttpRequests()
    {
        var remotePort = GetAvailablePort();
        var httpServer = StartSimpleHttpServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-http-multi",
            Protocol = ProxyProtocol.Http,
            LocalPort = GetAvailablePort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 10,
            TimeoutSeconds = 5
        };

        using var handler = new HttpForwardHandler(config, _logger);
        _ = handler.StartAsync();
        await Task.Delay(200);

        var tasks = new Task[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", config.LocalPort);
                var stream = client.GetStream();
                var req = "GET / HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(req));
                using var reader = new StreamReader(stream);
                var resp = await reader.ReadToEndAsync();
                Assert.Contains("200 OK", resp);
            });
        }

        await Task.WhenAll(tasks);
        await handler.StopAsync();
    }

    private static TcpListener StartSimpleHttpServer(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        _ = AcceptHttpClients(listener);
        return listener;
    }

    private static async Task AcceptHttpClients(TcpListener listener)
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = ServeHttpClientAsync(client);
            }
        }
        catch (ObjectDisposedException) { }
    }

    private static async Task ServeHttpClientAsync(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            var bytesRead = await stream.ReadAsync(buffer);
            var request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            // Read until \r\n\r\n (end of headers)
            while (!request.Contains("\r\n\r\n") && bytesRead > 0)
            {
                bytesRead = await stream.ReadAsync(buffer);
                request += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }

            var response = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 2\r\n\r\nOK";
            var responseBytes = Encoding.ASCII.GetBytes(response);
            await stream.WriteAsync(responseBytes);
            await stream.FlushAsync();
        }
        catch { }
        finally
        {
            client.Dispose();
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
