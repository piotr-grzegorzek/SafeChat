namespace SafeChat
{
    public abstract class KeyExchangeService
    {
        public abstract Task GenerateKeyPairs();
        public abstract Task<string> GetPublicKey();
        public abstract Task SetRemotePublicKey(string publicKey);
        public abstract Task<string> GenerateSessionKey();
        public abstract Task<string> EncryptSessionKey(string sessionKey);
        public abstract Task<string> DecryptSessionKey(string encryptedSessionKey);
    }
}