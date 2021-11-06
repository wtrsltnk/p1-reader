using System;
using System.Threading;
using System.Threading.Tasks;

namespace P1ReaderApp.Interfaces
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