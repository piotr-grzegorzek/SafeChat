using System.Security.Cryptography;

namespace SafeChat
{
    public class EncryptionServiceAES : EncryptionService
    {
        public override Task<string> GenerateSessionKey()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                return Task.FromResult(Convert.ToBase64String(aes.Key));
            }
        }

        public override string Encrypt(string data, string key)
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("Data and key cannot be null or empty.");
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                aes.GenerateIV();
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(data);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public override string Decrypt(string data, string key)
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("Data and key cannot be null or empty.");
            }

            byte[] buffer = Convert.FromBase64String(data);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}