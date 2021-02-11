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

        byte Read();

        void Write(byte[] data);

        #endregion
    }
}
