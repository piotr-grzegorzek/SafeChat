using System;
using System.Threading.Tasks;

public interface IMessageService
{
    event Action<string>? MessageReceived;

    Task SendMessage(string message);
}