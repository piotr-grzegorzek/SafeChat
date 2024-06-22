using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class KeyExchangeServiceRSA
    {
        private readonly RSAParameters _privateKey;
        private readonly RSAParameters _publicKey;
        private RSAParameters _remotePublicKey;

        public KeyExchangeServiceRSA(RSAParameters privateKey, RSAParameters publicKey)
        {
            _privateKey = privateKey;
            _publicKey = publicKey;
        }

        public string GetPublicKey()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_publicKey);
                return Convert.ToBase64String(rsa.ExportRSAPublicKey());
            }
        }

        public void SetRemotePublicKey(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentNullException(nameof(publicKey), "Public key cannot be null or empty.");
            }

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                _remotePublicKey = rsa.ExportParameters(false);
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
    }
}