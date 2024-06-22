namespace SafeChat
{
    public abstract class SocketService
    {
        public abstract event Action? ConnectionEstablished;
        public abstract event Action? ConnectionClosed;
        public abstract event Action<string>? MessageReceived;

        public abstract Task StartConnection(string role, string host, int port);
        public abstract Task SendMessage(string message);
        public abstract void Stop();
    }
}