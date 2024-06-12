namespace SafeChat
{
    public abstract class SignatureService
    {
        public abstract string SignData(string data);
        public abstract bool VerifySignature(string data, string signature);
    }
}