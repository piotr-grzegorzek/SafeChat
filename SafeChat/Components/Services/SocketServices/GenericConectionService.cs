using System.Net;
using System.Net.Sockets;

namespace SafeChat
{
    public class GenericConnectionService : ConnectionService
    {
        public override event Action? ConnectionEstablished;
        public override event Action? ConnectionClosed;

        private TcpListener? _server;
        private TcpClient? _client;

        public override async Task StartConnection(string role, string host, int port)
        {
            if (role == "server")
            {
                _server = new TcpListener(IPAddress.Parse(host), port);
                _server.Start();

                try
                {
                    _client = await _server.AcceptTcpClientAsync();
                    Stream = _client.GetStream();
                    ConnectionEstablished?.Invoke();
                }
                catch (Exception)
                {
                    Stop();
                }
            }
            else if (role == "client")
            {
                _client = new TcpClient();
                try
                {
                    await _client.ConnectAsync(host, port);
                    Stream = _client.GetStream();
                    ConnectionEstablished?.Invoke();
                }
                catch (Exception)
                {
                    Stop();
                }
            }
        }

        public override void Stop()
        {
            try
            {
                CancellationTokenSource.Cancel();
                Stream?.Close();
                _client?.Close();
                _server?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping the service: {ex.Message}");
            }
            finally
            {
                Reset();
                ConnectionClosed?.Invoke();
            }
        }

        private void Reset()
        {
            _server = null;
            _client = null;
            Stream = null;
            CancellationTokenSource = new CancellationTokenSource();
        }
    }
}