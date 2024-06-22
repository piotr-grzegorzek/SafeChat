using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class SocketServiceGeneric : SocketService
    {
        public override event Action? ConnectionEstablished;
        public override event Action? ConnectionClosed;
        public override event Action<string>? MessageReceived;

        private TcpListener? _server;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isStopping = false;

        private readonly KeyExchangeServiceRSA _keyExchangeService;
        private readonly EncryptionServiceAES _encryptionService = new EncryptionServiceAES();
        private readonly SignatureServiceRSA _signatureService;
        private string _sessionKey = string.Empty;

        public SocketServiceGeneric(RSAParameters privateKey, RSAParameters publicKey)
        {
            _keyExchangeService = new KeyExchangeServiceRSA(privateKey, publicKey);
            _signatureService = new SignatureServiceRSA(privateKey, publicKey);
        }

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
                    await PerformKeyExchangeAsServer();
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
                    await PerformKeyExchangeAsClient();
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

        private async Task PerformKeyExchangeAsServer()
        {
            string clientPublicKey = await ReceiveMessage();    // 2
            await _keyExchangeService.SetRemotePublicKey(clientPublicKey);
            string serverPublicKey = await _keyExchangeService.GetPublicKey();
            await SendMessage(serverPublicKey, false);  // 3

            string encryptedSessionKey = await ReceiveMessage();    //6
            string sessionKeyHash = await ReceiveMessage(); //8
            string sessionKeySignature = await ReceiveMessage();    //10

            _sessionKey = await _keyExchangeService.DecryptSessionKey(encryptedSessionKey);
            string calculatedHash = CalculateHash(_sessionKey);

            //11
            if (calculatedHash != sessionKeyHash || !_signatureService.VerifySignature(_sessionKey, sessionKeySignature))
            {
                await SendMessage("NOT OK", false);
                throw new InvalidOperationException("Session key verification failed.");
            }
            else
            {
                await SendMessage("OK", false);
            }
        }

        private async Task PerformKeyExchangeAsClient()
        {
            string clientPublicKey = await _keyExchangeService.GetPublicKey();
            await SendMessage(clientPublicKey, false);  //1
            string serverPublicKey = await ReceiveMessage();    //4
            await _keyExchangeService.SetRemotePublicKey(serverPublicKey);

            _sessionKey = await _keyExchangeService.GenerateSessionKey();
            string encryptedSessionKey = await _keyExchangeService.EncryptSessionKey(_sessionKey);
            string sessionKeyHash = CalculateHash(_sessionKey);
            string sessionKeySignature = _signatureService.SignData(_sessionKey);

            await SendMessage(encryptedSessionKey, false);  //5
            await SendMessage(sessionKeyHash, false);   //7
            await SendMessage(sessionKeySignature, false);  //9
            string serverResponse = await ReceiveMessage(); // 12
            if (serverResponse != "OK")
            {
                throw new InvalidOperationException("Server response is not OK.");
            }
        }

        private string CalculateHash(string data)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        private async Task StartReceivingMessages(CancellationToken token)
        {
            byte[] buffer = new byte[1024];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await _stream!.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string decryptedMessage = _encryptionService.Decrypt(receivedData, _sessionKey!);
                    MessageReceived?.Invoke(decryptedMessage);
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
        }

        public override async Task SendMessage(string message, bool encrypt = true)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Connection is not established.");
            }
            byte[] data;
            if (encrypt)
            {
                string encryptedMessage = _encryptionService.Encrypt(message, _sessionKey!);
                data = Encoding.UTF8.GetBytes(encryptedMessage);
            }
            else
            {
                data = Encoding.UTF8.GetBytes(message);
            }
            await _stream.WriteAsync(data);
        }

        public override void Stop()
        {
            _isStopping = true;
            _cancellationTokenSource.Cancel();

            _stream?.Close();
            _client?.Close();
            _server?.Stop();

            ConnectionClosed?.Invoke();
        }

        private async Task<string> ReceiveMessage()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await _stream!.ReadAsync(buffer.AsMemory(0, buffer.Length));
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }
}