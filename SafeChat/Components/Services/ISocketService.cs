using System;
using System.Threading.Tasks;

public interface ISocketService
{
    event Action<string>? MessageReceived;
    event Action? ConnectionEstablished;
    event Action? ConnectionClosed;

    Task StartConnection(string role, string host, int port);
    Task SendMessage(string message);
    void Stop();
}
