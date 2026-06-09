using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalProxy.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalProxy.Engine.Tests;

public class UdpForwardHandlerTests
{
    private readonly ILogger<UdpForwardHandler> _logger = NullLogger<UdpForwardHandler>.Instance;

    [Fact]
    public async Task ForwardsUdpDatagramAndReceivesResponse()
    {
        var remotePort = GetAvailableUdpPort();
        var echoServer = StartUdpEchoServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-udp",
            Protocol = ProxyProtocol.Udp,
            LocalPort = GetAvailableUdpPort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 10,
            TimeoutSeconds = 5
        };

        using var handler = new UdpForwardHandler(config, _logger);
        _ = handler.StartAsync();
        await Task.Delay(200);

        // Send UDP datagram through proxy
        using var client = new UdpClient();
        var testMessage = "Hello, UDP Proxy!";
        var sendBuffer = Encoding.UTF8.GetBytes(testMessage);
        await client.SendAsync(sendBuffer, "127.0.0.1", config.LocalPort);

        // Receive response
        var result = await client.ReceiveAsync();

        var response = Encoding.UTF8.GetString(result.Buffer);
        Assert.Equal(testMessage, response);

        var stats = handler.GetStats();
        Assert.Equal(TunnelStatus.Running, stats.Status);

        await handler.StopAsync();
    }

    [Fact]
    public async Task MultipleClientsCanUseUdp()
    {
        var remotePort = GetAvailableUdpPort();
        var echoServer = StartUdpEchoServer(remotePort);

        var config = new ProxyConfig
        {
            Name = "test-udp-multi",
            Protocol = ProxyProtocol.Udp,
            LocalPort = GetAvailableUdpPort(),
            RemoteHost = "127.0.0.1",
            RemotePort = remotePort,
            MaxConnections = 10,
            TimeoutSeconds = 5
        };

        using var handler = new UdpForwardHandler(config, _logger);
        _ = handler.StartAsync();
        await Task.Delay(200);

        var tasks = new Task[5];
        for (int i = 0; i < 5; i++)
        {
            var idx = i;
            tasks[i] = Task.Run(async () =>
            {
                using var client = new UdpClient();
                var msg = Encoding.UTF8.GetBytes($"udp-msg-{idx}");
                await client.SendAsync(msg, "127.0.0.1", config.LocalPort);
                var result = await client.ReceiveAsync();
                Assert.Equal(msg.Length, result.Buffer.Length);
            });
        }

        await Task.WhenAll(tasks);
        await handler.StopAsync();
    }

    private static UdpClient StartUdpEchoServer(int port)
    {
        var server = new UdpClient(port);
        _ = EchoLoop(server);
        return server;
    }

    private static async Task EchoLoop(UdpClient server)
    {
        try
        {
            while (true)
            {
                var result = await server.ReceiveAsync();
                await server.SendAsync(result.Buffer, result.RemoteEndPoint);
            }
        }
        catch (ObjectDisposedException) { }
    }

    private static int GetAvailableUdpPort()
    {
        var client = new UdpClient(0);
        var port = ((IPEndPoint)client.Client.LocalEndPoint!).Port;
        client.Close();
        return port;
    }
}
