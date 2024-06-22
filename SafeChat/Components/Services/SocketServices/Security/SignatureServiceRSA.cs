using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class SignatureServiceRSA : SignatureService
    {
        private readonly RSAParameters _privateKey;
        private RSAParameters _remotePublicKey;

        public SignatureServiceRSA(RSAParameters privateKey)
        {
            _privateKey = privateKey;
        }

        public override string SignData(string data)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_privateKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Convert.ToBase64String(signature);
            }
        }

        public override bool VerifySignature(string data, string signature)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_remotePublicKey);
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        public Task SetRemotePublicKey(string publicKey)
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
            return Task.CompletedTask;
        }
    }
}