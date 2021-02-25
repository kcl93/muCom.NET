using System.IO.Ports;

namespace MuCom
{
    internal class SerialPortWrapper : ISerial
    {
        #region Fields and properties

        private SerialPort Serial { get; }

        public int ReadTimeout { get; set; }

        public int WriteTimeout { get; set; }

        #endregion

        #region Events

        public event DataReceivedEventHandler DataReceived;

        #endregion

        #region Constructor

        internal SerialPortWrapper(string portName, int baudrate, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            this.Serial = new SerialPort(portName, baudrate, parity, 8, stopBits)
            {
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true,
                ReceivedBytesThreshold = 1
            };

            this.Serial.DataReceived += new SerialDataReceivedEventHandler(this.SerialDataReceivedHandler);
        }

        #endregion

        #region Methods

        public void Open() => this.Serial.Open();

        public void Close()
        {
            this.Serial.DiscardInBuffer();
            this.Serial.Close();
        }

        public int Available() => this.Serial.BytesToRead;

        public byte Read() => (byte)this.Serial.ReadByte();

        public void Write(byte[] data) => this.Serial.Write(data, 0, data.Length);

        private void SerialDataReceivedHandler(object obj, SerialDataReceivedEventArgs e)
        {
            ((DataReceivedEventHandler)this.DataReceived.Clone())?.Invoke(this.Serial);
        }

        #endregion
    }
}
