using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class SocketServiceSecurityDecorator : SocketServiceDecorator
    {
        private RSAParameters _privateKey;
        private RSAParameters _publicKey;
        private RSAParameters _remotePublicKey;
        private string _sessionKey = string.Empty;

        public SocketServiceSecurityDecorator(SocketService socketService, RSAParameters privateKey, RSAParameters publicKey) : base(socketService)
        {
            _privateKey = privateKey;
            _publicKey = publicKey;
        }
        public override string BeforeMessageReceivedInvoke(string message) => EncryptionServiceAES.Decrypt(message, _sessionKey!);

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
                    base.ConnectionEstablished?.Invoke();
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
        public override void BeforeConnectionEstablishedInvoke(string role)
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

        private async void PerformKeyExchangeAsServer()
        {
            SetRemotePublicKey(ReceiveSingleMessage());   //2
            await SendMessageGeneric(GetPublicKey());  // 3

            string encryptedSessionKey = ReceiveSingleMessage();    //6
            await SendMessageGeneric("session key recv");
            string sessionKeyHash = ReceiveSingleMessage(); //8
            await SendMessageGeneric("hash recv");
            string sessionKeySignature = ReceiveSingleMessage();    //10
            await SendMessageGeneric("signature recv");

            _sessionKey = DecryptSessionKey(encryptedSessionKey);
            string calculatedHash = CalculateHash(_sessionKey);

            //11
            if (calculatedHash != sessionKeyHash || !VerifySignature(_sessionKey, sessionKeySignature))
            {
                await SendMessageGeneric("NOT OK");
                throw new InvalidOperationException("Session key verification failed.");
            }
            else
            {
                await SendMessageGeneric("OK");
            }
        }

        private async void PerformKeyExchangeAsClient()
        {
            await SendMessageGeneric(GetPublicKey());  //1  
            SetRemotePublicKey(ReceiveSingleMessage());  //4

            _sessionKey = EncryptionServiceAES.GenerateSessionKey();
            System.Diagnostics.Debug.WriteLine("********** Unencrypted session key **********");
            System.Diagnostics.Debug.WriteLine(_sessionKey);

            string encryptedSessionKey = EncryptSessionKey(_sessionKey);
            System.Diagnostics.Debug.WriteLine("********** Encrypted session key **********");
            System.Diagnostics.Debug.WriteLine(encryptedSessionKey);

            string sessionKeyHash = CalculateHash(_sessionKey);
            string sessionKeySignature = SignData(_sessionKey);

            await SendMessageGeneric(encryptedSessionKey);  //5
            if (ReceiveSingleMessage() == "session key recv")
            {
                await SendMessageGeneric(sessionKeyHash);   //7
                if (ReceiveSingleMessage() == "hash recv")
                {
                    await SendMessageGeneric(sessionKeySignature);  //9
                    if (ReceiveSingleMessage() == "signature recv")
                    {
                        string serverResponse = ReceiveSingleMessage(); // 12
                        if (serverResponse != "OK")
                        {
                            throw new InvalidOperationException("Server response is not OK.");
                        }
                    }
                }
            }
        }

        private string ReceiveSingleMessage()
        {
            byte[] buffer = new byte[1024];
            int bytesRead = Stream!.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }

        private async Task SendMessageGeneric(string message) => await base.SendMessage(message);

        private string EncryptSessionKey(string sessionKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_remotePublicKey);
                byte[] encryptedKey = rsa.Encrypt(Encoding.UTF8.GetBytes(sessionKey), RSAEncryptionPadding.OaepSHA256);
                return Convert.ToBase64String(encryptedKey);
            }
        }

        private string DecryptSessionKey(string encryptedSessionKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_privateKey);
                byte[] decryptedKey = rsa.Decrypt(Convert.FromBase64String(encryptedSessionKey), RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(decryptedKey);
            }
        }

        private string GetPublicKey()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_publicKey);
                return Convert.ToBase64String(rsa.ExportRSAPublicKey());
            }
        }

        private void SetRemotePublicKey(string publicKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                _remotePublicKey = rsa.ExportParameters(false);
            }
        }

        private string CalculateHash(string data)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        public override async Task SendMessage(string message)
        {
            if (Stream == null)
            {
                throw new InvalidOperationException("Connection is not established.");
            }
            message = EncryptionServiceAES.Encrypt(message, _sessionKey!);
            await Stream.WriteAsync(Encoding.UTF8.GetBytes(message));
        }

        private string SignData(string data)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_privateKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signature);
            }
        }

        private bool VerifySignature(string data, string signature)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_remotePublicKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }
    }
}
