namespace SafeChat
{
    public abstract class SocketServiceDecorator : SocketService
    {
        protected SocketService SocketService;
        public SocketServiceDecorator(SocketService socketService) => SocketService = socketService;

        public override event Action? ConnectionEstablished
        {
            add => SocketService.ConnectionEstablished += value;
            remove => SocketService.ConnectionEstablished -= value;
        }

        public override event Action? ConnectionClosed
        {
            add => SocketService.ConnectionClosed += value;
            remove => SocketService.ConnectionClosed -= value;
        }

        public override event Action<string>? MessageReceived
        {
            add => SocketService.MessageReceived += value;
            remove => SocketService.MessageReceived -= value;
        }

        public override Task SendMessage(string message) => SocketService.SendMessage(message);

        public override Task StartConnection(string role, string host, int port) => SocketService.StartConnection(role, host, port);

        public override void Stop() => SocketService.Stop();
    }
}
