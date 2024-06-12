using System.Net.Sockets;

namespace SafeChat
{
    public abstract class MessageService
    {
        public abstract event Action<string>? MessageReceived;

        protected NetworkStream? Stream;
        protected CancellationTokenSource? CancellationTokenSource;

        public void PassConnectionParams(NetworkStream stream, CancellationTokenSource cancellationTokenSource)
        {
            Stream ??= stream;
            CancellationTokenSource ??= cancellationTokenSource;
        }

        public abstract Task StartReceivingMessages();
        public abstract Task SendMessage(string message);
    }
}