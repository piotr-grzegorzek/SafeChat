using System.Net.Sockets;

namespace SafeChat
{
    public class SecureMessageService : GenericMessageService
    {
        private readonly EncryptionService _encryptionService;
        private readonly SignatureService _signatureService;

        public SecureMessageService()
        {
            _encryptionService = new CustomEncryptionService();
            _signatureService = new CustomSignatureService();
        }
    }
}