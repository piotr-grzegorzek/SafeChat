
namespace SafeChat
{
    public class SocketServiceSecurityDecorator : SocketServiceDecorator
    {
        private readonly KeyExchangeService _keyExchangeService;
        private readonly EncryptionService _encryptionService;
        private readonly SignatureService _signatureService;

        public SocketServiceSecurityDecorator(SocketService socketService) : base(socketService)
        {
            _keyExchangeService = new KeyExchangeServiceRSA();
            _encryptionService = new EncryptionServiceAES();
            _signatureService = new SignatureServiceRSA();
        }
    }
}
