using P1ReaderApp.Interfaces;
using System;
using System.Threading;

namespace P1ReaderApp.Services
{
    public class FakeSerialPort :
        ISerialPort
    {
        public bool IsOpen { get; private set; } = false;

        public void Close()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Port already closed");
            }

            IsOpen = false;
        }

        public void Open()
        {
            if (IsOpen)
            {
                throw new InvalidOperationException("Port already open");
            }

            IsOpen = true;
        }

        static int cursor = -1;
        static Random random = new Random();
        string[] lines = null;
        double a = 1234.567;
        double b = 1234.567;
        double c = 1234.567;
        double d = 1234.567;
        public string ReadLine()
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("Port not open");
            }

            cursor++;

            if (lines == null || cursor >= lines.Length)
            {
                a += random.NextDouble() * 5;
                b += random.NextDouble() * 6;
                c += random.NextDouble() * 7;
                d += random.NextDouble() * 8;
                
                lines = string.Format(
                    message, a, b, c, d).Split(Environment.NewLine);

                cursor = 0;
            }

            Thread.Sleep(random.Next(10) + 20);

            return lines[cursor];
        }


        private string message = @"/ISk5\2MT382-1000
1-3:0.2.8(50)
0-0:1.0.0(101209113020W)
0-0:96.1.1(4B384547303034303436333935353037)
1-0:1.8.1({0}*kWh)
1-0:1.8.2({1}*kWh)
1-0:2.8.1({2}*kWh)
1-0:2.8.2({3}*kWh)
0-0:96.14.0(0002)
1-0:1.7.0(01.193*kW)
1-0:2.7.0(00.000*kW)
0-0:96.7.21(00004)
0-0:96.7.9(00002)
1-0:99.97.0(2)(0-0:96.7.19)(101208152415W)(0000000240*s)(101208151004W)(0000000301*s)
1-0:32.32.0(00002)
1-0:52.32.0(00001)
1-0:72.32.0(00000)
1-0:32.36.0(00000)
1-0:52.36.0(00003)
1-0:72.36.0(00000)
0-
0:96.13.0(303132333435363738393A3B3C3D3E3F303132333435363738393A3B3C3D3E3F303132333435363738393A3B3C
3D3E3F303132333435363738393A3B3C3D3E3F303132333435363738393A3B3C3D3E3F)
1-0:32.7.0(220.1*V)
1-0:52.7.0(220.2*V)
1-0:72.7.0(220.3*V)
1-0:31.7.0(001*A)
1-0:51.7.0(002*A)
1-0:71.7.0(003*A)
1-0:21.7.0(01.111*kW)
1-0:41.7.0(02.222*kW)
1-0:61.7.0(03.333*kW)
1-0:22.7.0(04.444*kW)
1-0:42.7.0(05.555*kW)
1-0:62.7.0(06.666*kW)
0-1:24.1.0(003)
0-1:96.1.0(3232323241424344313233343536373839)
0-1:24.2.1(101209112500W)(12785.123*m3)
!EF2F";

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
                    if (IsOpen) Close();
                }

                _disposedValue = true;
            }
        }
        #endregion
    }
}
