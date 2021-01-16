using System;
using System.Collections.Generic;
using System.Text;

namespace MuCom
{
    internal class MuComFrame
    {
        #region Fields and properties

        public byte[] RawBuffer { get; private set; }

        public byte[] DataBytes { get; private set; }

        public byte ID { get; private set; }

        public MuComFrameDesc Description { get; private set; }

        #endregion

        #region Constructor

        public MuComFrame(byte[] inputBuffer)
        {
            if (inputBuffer is null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputBuffer.Length == 0) throw new ArgumentException("Input buffer contains no elements!");

            //Copy buffer
            this.RawBuffer = new byte[inputBuffer.Length];
            inputBuffer.CopyTo(this.RawBuffer, 0);

            //Decode header
            this.Description = MuComFrame.GetFrameDescriptionFromHeader(this.RawBuffer[0]);

            //Create buffer for data bytes and intermediate buffer
            byte[] buffer = new byte[MuComFrame.GetDataByteCountFromHeader(this.RawBuffer[0]) + 1];
            this.DataBytes = new byte[MuComFrame.GetDataByteCountFromHeader(this.RawBuffer[0])];

            //Reconstruct data from frame
            //Buffer[0..8] = Data bytes (first byte will be index)
            int dataPos = 0;
            int bytePos = 1;
            for (int i = 0; i < (this.DataBytes.Length + 1); i++)
            {
                if (bytePos < 0)
                {
                    bytePos = 6;
                    dataPos++;
                }
                buffer[i] = (byte)(this.RawBuffer[dataPos] << (7 - bytePos));
                dataPos++;
                if(dataPos < this.RawBuffer.Length)
                {
                    buffer[i] |= (byte)(this.RawBuffer[dataPos] >> bytePos);
                    bytePos--;
                }
            }

            //Copy data
            this.ID = buffer[0];
            Array.Copy(buffer, 1, this.DataBytes, 0, this.DataBytes.Length);
        }

        #endregion

        #region Methods

        public static int GetDataByteCountFromHeader(byte header)
        {
            return ((header & 0x1C) >> 2) + 1;
        }

        public static MuComFrameDesc GetFrameDescriptionFromHeader(byte header)
        {
            return (MuComFrameDesc)(header & 0x60);
        }

        public static bool IsHeaderByte(byte data)
        {
            if((data & 0x80) != 0x00)
            {
                return true;
            }
            return false;
        }

        #endregion
    }
}
