using System;
using System.Threading.Tasks;

public interface IConnectionService
{
    event Action? ConnectionEstablished;
    event Action? ConnectionClosed;

    Task StartConnection(string role, string host, int port);
    void Stop();
}