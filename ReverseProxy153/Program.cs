using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class TcpReverseProxy
{
    private readonly IPAddress localIpAddress;
    private readonly int localPort;
    private readonly IPAddress remoteIpAddress;
    private readonly int remotePort;

    public TcpReverseProxy(IPAddress localIpAddress, int localPort, IPAddress remoteIpAddress, int remotePort)
    {
        this.localIpAddress = localIpAddress;
        this.localPort = localPort;
        this.remoteIpAddress = remoteIpAddress;
        this.remotePort = remotePort;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(localIpAddress, localPort);
        listener.Start();

        Console.WriteLine($"TCP reverse proxy started on {localIpAddress}:{localPort}, forwarding to {remoteIpAddress}:{remotePort}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync(); 
            Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");

            var remoteClient = new TcpClient();
            await remoteClient.ConnectAsync(remoteIpAddress, remotePort);
            Console.WriteLine($"Connected to remote server at {remoteIpAddress}:{remotePort}");

            _ = Task.Run(async () => await ForwardDataAsync(client, remoteClient));
            _ = Task.Run(async () => await ForwardDataAsync(remoteClient, client));
        }
    }

    private async Task ForwardDataAsync(TcpClient srcClient, TcpClient dstClient)
    {
        try
        {
            var stream = srcClient.GetStream();
            var buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await dstClient.GetStream().WriteAsync(buffer, 0, bytesRead);
            }
        }
        catch (Exception ex)
        {
                Console.WriteLine($"Error forwarding data to/from {srcClient.Client.RemoteEndPoint}: {ex.Message}");
        }
        finally
        {
            srcClient.Close();
            dstClient.Close();
        }
    }
    static async Task Main(string[] args)
    {
        Console.WriteLine("Destination IP?");
        string ipDest = Console.ReadLine();
        if (string.IsNullOrEmpty(ipDest)) ipDest = "172.16.0.153";

        Console.WriteLine("Destination Port?");
        int portDest;
        if(!int.TryParse(Console.ReadLine(), out portDest)) portDest = 1433;

        TcpReverseProxy tcpReverseProxy = new TcpReverseProxy(IPAddress.Any,5000,IPAddress.Parse(ipDest),portDest);
        await tcpReverseProxy.StartAsync();
    }
}