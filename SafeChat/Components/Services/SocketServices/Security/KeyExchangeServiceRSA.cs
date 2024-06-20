using System.Security.Cryptography;

namespace SafeChat
{
    public class KeyExchangeServiceRSA
    {
        public RSA? PrivateKey;
        public RSA? PublicKey;

        private RSA? _remotePublicKey;

        public Task GenerateKeyPairs()
        {
            PrivateKey = RSA.Create(2048);
            PublicKey = RSA.Create();
            PublicKey.ImportParameters(PrivateKey.ExportParameters(false));
            return Task.CompletedTask;
        }

        public static Task<string> GenerateSessionKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                return Task.FromResult(Convert.ToBase64String(aes.Key));
            }
        }

        public Task<string> GetPublicKey() => Task.FromResult(Convert.ToBase64String(PublicKey.ExportRSAPublicKey()));

        public Task SetRemotePublicKey(string publicKey)
        {
            _remotePublicKey = RSA.Create();
            _remotePublicKey.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            return Task.CompletedTask;
        }

        public Task<string> EncryptSessionKey(string sessionKey)
        {
            byte[] encryptedSessionKey = _remotePublicKey.Encrypt(Convert.FromBase64String(sessionKey), RSAEncryptionPadding.OaepSHA256);
            return Task.FromResult(Convert.ToBase64String(encryptedSessionKey));
        }

        public Task<string> DecryptSessionKey(string encryptedSessionKey)
        {
            byte[] decryptedSessionKey = PrivateKey.Decrypt(Convert.FromBase64String(encryptedSessionKey), RSAEncryptionPadding.OaepSHA256);
            return Task.FromResult(Convert.ToBase64String(decryptedSessionKey));
        }
    }
}
