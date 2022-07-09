using System;

namespace P1ReaderApp.Interfaces
{
    public interface ISerialPort :
        IDisposable
    {
        bool IsOpen { get; }

        void Open();

        void Close();

        string ReadLine();
    }
}
