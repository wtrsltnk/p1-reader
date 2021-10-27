namespace P1ReaderApp.Model
{
    public class P1Config
    {
        public string Port { get; set; }

        public int BaudRate { get; set; }

        public int StopBits { get; set; }

        public int DataBits { get; set; }

        public int Parity { get; set; }
    }
}
