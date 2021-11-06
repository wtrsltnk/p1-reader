using P1ReaderApp.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace P1ReaderApp.Services
{
    public class MessageBuffer<TMessage> :
        IMessageBuffer<TMessage>
    {
        private readonly BufferBlock<TMessage> _buffer = new();

        private readonly List<Func<TMessage, Task>> _messageHandlers = new();

        public MessageBuffer()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var message = await _buffer.ReceiveAsync();

                    Parallel.ForEach(_messageHandlers, async (x) =>
                    {
                        await x(message);
                    });
                }
            });
        }

        public async Task QueueMessage(
            TMessage message,
            CancellationToken cancellationToken)
        {
            await _buffer.SendAsync(message, cancellationToken);
        }

        public void RegisterMessageHandler(Func<TMessage, Task> action)
        {
            _messageHandlers.Add(action);
        }
    }
}