namespace SafeChat
{
    public class CustomEncryptionService : EncryptionService
    {
        public override string Encrypt(string data, string key) => throw new NotImplementedException();

        public override string Decrypt(string data, string key) => throw new NotImplementedException();

    }
}