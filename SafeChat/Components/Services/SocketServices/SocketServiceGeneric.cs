using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class SocketService
    {
        public event Action? ConnectionEstablished;
        public event Action? ConnectionClosed;
        public event Action<string>? MessageReceived;

        private TcpListener? _server;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isStopping = false;

        private readonly KeyExchangeServiceRSA _keyExchangeService = new KeyExchangeServiceRSA();
        private readonly EncryptionServiceAES _encryptionService = new EncryptionServiceAES();
        private readonly SignatureServiceRSA _signatureService;
        private string? _sessionKey;

        public SocketService()
        {
            _keyExchangeService.GenerateKeyPairs().Wait();
            _signatureService = new SignatureServiceRSA(_keyExchangeService.PrivateKey, _keyExchangeService.PublicKey);
        }

        public async Task StartConnection(string role, string host, int port)
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
                    await HandleKeyExchange();
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
                    await HandleKeyExchange();
                    _ = Task.Run(() => StartReceivingMessages(_cancellationTokenSource.Token));
                }
                catch (Exception)
                {
                    Stop();
                    throw;
                }
            }
        }

        private async Task HandleKeyExchange()
        {
            string role = _server != null ? "server" : "client";

            if (role == "client")
            {
                string publicKey = await _keyExchangeService.GetPublicKey();
                await SendMessage(publicKey);
                string serverPublicKey = await ReceiveMessage();
                await _keyExchangeService.SetRemotePublicKey(serverPublicKey);

                _sessionKey = await KeyExchangeServiceRSA.GenerateSessionKey();
                string encryptedSessionKey = await _keyExchangeService.EncryptSessionKey(_sessionKey);
                string sessionKeyHash = ComputeHash(_sessionKey);
                string sessionKeySignature = _signatureService.SignData(_sessionKey);

                await SendMessage($"{encryptedSessionKey}.{sessionKeyHash}.{sessionKeySignature}");
            }
            else if (role == "server")
            {
                string clientPublicKey = await ReceiveMessage();
                await _keyExchangeService.SetRemotePublicKey(clientPublicKey);

                string serverPublicKey = await _keyExchangeService.GetPublicKey();
                await SendMessage(serverPublicKey);

                string[] sessionKeyParts = (await ReceiveMessage()).Split('.');
                string encryptedSessionKey = sessionKeyParts[0];
                string sessionKeyHash = sessionKeyParts[1];
                string sessionKeySignature = sessionKeyParts[2];

                string decryptedSessionKey = await _keyExchangeService.DecryptSessionKey(encryptedSessionKey);

                if (ComputeHash(decryptedSessionKey) != sessionKeyHash ||
                    !_signatureService.VerifySignature(decryptedSessionKey, sessionKeySignature))
                {
                    throw new Exception("Session key verification failed.");
                }

                _sessionKey = decryptedSessionKey;
            }
        }

        private static string ComputeHash(string data)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        private async Task StartReceivingMessages(CancellationToken token)
        {
            byte[] buffer = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await _stream!.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string decryptedMessage = EncryptionServiceAES.Decrypt(message, _sessionKey);
                        MessageReceived?.Invoke(decryptedMessage);
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

        public async Task SendMessage(string message)
        {
            if (_stream != null && _stream.CanWrite)
            {
                string encryptedMessage = EncryptionServiceAES.Encrypt(message, _sessionKey);
                byte[] data = Encoding.UTF8.GetBytes(encryptedMessage);
                await _stream.WriteAsync(data);
            }
        }

        public void Stop()
        {
            _isStopping = true;
            _cancellationTokenSource.Cancel();

            _stream?.Close();
            _client?.Close();
            _server?.Stop();

            ConnectionClosed?.Invoke();
        }
    }
}