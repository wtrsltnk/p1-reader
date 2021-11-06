using System;
using System.Threading;
using System.Threading.Tasks;

namespace P1ReaderApp.Services
{
    public interface IMessageBuffer<TMessage>
    {
        Task QueueMessage(
            TMessage message, 
            CancellationToken cancellationToken);

        void RegisterMessageHandler(
            Func<TMessage, Task> action);
    }
}