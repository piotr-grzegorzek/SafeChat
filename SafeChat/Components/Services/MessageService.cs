using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MessageService : IMessageService
{
    public event Action<string>? MessageReceived;

    private NetworkStream? stream;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public MessageService(NetworkStream? stream)
    {
        this.stream = stream;
    }

    public async Task SendMessage(string message)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    public async Task StartReceivingMessages()
    {
        byte[] buffer = new byte[1024];

        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await stream!.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
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
    }
}