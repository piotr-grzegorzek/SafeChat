
namespace SafeChat
{
    public class CustomKeyExchangeService : KeyExchangeService
    {
        public override Task<string> DecryptSessionKey(string encryptedSessionKey) => throw new NotImplementedException();
        public override Task<string> EncryptSessionKey(string sessionKey) => throw new NotImplementedException();
        public override Task GenerateKeyPairs() => throw new NotImplementedException();
        public override Task<string> GenerateSessionKey() => throw new NotImplementedException();
        public override Task<string> GetPublicKey() => throw new NotImplementedException();
        public override Task SetRemotePublicKey(string publicKey) => throw new NotImplementedException();
    }
}