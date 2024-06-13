
namespace SafeChat
{
    public class SecureSocketServiceDecorator : SocketServiceDecorator
    {
        public SecureSocketServiceDecorator(SocketService socketService) : base(socketService) { }
    }
}
