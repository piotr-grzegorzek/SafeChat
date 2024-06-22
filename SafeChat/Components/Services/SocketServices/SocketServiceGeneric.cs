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

        RSAParameters _privateKey;

        RSAParameters _publicKey;
        public string GetPublicKey()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_publicKey);
                return Convert.ToBase64String(rsa.ExportRSAPublicKey());
            }
        }

        RSAParameters _remotePublicKey;
        public void SetRemotePublicKey(string publicKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                _remotePublicKey = rsa.ExportParameters(false);
            }
        }

        private string _sessionKey = string.Empty;

        public SocketServiceGeneric(RSAParameters privateKey, RSAParameters publicKey)
        {
            _privateKey = privateKey;
            _publicKey = publicKey;
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
                string encryptedMessage = EncryptionServiceAES.Encrypt(message, _sessionKey!);
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

        private string BeforeNormalMessageReceivedInvoke(string message) => EncryptionServiceAES.Decrypt(message, _sessionKey!);

        private async void PerformKeyExchangeAsServer()
        {
            SetRemotePublicKey(ReceiveMessage());   //2
            await SendMessage(GetPublicKey(), false);  // 3

            string encryptedSessionKey = ReceiveMessage();    //6
            await SendMessage("session key recv", false);
            string sessionKeyHash = ReceiveMessage(); //8
            await SendMessage("hash recv", false);
            string sessionKeySignature = ReceiveMessage();    //10
            await SendMessage("signature recv", false);

            _sessionKey = DecryptSessionKey(encryptedSessionKey);
            string calculatedHash = CalculateHash(_sessionKey);

            //11
            if (calculatedHash != sessionKeyHash || !VerifySignature(_sessionKey, sessionKeySignature))
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
            await SendMessage(GetPublicKey(), false);  //1  
            SetRemotePublicKey(ReceiveMessage());  //4

            _sessionKey = EncryptionServiceAES.GenerateSessionKey();
            System.Diagnostics.Debug.WriteLine("********** Unencrypted session key **********");
            System.Diagnostics.Debug.WriteLine(_sessionKey);

            string encryptedSessionKey = EncryptSessionKey(_sessionKey);
            System.Diagnostics.Debug.WriteLine("********** Encrypted session key **********");
            System.Diagnostics.Debug.WriteLine(encryptedSessionKey);

            string sessionKeyHash = CalculateHash(_sessionKey);
            string sessionKeySignature = SignData(_sessionKey);

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

        public string EncryptSessionKey(string sessionKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_remotePublicKey);
                byte[] encryptedKey = rsa.Encrypt(Encoding.UTF8.GetBytes(sessionKey), RSAEncryptionPadding.OaepSHA256);
                return Convert.ToBase64String(encryptedKey);
            }
        }

        public string DecryptSessionKey(string encryptedSessionKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_privateKey);
                byte[] decryptedKey = rsa.Decrypt(Convert.FromBase64String(encryptedSessionKey), RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(decryptedKey);
            }
        }

        public string SignData(string data)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_privateKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signature);
            }
        }

        public bool VerifySignature(string data, string signature)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_remotePublicKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        private string ReceiveMessage()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = _stream!.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        private static string CalculateHash(string data)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }
    }
}