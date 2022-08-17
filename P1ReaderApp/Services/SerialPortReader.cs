using P1ReaderApp.Interfaces;
using P1ReaderApp.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace P1ReaderApp.Services
{
    public class SerialPortReader :
        IDisposable
    {
        private readonly IMessageBuffer<P1MessageCollection> _messageBuffer;
        private readonly ILogger _logger;
        private readonly ISerialPort _serialPort;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposedValue = false;

        public SerialPortReader(
            ISerialPort serialPort,
            IMessageBuffer<P1MessageCollection> messageBufferService,
            ILogger logger)
        {
            _serialPort = serialPort;
            _messageBuffer = messageBufferService;
            _logger = logger;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            _logger.Information("Disposing SerialPortReader");

            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void StartReading()
        {
            if (!_serialPort.IsOpen)
            {
                _cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    _logger.Information("Opening serial port");

                    _serialPort.Open();

                    _logger.Information("Starting read task");

                    CreateReadTask(_cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    _logger.Fatal(exception, "Error during serial port read");
                }
            }
            else
            {
                _logger.Error("Cannot read serial port: already opened");
            }
        }

        public void StopReading()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        protected virtual void Dispose(
            bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Cancel();
                    _serialPort.Close();
                }

                _disposedValue = true;
            }
        }

        private void CreateReadTask(
            CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Read(cancellationToken);
                    }
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error during read");
                }
            }, cancellationToken);
        }

        private async Task Read(
            CancellationToken cancellationToken)
        {
            try
            {
                var messages = new List<string>();

                while (true)
                {
                    var message = _serialPort.ReadLine();

                    messages.Add(message);

                    if (message.StartsWith("!"))
                    {
                        break;
                    }
                }

                await _messageBuffer
                    .QueueMessage(new P1MessageCollection
                    {
                        Messages = messages,
                        ReceivedUtc = DateTime.UtcNow,
                    }, cancellationToken);

                if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                {
                    foreach (var message in messages)
                    {
                        _logger.Verbose("Serial message:{message}", message);
                    }
                }
            }
            catch (TimeoutException exc)
            {
                _logger.Debug("Timeout exception {message}", exc.Message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unexpected exception during serial read");
            }
        }
    }
}