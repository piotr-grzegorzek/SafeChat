using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ConnectionService : IConnectionService
{
    public event Action? ConnectionEstablished;
    public event Action? ConnectionClosed;

    private TcpListener? server;
    private TcpClient? client;
    private NetworkStream? stream;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private bool isStopping = false;

    public async Task StartConnection(string role, string host, int port)
    {
        if (role == "server")
        {
            server = new TcpListener(IPAddress.Parse(host), port);
            server.Start();
            Console.WriteLine("Waiting for a connection...");

            try
            {
                client = await server.AcceptTcpClientAsync();
                stream = client.GetStream();
                ConnectionEstablished?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
                Stop(); // Ensure proper cleanup
            }
        }
        else if (role == "client")
        {
            client = new TcpClient();
            try
            {
                await client.ConnectAsync(host, port);
                stream = client.GetStream();
                Console.WriteLine($"Connected to server {host}:{port}");
                ConnectionEstablished?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to server: {ex.Message}");
                Stop(); // Ensure proper cleanup
            }
        }
    }

    public void Stop()
    {
        try
        {
            isStopping = true;
            cancellationTokenSource.Cancel();
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