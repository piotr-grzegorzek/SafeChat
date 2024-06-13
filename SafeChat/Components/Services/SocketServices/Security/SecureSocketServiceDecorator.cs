
namespace SafeChat
{
    public class SecureSocketServiceDecorator : SocketServiceDecorator
    {
        private readonly KeyExchangeService _keyExchangeService;
        private readonly EncryptionService _encryptionService;
        private readonly SignatureService _signatureService;

        public SecureSocketServiceDecorator(SocketService socketService) : base(socketService)
        {
            _keyExchangeService = new CustomKeyExchangeService();
            _encryptionService = new CustomEncryptionService();
            _signatureService = new CustomSignatureService();
        }
    }
}
