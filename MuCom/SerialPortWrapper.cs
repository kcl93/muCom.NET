using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Text;

namespace MuCom
{
    internal class SerialPortWrapper : ISerial
    {
        #region Fields and properties

        private SerialPort serial = null;

        public int ReadTimeout { get; set; }

        public int WriteTimeout { get; set; }

        #endregion

        #region Events

        public event DataReceivedEventHandler DataReceived;

        #endregion

        #region Constructor

        internal SerialPortWrapper(string portName, int baudrate, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            this.serial = new SerialPort(portName, baudrate, parity, 8, stopBits);

            this.serial.ReceivedBytesThreshold = 1;

            this.serial.DataReceived += new SerialDataReceivedEventHandler(this.SerialDataReceivedHandler);
        }

        #endregion

        #region Methods

        public void Open() => this.serial.Open();

        public void Close() => this.serial.Close();

        public int Available() => this.serial.BytesToRead;

        public byte ReadByte() => (byte)this.serial.ReadByte();

        public void WriteBytes(byte[] data) => this.serial.Write(data, 0, data.Length);

        private void SerialDataReceivedHandler(object obj, SerialDataReceivedEventArgs e)
        {
            var localCopy = (DataReceivedEventHandler)this.DataReceived.Clone();

            if(localCopy != null)
            {
                localCopy(this.serial);
            }
        }

        #endregion
    }
}
