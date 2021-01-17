using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Linq;

//  ##### Frame structure #####
//  Byte    Bit(s)  Function
//  0       7       Start of frame indicator (must be '1')
//  0       6 - 5   Frame description
//  				    0	read data response
//  				    1	read data request
//  				    2	write data request
//  				    3	Execute function request
//  0		4-2		Data byte count -1 (relevant for read and write frames)
//  0       1 - 0   Bits 6 - 7 of 1.payload byte
//  1       7       Start of frame indicator (must be '0')
//  1       6 - 1   Bits 5 - 0 of 1.payload byte
//  1       0       Bit 7 of 2.payload byte
//  2       7       Start of frame indicator (must be '0')
//  2       6 - 0   Bits 6 - 0 of 2.payload byte
//  3       7       Start of frame indicator (must be '0')
//  3       6 - 0   Bits 7 - 1 of 3.payload byte
//  4       7       Start of frame indicator (must be '0')
//  4       6       Bit 0 of 3.payload byte
//  4       5 - 0   Bits 7 - 2 of 4.payload byte
//  5       7       Start of frame indicator (must be '0')
//  5       6 - 5   Bits 1 - 0 of 4.payload byte
//  5       4 - 0   Bits 7 - 3 of 5.payload byte
//  6       7       Start of frame indicator (must be '0')
//  6       6 - 4   Bits 2 - 0 of 5.payload byte
//  6       3 - 0   Bits 7 - 4 of 6.payload byte
//  7       7       Start of frame indicator (must be '0')
//  7       6 - 3   Bits 3 - 0 of 6.payload byte
//  7       2 - 0   Bits 7 - 5 of 7.payload byte
//  8       7       Start of frame indicator (must be '0')
//  8       6 - 2   Bits 4 - 0 of 7.payload byte
//  8       1 - 0   Bits 7 - 6 of 8.payload byte
//  9       7       Start of frame indicator (must be '0')
//  9       6 - 1   Bits 5 - 0 of 8.payload byte
//  9       0       Bit 7 of 9.payload byte
//  10      7       Start of frame indicator (must be '0')
//  10      6 - 0   Bits 6 - 0 of 9.payload byte

[assembly: InternalsVisibleTo("MuComTests")]

namespace MuCom
{
    public delegate void MuComFunction(byte[] data);

    public class MuComHandler
    {
        #region Constants

        public static IReadOnlyList<Type> AllowedVariableTypes { get; } = new List<Type>
        {
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(float)
        };

        #endregion
        
        #region Delegates

        private delegate void WriteVariable(object target, object value);

        private delegate object ReadVariable(object source);

        #endregion

        #region Structs

        private struct VariableAccessors
        {
            internal object variable;
            internal WriteVariable Write;
            internal ReadVariable Read;
        }

        #endregion

        #region Fields and properties

        private readonly ISerial serial;

        private readonly byte[] frameBuffer = new byte[11];

        private MuComFrame lastFrame = null;

        private int byteCounter = 0;

        private readonly Dictionary<byte, MuComFunction> linkedFunctions = new Dictionary<byte, MuComFunction>();

        private readonly Dictionary<byte, VariableAccessors> linkedVariables = new Dictionary<byte, VariableAccessors>();

        #region Locks

        private readonly object serialLock = new object();

        private readonly object functionLock = new object();

        private readonly object variableLock = new object();

        private readonly object readLock = new object();

        #endregion

        #region Public properties

        public int Timeout
        {
            get => this.serial.ReadTimeout;
            set => this.serial.ReadTimeout = value;
        }

        public DateTime LastCommTime { get; private set; }

        #endregion

        #endregion

        #region Constructor

        public MuComHandler(ISerial serial)
        {
            if (serial is null) throw new ArgumentNullException(nameof(serial));

            this.serial = serial;
        }

        public MuComHandler(string portName, int baudrate, Parity parity = Parity.None, StopBits stopBits = StopBits.One)
        {
            if (portName is null) throw new ArgumentNullException(nameof(portName));

            this.serial = new SerialPortWrapper(portName, baudrate, parity, stopBits);

            this.serial.DataReceived += new DataReceivedEventHandler(this.DataReceivedHandler);
            
            this.Timeout = 100;
        }

        #endregion

        #region Public methods

        public void Open()
        {
            lock (this.serialLock)
            {
                this.serial.Open();

                byteCounter = 0;
            }
        }

        public void Close()
        {
            lock (this.serialLock)
            {
                this.serial.Close();
            }
        }

        public void Reset()
        {
            lock (this.serialLock)
            {
                this.byteCounter = 0;
            }
        }

        public void UnlinkMethod(byte ID)
        {
            lock (functionLock)
            {
                if (this.linkedFunctions.ContainsKey(ID) == true)
                {
                    this.linkedFunctions.Remove(ID);
                }
            }
        }

        public void LinkMethod(byte ID, MuComFunction function)
        {
            if (function is null) throw new ArgumentNullException(nameof(function));

            lock (this.functionLock)
            {
                if(this.linkedFunctions.ContainsKey(ID) == true)
                {
                    this.linkedFunctions[ID] = function;
                }
                else
                {
                    this.linkedFunctions.Add(ID, function);
                }
            }
        }

        public void UnlinkVariable(byte ID)
        {
            lock (variableLock)
            {
                if (this.linkedVariables.ContainsKey(ID) == true)
                {
                    this.linkedVariables.Remove(ID);
                }
            }
        }

