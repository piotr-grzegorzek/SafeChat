using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class KeyExchangeServiceRSA : KeyExchangeService
    {
        private RSAParameters _privateKey;
        private RSAParameters _publicKey;
        private RSAParameters _remotePublicKey;

        public KeyExchangeServiceRSA(RSAParameters privateKey, RSAParameters publicKey)
        {
            _privateKey = privateKey;
            _publicKey = publicKey;
        }

        public override Task<string> GetPublicKey()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_publicKey);
                return Task.FromResult(Convert.ToBase64String(rsa.ExportRSAPublicKey()));
            }
        }

        public override Task SetRemotePublicKey(string publicKey)
        {
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentNullException(nameof(publicKey), "Public key cannot be null or empty.");
            }
            try
            {
                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                    _remotePublicKey = rsa.ExportParameters(false);
                }
                return Task.CompletedTask;
            }
            catch (FormatException)
            {
                throw new ArgumentException("The provided public key is not a valid Base64 string.");
            }
        }

        public override Task<string> EncryptSessionKey(string sessionKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_remotePublicKey);
                byte[] encryptedKey = rsa.Encrypt(Encoding.UTF8.GetBytes(sessionKey), RSAEncryptionPadding.OaepSHA256);
                return Task.FromResult(Convert.ToBase64String(encryptedKey));
            }
        }

        public override Task<string> DecryptSessionKey(string encryptedSessionKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_privateKey);
                byte[] decryptedKey = rsa.Decrypt(Convert.FromBase64String(encryptedSessionKey), RSAEncryptionPadding.OaepSHA256);
                return Task.FromResult(Encoding.UTF8.GetString(decryptedKey));
            }
        }
    }
}