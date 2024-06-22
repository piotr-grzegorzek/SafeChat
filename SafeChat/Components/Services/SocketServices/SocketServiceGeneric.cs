using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SafeChat
{
    public class SocketServiceGeneric : SocketService
    {
        public override event Action? ConnectionEstablished;
        public override event Action? ConnectionClosed;
        public override event Action<string>? MessageReceived;
        public override NetworkStream? Stream { get; set; }

        private TcpListener? _listener;
        private TcpClient? _client;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isStopping = false;

        public override async Task StartConnection(string role, string host, int port)
        {
            if (role == "server")
            {
                _listener = new TcpListener(IPAddress.Parse(host), port);
                _listener.Start();

                try
                {
                    _client = await _listener.AcceptTcpClientAsync();
                    Stream = _client.GetStream();

                    BeforeConnectionEstablishedInvoke(role);
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
                    Stream = _client.GetStream();

                    BeforeConnectionEstablishedInvoke(role);
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


        public override async Task SendMessage(string message)
        {
            if (Stream == null)
            {
                throw new InvalidOperationException("Connection is not established.");
            }
            await Stream.WriteAsync(Encoding.UTF8.GetBytes(message));
        }

        public override void Stop()
        {
            _isStopping = true;
            _cancellationTokenSource.Cancel();

            Stream?.Close();
            _client?.Close();
            _listener?.Stop();

            MessageReceived?.Invoke("Connection closed.");
            ConnectionClosed?.Invoke();
        }

        private async void StartReceivingMessages(CancellationToken token)
        {
            byte[] buffer = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await Stream!.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string message = BeforeMessageReceivedInvoke(receivedData);
                    MessageReceived?.Invoke(message);
                }
                catch (Exception)
                {
                    if (!_isStopping)
                    {
                        Stop();
                    }
                    break;
                }
            }

            if (!_isStopping)
            {
                MessageReceived?.Invoke("Connection closed.");
                ConnectionClosed?.Invoke();
            }
        }
    }
}