using System.Security.Cryptography;
using System.Text;

namespace SafeChat
{
    public class EncryptionServiceAES
    {
        public static string Encrypt(string data, string key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                {
                    byte[] plainText = Encoding.UTF8.GetBytes(data);
                    byte[] cipherText = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);

                    byte[] result = new byte[iv.Length + cipherText.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(cipherText, 0, result, iv.Length, cipherText.Length);

                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string Decrypt(string data, string key)
        {
            byte[] fullCipher = Convert.FromBase64String(data);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(key);
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] cipher = new byte[fullCipher.Length - iv.Length];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] plainText = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(plainText);
                }
            }
        }
    }
}