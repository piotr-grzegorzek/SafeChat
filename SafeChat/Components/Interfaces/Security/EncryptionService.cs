namespace SafeChat
{
    public abstract class EncryptionService
    {
        public abstract Task<string> GenerateSessionKey();
        public abstract string Encrypt(string data, string key);
        public abstract string Decrypt(string data, string key);
    }
}