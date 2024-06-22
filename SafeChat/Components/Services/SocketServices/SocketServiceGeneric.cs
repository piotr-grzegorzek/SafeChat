using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class SocketServiceGeneric
    {
        public event Action? ConnectionEstablished;
        public event Action? ConnectionClosed;
        public event Action<string>? MessageReceived;

        private TcpListener? _listener;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isStopping = false;

        private readonly KeyExchangeServiceRSA _keyExchangeService;
        private readonly SignatureServiceRSA _signatureService;
        private readonly EncryptionServiceAES _encryptionService = new EncryptionServiceAES();
        private string _sessionKey = string.Empty;

        public SocketServiceGeneric(RSAParameters privateKey, RSAParameters publicKey)
        {
            _keyExchangeService = new KeyExchangeServiceRSA(privateKey, publicKey);
            _signatureService = new SignatureServiceRSA(privateKey);
        }

        public async Task StartConnection(string role, string host, int port)
        {
            if (role == "server")
            {
                _listener = new TcpListener(IPAddress.Parse(host), port);
                _listener.Start();

                try
                {
                    _client = await _listener.AcceptTcpClientAsync();
                    _stream = _client.GetStream();

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
                    _stream = _client.GetStream();

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

        public async Task SendMessage(string message, bool encrypt = true)
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

        public void Stop()
        {
            _isStopping = true;
            _cancellationTokenSource.Cancel();

            _stream?.Close();
            _client?.Close();
            _listener?.Stop();

            MessageReceived?.Invoke("Connection closed.");
            ConnectionClosed?.Invoke();
        }

        private void BeforeConnectionEstablishedInvoke(string role)
        {
            if (role == "server")
            {
                PerformKeyExchangeAsServer();
            }
            else if (role == "client")
            {
                PerformKeyExchangeAsClient();
            }
        }

        private async void StartReceivingMessages(CancellationToken token)
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
                    string message = BeforeNormalMessageReceivedInvoke(receivedData);
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

        private string BeforeNormalMessageReceivedInvoke(string message) => _encryptionService.Decrypt(message, _sessionKey!);

        private async void PerformKeyExchangeAsServer()
        {
            string clientPublicKey = ReceiveMessage();    // 2
            _keyExchangeService.SetRemotePublicKey(clientPublicKey);
            _signatureService.SetRemotePublicKey(clientPublicKey);
            string serverPublicKey = _keyExchangeService.GetPublicKey();
            await SendMessage(serverPublicKey, false);  // 3

            string encryptedSessionKey = ReceiveMessage();    //6
            await SendMessage("session key recv", false);
            string sessionKeyHash = ReceiveMessage(); //8
            await SendMessage("hash recv", false);
            string sessionKeySignature = ReceiveMessage();    //10
            await SendMessage("signature recv", false);

            _sessionKey = _keyExchangeService.DecryptSessionKey(encryptedSessionKey);
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

        private async void PerformKeyExchangeAsClient()
        {
            string clientPublicKey = _keyExchangeService.GetPublicKey();
            await SendMessage(clientPublicKey, false);  //1
            string serverPublicKey = ReceiveMessage();    //4
            _keyExchangeService.SetRemotePublicKey(serverPublicKey);
            _signatureService.SetRemotePublicKey(clientPublicKey);

            _sessionKey = _encryptionService.GenerateSessionKey();
            System.Diagnostics.Debug.WriteLine("********** Unencrypted session key **********");
            System.Diagnostics.Debug.WriteLine(_sessionKey);
            string encryptedSessionKey = _keyExchangeService.EncryptSessionKey(_sessionKey);
            System.Diagnostics.Debug.WriteLine("********** Encrypted session key **********");
            System.Diagnostics.Debug.WriteLine(encryptedSessionKey);
            string sessionKeyHash = CalculateHash(_sessionKey);
            string sessionKeySignature = _signatureService.SignData(_sessionKey);

            await SendMessage(encryptedSessionKey, false);  //5
            if (ReceiveMessage() == "session key recv")
            {
                await SendMessage(sessionKeyHash, false);   //7
                if (ReceiveMessage() == "hash recv")
                {
                    await SendMessage(sessionKeySignature, false);  //9
                    if (ReceiveMessage() == "signature recv")
                    {
                        string serverResponse = ReceiveMessage(); // 12
                        if (serverResponse != "OK")
                        {
                            throw new InvalidOperationException("Server response is not OK.");
                        }
                    }
                }
            }
        }

        private string ReceiveMessage()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = _stream!.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        private string CalculateHash(string data)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }
    }
}