        public void LinkVariable(byte ID, object obj, string name)
        {
            VariableAccessors accessors;

            Type type = obj.GetType();
            if (type.GetField(name) is null)
            {
                if(type.GetProperty(name) is null)
                {
                    throw new ArgumentException("Object '" + type.Name + "' does not have a field or property with the name '" + name + "'");
                }

                var variable = type.GetProperty(name);

                //Check variable type
                if (MuComHandler.AllowedVariableTypes.Where(x => x == variable.PropertyType).Any() == false)
                {
                    throw new ArgumentException("Type '" + variable.PropertyType.Name + "' of property '" + name + "' of class '" + type.Name + "' is not supported!");
                }
                
                //It is a property
                accessors.Write = new WriteVariable(variable.SetValue);
                accessors.Read = new ReadVariable(variable.GetValue);
            }
            else
            {
                var variable = type.GetField(name);

                //Check variable type
                if (MuComHandler.AllowedVariableTypes.Where(x => x == variable.FieldType).Any() == false)
                {
                    throw new ArgumentException("Type '" + variable.FieldType.Name + "' of property '" + name + "' of class '" + type.Name + "' is not supported!");
                }

                //It is a field
                accessors.Write = new WriteVariable(variable.SetValue);
                accessors.Read = new ReadVariable(variable.GetValue);
            }

            accessors.variable = obj;

            lock(variableLock)
            {
                if (this.linkedVariables.ContainsKey(ID) == true)
                {
                    this.linkedVariables[ID] = accessors;
                }
                else
                {
                    this.linkedVariables.Add(ID, accessors);
                }
            }
        }

        #endregion

        #region Private helper methods

        private void DataReceivedHandler(object sender)
        {
            lock(serialLock)
            {
                //Read all available data bytes
                while (this.serial.Available() > 0)
                {
                    //Get byte from receive buffer
                    byte receivedByte = this.serial.ReadByte();

                    if (MuComFrame.IsHeaderByte(receivedByte) == true)
                    {
                        //Header received! Reset receive statemachine, e.g. data counter
                        this.byteCounter = 1;
                        this.frameBuffer[0] = receivedByte;
                        continue;
                    }

                    if (this.byteCounter == 0)
                    {
                        //Waiting for the header... Discard any non-header bytes
                        continue;
                    }

                    if (this.byteCounter > 10)
                    {
                        //Buffer overflow! Invalid number of bytes received for the current frame! Reset statemachine...
                        this.byteCounter = 0;
                        continue;
                    }

                    //Store data byte and calculate desired frame length
                    this.frameBuffer[this.byteCounter] = receivedByte;
                    this.byteCounter++;

                    int dataByteCount = MuComFrame.GetDataByteCountFromHeader(this.frameBuffer[0]);

                    if ((MuComFrame.GetFrameDescriptionFromHeader(this.frameBuffer[0]) == MuComFrameDesc.ReadRequest)
                        || ((dataByteCount == 1) && (this.byteCounter >= (dataByteCount + 1)))
                        || (this.byteCounter >= (dataByteCount + 2)))
                    {
                        this.byteCounter = 0; //Reset statemachine

                        this.LastCommTime = DateTime.Now; //Save timestamp

                        //Sufficient data received. Decode it and do stuff if required
                        var frame = new MuComFrame(this.frameBuffer);

                        //Act on the received frame
                        switch (this.lastFrame.Description)
                        {
                            case MuComFrameDesc.ExecuteRequest:
                                this.HandleExecuteRequest(frame);
                                break;

                            case MuComFrameDesc.ReadRequest:
                                this.HandleReadRequest(frame);
                                break;

                            case MuComFrameDesc.ReadResponse:
                                this.lastFrame = frame;
                                break;

                            case MuComFrameDesc.WriteRequest:
                                this.HandleWriteRequest(frame);
                                break;

                            default:
                                break; //Do nothing if unknown
                        }
                    }
                }
            }
        }

        private void HandleExecuteRequest(MuComFrame frame)
        {
            lock (this.functionLock)
            {
                if (this.linkedFunctions.ContainsKey(frame.ID))
                {
                    try
                    {
                        //Check if delegate is null
                        if (this.linkedFunctions[frame.ID] is null)
                        {
                            return;
                        }

                        //Execute the function asynchronously
                        Task.Run(() => this.linkedFunctions[frame.ID](frame.DataBytes));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        private void HandleReadRequest(MuComFrame frame)
        {
            lock (this.variableLock)
            {
                if (this.linkedVariables.ContainsKey(frame.ID))
                {
                    byte[] data = new byte[frame.DataCount];

                    //Check if a read accessor is there
                    if (this.linkedVariables[frame.ID].Read != null)
                    {
                        var value = this.linkedVariables[frame.ID].Read(this.linkedVariables[frame.ID].variable);

                        if (value is sbyte) data = BitConverter.GetBytes((sbyte)value);
                        else if (value is short) data = BitConverter.GetBytes((short)value);
                        else if (value is int) data = BitConverter.GetBytes((int)value);
                        else if (value is long) data = BitConverter.GetBytes((long)value);
                        else if (value is byte) data = BitConverter.GetBytes((byte)value);
                        else if (value is ushort) data = BitConverter.GetBytes((ushort)value);
                        else if (value is uint) data = BitConverter.GetBytes((uint)value);
                        else if (value is byte) data = BitConverter.GetBytes((byte)value);
                        else if (value is ulong) data = BitConverter.GetBytes((ulong)value);
                    }

                    var response = new MuComFrame(MuComFrameDesc.ReadResponse, this.lastFrame.ID, this.lastFrame.DataCount, data);
                    this.serial.WriteBytes(response.RawBuffer);
                }
            }
        }

        private void HandleWriteRequest(MuComFrame frame)
        {
            lock (this.variableLock)
            {
                if (this.linkedVariables.ContainsKey(frame.ID))
                {
                    //Check if a write accessor is there
                    if(this.linkedVariables[frame.ID].Write is null)
                    {
                        return;
                    }

                    this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, 0);
                }
            }
        }

        #endregion
    }
}
