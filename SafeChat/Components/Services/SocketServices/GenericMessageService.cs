using System.Text;

namespace SafeChat
{
    public class GenericMessageService : MessageService
    {
        public override event Action<string>? MessageReceived;

        public override async Task StartReceivingMessages()
        {
            byte[] buffer = new byte[1024];

            while (Stream != null && !CancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await Stream!.ReadAsync(buffer, 0, buffer.Length, CancellationTokenSource.Token);
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
                catch (Exception)
                {
                    break;
                }
            }
        }

        public override async Task SendMessage(string message)
        {
            if (Stream != null && Stream.CanWrite)
            {
                System.Diagnostics.Debug.WriteLine($"Sending message: {message}");
                byte[] data = Encoding.UTF8.GetBytes(message);
                await Stream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}