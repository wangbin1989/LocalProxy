using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalProxy.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalProxy.Engine.Tests;

public class TcpForwardHandlerTests
{
    private readonly ILogger<TcpForwardHandler> _logger = NullLogger<TcpForwardHandler>.Instance;

    [Fact]
    public async Task ForwardsDataFromLocalToRemote()
    {
        // Start a remote echo server
        var remotePort = GetAvailablePort();
        var echoServer = StartEchoServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-tcp",
            Protocol = ProxyProtocol.Tcp,
            LocalPort = GetAvailablePort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 10,
            TimeoutSeconds = 5
        };

        using var handler = new TcpForwardHandler(config, _logger);
        var startTask = handler.StartAsync();

        // Give it time to start
        await Task.Delay(200);

        // Connect to local proxy
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", config.LocalPort);

        // Send test message
        var testMessage = "Hello, Proxy!";
        var sendBuffer = Encoding.UTF8.GetBytes(testMessage);
        await client.GetStream().WriteAsync(sendBuffer);

        // Read echo response
        var receiveBuffer = new byte[1024];
        var bytesRead = await client.GetStream().ReadAsync(receiveBuffer);
        var response = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);

        Assert.Equal(testMessage, response);

        // Verify stats
        var stats = handler.GetStats();
        Assert.Equal(TunnelStatus.Running, stats.Status);
        Assert.True(stats.BytesIn > 0);
        Assert.True(stats.BytesOut > 0);

        // Cleanup
        await handler.StopAsync();
        Assert.Equal(TunnelStatus.Stopped, handler.GetStats().Status);
    }

    [Fact]
    public async Task HandlesConcurrentConnections()
    {
        var remotePort = GetAvailablePort();
        var echoServer = StartEchoServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-concurrent",
            Protocol = ProxyProtocol.Tcp,
            LocalPort = GetAvailablePort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 50,
            TimeoutSeconds = 10
        };

        using var handler = new TcpForwardHandler(config, _logger);
        _ = handler.StartAsync();
        await Task.Delay(200);

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", config.LocalPort);
                var msg = Encoding.UTF8.GetBytes($"msg-{i}");
                await client.GetStream().WriteAsync(msg);
                var buf = new byte[1024];
                var n = await client.GetStream().ReadAsync(buf);
                Assert.Equal(msg.Length, n);
            });
        }

        await Task.WhenAll(tasks);
        await handler.StopAsync();
    }

    [Fact]
    public async Task ConnectionLimitIsEnforced()
    {
        var remotePort = GetAvailablePort();
        var slowServer = StartSlowServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-limit",
            Protocol = ProxyProtocol.Tcp,
            LocalPort = GetAvailablePort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 2,
            TimeoutSeconds = 5
        };

        using var handler = new TcpForwardHandler(config, _logger);
        _ = handler.StartAsync();
        await Task.Delay(200);

        // Fill all available connection slots
        var clients = new TcpClient[2];
        for (int i = 0; i < 2; i++)
        {
            clients[i] = new TcpClient();
            await clients[i].ConnectAsync("127.0.0.1", config.LocalPort);
        }

        // Active connections should not exceed MaxConnections
        var stats = handler.GetStats();
        Assert.True(stats.ActiveConnections <= config.MaxConnections);

        foreach (var c in clients) c.Dispose();
        await handler.StopAsync();
    }

    [Fact]
    public async Task StartStopCycleDoesNotLeakResources()
    {
        var config = new ProxyConfig
        {
            Name = "test-cycle",
            Protocol = ProxyProtocol.Tcp,
            LocalPort = GetAvailablePort(),
            RemoteHost = "127.0.0.1",
            RemotePort = GetAvailablePort(),
            MaxConnections = 10,
            TimeoutSeconds = 2
        };

        for (int i = 0; i < 50; i++)
        {
            using var handler = new TcpForwardHandler(config, _logger);
            _ = handler.StartAsync();
            await Task.Delay(50);
            await handler.StopAsync();
        }
    }

    private static TcpListener StartEchoServer(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        _ = AcceptEchoClients(listener);
        return listener;
    }

    private static async Task AcceptEchoClients(TcpListener listener)
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = EchoClient(client);
            }
        }
        catch (ObjectDisposedException) { }
    }

    private static async Task EchoClient(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            var bytesRead = await stream.ReadAsync(buffer);
            await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
        catch { }
        finally
        {
            client.Dispose();
        }
    }

    private static TcpListener StartSlowServer(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        _ = AcceptSlowClients(listener);
        return listener;
    }

    private static async Task AcceptSlowClients(TcpListener listener)
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(10_000); // Hold connection
                    }
                    finally
                    {
                        client.Dispose();
                    }
                });
            }
        }
        catch (ObjectDisposedException) { }
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
