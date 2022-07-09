using Microsoft.Extensions.Configuration;
using P1ReaderApp.Interfaces;
using P1ReaderApp.Model;
using System.IO.Ports;

namespace P1ReaderApp.Services
{
    public class SerialPortWrapper :
        ISerialPort
    {
        private readonly SerialPort _serialPort;

        public SerialPortWrapper(
            IConfiguration config)
        {
            var p1Config = new P1Config();

            config.GetSection("P1Config").Bind(p1Config);

            _serialPort = new SerialPort(p1Config.Port, p1Config.BaudRate)
            {
                ReadTimeout = 20_000,
                Parity = (Parity)p1Config.Parity,
                DataBits = p1Config.DataBits,
                StopBits = (StopBits)p1Config.StopBits
            };
        }

        public bool IsOpen => _serialPort.IsOpen;

        public void Close()
        {
            _serialPort.Close();
        }

        public void Open()
        {
            _serialPort.Open();
        }

        public string ReadLine()
        {
            return _serialPort.ReadLine();
        }

        #region Dispoable
        // To detect redundant calls
        private bool _disposedValue;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose() => Dispose(true);

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(
            bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _serialPort.Dispose();
                }

                _disposedValue = true;
            }
        }
        #endregion
    }
}
