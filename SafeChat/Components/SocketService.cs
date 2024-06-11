using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SocketService
{
    private TcpListener? server;
    private TcpClient? client;
    private NetworkStream? stream;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public event Action<string>? MessageReceived;

    public async Task StartServer(string host, int port)
    {
        server = new TcpListener(IPAddress.Parse(host), port);
        server.Start();
        Console.WriteLine("Waiting for a connection...");

        client = await server.AcceptTcpClientAsync();
        stream = client.GetStream();

        _ = Task.Run(() => ReceiveMessages(cancellationTokenSource.Token));
    }

    public async Task StartClient(string host, int port)
    {
        client = new TcpClient();
        await client.ConnectAsync(host, port);
        stream = client.GetStream();
        Console.WriteLine($"Connected to server {host}:{port}");

        _ = Task.Run(() => ReceiveMessages(cancellationTokenSource.Token));
    }

    public async Task SendMessage(string message)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    private async Task ReceiveMessages(CancellationToken token)
    {
        byte[] buffer = new byte[1024];

        while (!token.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await stream!.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                break;
            }
        }
    }

    public void Stop()
    {
        cancellationTokenSource.Cancel();
        stream?.Close();
        client?.Close();
        server?.Stop();
    }
}
