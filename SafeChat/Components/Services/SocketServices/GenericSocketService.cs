using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SafeChat
{
    public class GenericSocketService : SocketService
    {
        public override event Action? ConnectionEstablished;
        public override event Action? ConnectionClosed;
        public override event Action<string>? MessageReceived;

        private TcpListener? _server;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isStopping = false;

        public override async Task StartConnection(string role, string host, int port)
        {
            if (role == "server")
            {
                _server = new TcpListener(IPAddress.Parse(host), port);
                _server.Start();

                try
                {
                    _client = await _server.AcceptTcpClientAsync();
                    _stream = _client.GetStream();
                    ConnectionEstablished?.Invoke();
                    _ = Task.Run(() => StartReceivingMessages(_cancellationTokenSource.Token));
                }
                catch (Exception)
                {
                    Stop();
                    throw;
                }
            }
            else if (role == "client")
            {
                _client = new TcpClient();
                try
                {
                    await _client.ConnectAsync(host, port);
                    _stream = _client.GetStream();
                    ConnectionEstablished?.Invoke();
                    _ = Task.Run(() => StartReceivingMessages(_cancellationTokenSource.Token));
                }
                catch (Exception)
                {
                    Stop();
                    throw;
                }
            }
        }

        private async Task StartReceivingMessages(CancellationToken token)
        {
            byte[] buffer = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, token);
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

            if (!_isStopping)
            {
                ConnectionClosed?.Invoke();
            }
        }

        public override async Task SendMessage(string message)
        {
            if (_stream != null && _stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);
            }
        }

        public override void Stop()
        {
            try
            {
                _isStopping = true;
                _cancellationTokenSource.Cancel();
                if (_stream != null && _stream.CanWrite)
                {
                    byte[] data = Encoding.UTF8.GetBytes("Connection closed.");
                    _stream.Write(data, 0, data.Length);
                }
                _stream?.Close();
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
            _stream = null;
            _isStopping = false;
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }
}
