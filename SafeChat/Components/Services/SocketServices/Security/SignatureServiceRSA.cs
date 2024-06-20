using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class SignatureServiceRSA
    {
        private readonly RSA _privateKey;
        private readonly RSA _publicKey;

        public SignatureServiceRSA(RSA privateKey, RSA publicKey)
        {
            _privateKey = privateKey;
            _publicKey = publicKey;
        }

        public string SignData(string data)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            byte[] signature = _privateKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }

        public bool VerifySignature(string data, string signature)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            return _publicKey.VerifyHash(hash, Convert.FromBase64String(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}
