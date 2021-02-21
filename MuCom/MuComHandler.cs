﻿using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Collections.Generic;
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
            typeof(float),
            typeof(double),
            typeof(byte[])
        };

        #endregion

        #region Delegates

        private delegate void WriteVariable(object target, object value);

        private delegate object ReadVariable(object source);

        #endregion

        #region Structs

        private struct LinkedVariable
        {
            internal Type type;
            internal int byteCount;
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

        private readonly Dictionary<byte, LinkedVariable> linkedVariables = new Dictionary<byte, LinkedVariable>();

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

        #region General methods

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

        #endregion

        #region Methods for linking variables and functions

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
                if (this.linkedFunctions.ContainsKey(ID) == true)
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
            LinkedVariable accessors;

            Type type = obj.GetType();
            if (type.GetField(name) is null)
            {
                if (type.GetProperty(name) is null)
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
                accessors.type = variable.PropertyType;
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
                accessors.type = variable.FieldType;
            }

            accessors.variable = obj;

            if (MuComHandler.AllowedVariableTypes.Where(x => x == accessors.type).Any() == false)
            {
                throw new ArgumentException("The type of the given field is not supported by MuCom!");
            }
            accessors.byteCount = MuComDataConverter.VariableByteCount[accessors.type];

            lock (variableLock)
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

        #region Methods for reading variables

        public byte[] Read(byte ID, int dataCount)
        {
            var frame = new MuComFrame(MuComFrameDesc.ReadRequest, ID, dataCount, null);

            lock (this.readLock)
            {
                lock (this.serialLock)
                {
                    this.serial.Write(frame.RawBuffer);
                    this.lastFrame = null;
                }

                var timeout = Stopwatch.StartNew();
                while(this.lastFrame is null)
                {
                    if(timeout.ElapsedMilliseconds > this.serial.ReadTimeout)
                    {
                        throw new TimeoutException("Did not receive answer to read request within timeout!");
                    }
                }

                if(this.lastFrame.DataCount != dataCount)
                {
                    throw new ArgumentException("Invalid count of data bytes received (" + this.lastFrame.DataCount.ToString() + " instead of " + dataCount.ToString() + ")! Please check type of expected variable!");
                }

                return this.lastFrame.DataBytes;
            }
        }

        public byte ReadByte(byte ID)
        {
            return MuComDataConverter.GetByte(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(byte)]));
        }

        public sbyte ReadSByte(byte ID)
        {
            return MuComDataConverter.GetSByte(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(sbyte)]));
        }

        public ushort ReadUShort(byte ID)
        {
            return MuComDataConverter.GetUShort(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(ushort)]));
        }

        public short ReadShort(byte ID)
        {
            return MuComDataConverter.GetShort(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(short)]));
        }

        public uint ReadUInt(byte ID)
        {
            return MuComDataConverter.GetUInt(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(uint)]));
        }

        public int ReadInt(byte ID)
        {
            return MuComDataConverter.GetInt(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(int)]));
        }

        public ulong ReadULong(byte ID)
        {
            return MuComDataConverter.GetULong(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(ulong)]));
        }

        public long ReadLong(byte ID)
        {
            return MuComDataConverter.GetLong(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(long)]));
        }

        public float ReadFloat(byte ID)
        {
            return MuComDataConverter.GetFloat(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(float)]));
        }

        public double ReadDouble(byte ID)
        {
            return MuComDataConverter.GetDouble(this.Read(ID, MuComDataConverter.VariableByteCount[typeof(double)]));
        }

        #endregion

        #region Methods for writing variables

        public void Write(byte ID, byte[] data)
        {
            var frame = new MuComFrame(MuComFrameDesc.WriteRequest, ID, data.Length, data);
            this.serial.Write(frame.RawBuffer);
        }

        public void WriteByte(byte ID, byte value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteSByte(byte ID, sbyte value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteUShort(byte ID, ushort value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteShort(byte ID, short value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteUInt(byte ID, uint value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteInt(byte ID, int value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteULong(byte ID, ulong value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteLong(byte ID, long value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteFloat(byte ID, float value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        public void WriteDouble(byte ID, double value)
        {
            this.Write(ID, MuComDataConverter.GetBytes(value));
        }

        #endregion

        #region Methods for executing functions

        public void Execute(byte ID, byte[] data = null)
        {
            if (data is null) data = new byte[0];
            var frame = new MuComFrame(MuComFrameDesc.ExecuteRequest, ID, data.Length, data);
            this.serial.Write(frame.RawBuffer);
        }

        #endregion

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
                    byte receivedByte = this.serial.Read();

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
                        || ((dataByteCount == 1) && (this.byteCounter >= (dataByteCount + 2)))
                        || (this.byteCounter > (dataByteCount + 2)))
                    {
                        this.byteCounter = 0; //Reset statemachine

                        this.LastCommTime = DateTime.Now; //Save timestamp

                        //Sufficient data received. Decode it and do stuff if required
                        MuComFrame frame;
                        try
                        {
                            frame = new MuComFrame(this.frameBuffer);
                        }
                        catch
                        {
                            continue;
                        }

                        //Act on the received frame
                        switch (frame.Description)
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

                        if (value is sbyte)
                        {
                            data = MuComDataConverter.GetBytes((sbyte)value);
                        }
                        else if (value is short)
                        {
                            data = MuComDataConverter.GetBytes((short)value);
                        }
                        else if (value is int)
                        {
                            data = MuComDataConverter.GetBytes((int)value);
                        }
                        else if (value is long)
                        {
                            data = MuComDataConverter.GetBytes((long)value);
                        }
                        else if (value is byte)
                        {
                            data = MuComDataConverter.GetBytes((byte)value);
                        }
                        else if (value is ushort)
                        {
                            data = MuComDataConverter.GetBytes((ushort)value);
                        }
                        else if (value is uint)
                        {
                            data = MuComDataConverter.GetBytes((uint)value);
                        }
                        else if (value is byte)
                        {
                            data = MuComDataConverter.GetBytes((byte)value);
                        }
                        else if (value is ulong)
                        {
                            data = MuComDataConverter.GetBytes((ulong)value);
                        }
                        else if (value is float)
                        {
                            data = MuComDataConverter.GetBytes((float)value);
                        }
                        else if(value is double)
                        {
                            data = MuComDataConverter.GetBytes((double)value);
                        }
                    }

                    var response = new MuComFrame(MuComFrameDesc.ReadResponse, this.lastFrame.ID, this.lastFrame.DataCount, data);
                    this.serial.Write(response.RawBuffer);
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

                    var type = this.linkedVariables[frame.ID].type;
                    if(type == typeof(sbyte))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetSByte(frame.DataBytes));
                    }
                    else if(type == typeof(short))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetShort(frame.DataBytes));
                    }
                    else if(type == typeof(int))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetInt(frame.DataBytes));
                    }
                    else if(type == typeof(long))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetLong(frame.DataBytes));
                    }
                    else if(type == typeof(byte))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetByte(frame.DataBytes));
                    }
                    else if (type == typeof(ushort))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetUShort(frame.DataBytes));
                    }
                    else if (type == typeof(uint))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetUInt(frame.DataBytes));
                    }
                    else if (type == typeof(ulong))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetULong(frame.DataBytes));
                    }
                    else if(type == typeof(float))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetFloat(frame.DataBytes));
                    }
                    else if(type == typeof(double))
                    {
                        this.linkedVariables[frame.ID].Write(this.linkedVariables[frame.ID].variable, MuComDataConverter.GetDouble(frame.DataBytes));
                    }
                }
            }
        }

        #endregion
    }
}
