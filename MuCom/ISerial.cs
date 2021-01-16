using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace MuCom
{
    public delegate void DataReceivedEventHandler(object sender);

    public interface ISerial
    {
        #region Properties

        int ReadTimeout { get; set; }

        int WriteTimeout { get; set; }

        #endregion

        #region Events

        event DataReceivedEventHandler DataReceived;

        #endregion

        #region Methods

        void Open();

        void Close();

        int Available();

        byte ReadByte();

        #endregion
    }
}
