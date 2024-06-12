using System.Net.Sockets;

namespace SafeChat
{
    public abstract class ConnectionService
    {
        public abstract event Action? ConnectionEstablished;
        public abstract event Action? ConnectionClosed;

        public NetworkStream? Stream { get; protected set; }
        public CancellationTokenSource? CancellationTokenSource { get; protected set; } = new CancellationTokenSource();

        public abstract Task StartConnection(string role, string host, int port);
        public abstract void Stop();
    }
}