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

        public int DataCount { get; private set; }

        public byte ID { get; private set; }

        public MuComFrameDesc Description { get; private set; }

        #endregion

        #region Constructor

        public MuComFrame(byte[] inputBuffer)
        {
            if (inputBuffer is null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputBuffer.Length < 2) throw new ArgumentException("Input buffer must contain at least two elements");

            //Copy buffer
            this.RawBuffer = new byte[inputBuffer.Length];
            inputBuffer.CopyTo(this.RawBuffer, 0);

            //Decode header
            this.Description = MuComFrame.GetFrameDescriptionFromHeader(this.RawBuffer[0]);
            this.ID = MuComFrame.GetIdFromFrame(this.RawBuffer);
            this.DataCount = MuComFrame.GetDataByteCountFromHeader(this.RawBuffer[0]);

            //A read request frame is only two bytes long and does not 
            if (this.Description == MuComFrameDesc.ReadRequest)
            {
                this.DataBytes = new byte[0];
                return;
            }

            //Check input frame size
            int expectedLength = MuComFrame.GetFrameSizeFromDataCount(this.Description, this.DataCount);
            if (inputBuffer.Length != expectedLength)
            {
                throw new ArgumentException("Input buffer size mismatch! " + expectedLength.ToString() + " bytes expected but input buffer contains " + inputBuffer.Length.ToString() + ".");
            }

            //Create buffer for data bytes and intermediate buffer
            byte[] buffer = new byte[this.DataCount + 1];
            this.DataBytes = new byte[this.DataCount];
            
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
                buffer[i] |= (byte)(this.RawBuffer[dataPos] >> bytePos);
                bytePos--;
            }

            //Copy data
            this.ID = buffer[0];
            Array.Copy(buffer, 1, this.DataBytes, 0, this.DataBytes.Length);
        }

        public MuComFrame(MuComFrameDesc description, byte ID, int dataCount, byte[] data)
        {
            if((dataCount == 0) || (dataCount > 8))
            {
                throw new ArgumentException("Invalid dataCount '" + dataCount.ToString() + "'!");
            }

            this.Description = description;
            this.ID = ID;
            this.DataCount = dataCount;
            this.RawBuffer = new byte[MuComFrame.GetFrameSizeFromDataCount(this.Description, this.DataCount)];

            //Create first bytes with header and variable index
            this.RawBuffer[0] = (byte)(0x80 | (int)this.Description | ((this.DataCount - 1) << 2) | (this.ID >> 6));
            this.RawBuffer[1] = (byte)((this.ID & 0x3F) << 1);

            //Handle read request differently
            if(this.Description == MuComFrameDesc.ReadRequest)
            {
                this.DataBytes = new byte[0];
                return;
            }

            //Copy data array
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (data.Length != dataCount) throw new ArgumentException("Invalid length of array '" + nameof(data) + "'! " + data.Length.ToString() + " instead of " + dataCount.ToString() + "!");
            this.DataBytes = data;

            //Fill payload with data bytes
            int payload_pos = 1;
            int byte_pos = 0;
            for (int data_pos = 0; data_pos < dataCount; data_pos++)
            {
                if (byte_pos < 0)
                {
                    byte_pos = 6;
                    payload_pos++;
                    this.RawBuffer[payload_pos] = 0;
                }
                this.RawBuffer[payload_pos] |= (byte)(data[data_pos] >> (7 - byte_pos));
                payload_pos++;
                this.RawBuffer[payload_pos] = (byte)((data[data_pos] << byte_pos) & 0x7F);
                byte_pos--;
            }
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

        public static byte GetIdFromFrame(byte[] frame)
        {
            if (frame is null) throw new ArgumentNullException(nameof(frame));
            if (frame.Length < 2) throw new ArgumentException("The frame must contain at least two bytes");

            return (byte)(((frame[0] & 0x03) << 6) | ((frame[1] & 0x7E) >> 1));
        }

        public static int GetFrameSizeFromDataCount(MuComFrameDesc description, int dataCount)
        {
            if(description == MuComFrameDesc.ReadRequest)
            {
                return 2;
            }
            return ((dataCount * 8 + 5) / 7 + 2);
        }

        #endregion
    }
}
