namespace SafeChat
{
    public class SignatureServiceRSA : SignatureService
    {
        public override string SignData(string data) => throw new NotImplementedException();
        public override bool VerifySignature(string data, string signature) => throw new NotImplementedException();
    }
}