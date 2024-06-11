using System.Net;
using System.Net.Sockets;
using System.Text;

public class SocketService
{
    private TcpListener? server;
    private TcpClient? client;
    private NetworkStream? stream;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private bool isStopping = false;

    public event Action<string>? MessageReceived;
    public event Action? ConnectionEstablished;
    public event Action? ConnectionClosed;

    public async Task StartServer(string host, int port)
    {
        server = new TcpListener(IPAddress.Parse(host), port);
        server.Start();
        Console.WriteLine("Waiting for a connection...");

        try
        {
            client = await server.AcceptTcpClientAsync();
            stream = client.GetStream();
            ConnectionEstablished?.Invoke();
            _ = Task.Run(() => ReceiveMessages(cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting server: {ex.Message}");
            Stop(); // Ensure proper cleanup
        }
    }

    public async Task StartClient(string host, int port)
    {
        client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port);
            stream = client.GetStream();
            Console.WriteLine($"Connected to server {host}:{port}");
            ConnectionEstablished?.Invoke();
            _ = Task.Run(() => ReceiveMessages(cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
            Stop(); // Ensure proper cleanup
        }
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
                else
                {
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                break;
            }
        }

        if (!isStopping)
        {
            ConnectionClosed?.Invoke();
        }
    }

    public void Stop()
    {
        try
        {
            isStopping = true;
            cancellationTokenSource.Cancel();
            if (stream != null && stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes("disconnect");
                stream.Write(data, 0, data.Length);
            }
            stream?.Close();
            client?.Close();
            server?.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping the service: {ex.Message}");
        }
        finally
        {
            Reset();
            ConnectionClosed?.Invoke();
        }
    }

    private void Reset()
    {
        server = null;
        client = null;
        stream = null;
        isStopping = false;
        cancellationTokenSource = new CancellationTokenSource();
    }
}
