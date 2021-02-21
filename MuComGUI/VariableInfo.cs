using MuCom;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MuComGUI
{
    public class VariableInfo : INotifyPropertyChanged
    {
        #region Property change event

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region Constants

        public static IReadOnlyList<string> AllowedVariableTypeNames { get; } = new List<string>
        {
            "int8_t",
            "int16_t",
            "int32_t",
            "int64_t",
            "uint8_t",
            "uint16_t",
            "uint32_t",
            "int64_t",
            "float",
            "double"
        };

        #endregion

        #region Fields and properties

        private byte _ID;
        public byte ID
        {
            get => this._ID;
            set
            {
                this._ID = value;
                OnPropertyChanged();
            }
        }

        private string _Value = "";
        public string Value
        {
            get => this._Value;
            set
            {
                this._Value = value;
                OnPropertyChanged();
            }
        }

        private int _variableType = 0;
        public Type VariableType
        {
            get => MuComHandler.AllowedVariableTypes[this._variableType];
            set
            {
                int index = (MuComHandler.AllowedVariableTypes as List<Type>).IndexOf(value);
                this._variableType = index >= 0 ? index : 0;
                OnPropertyChanged();
            }
        }
        public string VariableTypeName
        {
            get => VariableInfo.AllowedVariableTypeNames[this._variableType];
            set
            {
                int index = (VariableInfo.AllowedVariableTypeNames as List<string>).IndexOf(value);
                this._variableType = index >= 0 ? index : 0;
                OnPropertyChanged();
            }
        }

        private bool _Plot;
        public bool Plot
        {
            get => this._Plot;
            set
            {
                this._Plot = value;
                if(value == false)
                {
                    Array.Clear(this.DataPoints, 0, this.DataPoints.Length);
                }
                OnPropertyChanged();
            }
        }

        public byte[] ByteData
        {
            get => this.GetByteData();
            set => this.SetByteData(value);
        }

        public readonly DataPoint[] DataPoints = new DataPoint[GUI.GraphValueCount];

        #endregion

        #region Methods

        public double ToDouble()
        {
            if(double.TryParse(this.Value, out double value) == true)
            {
                return value;
            }
            return double.NaN;
        }

        public void Read(MuComHandler handler)
        {
            try
            {
                double value;
                switch (this._variableType)
                {
                    case 0:
                        value = (float)handler.ReadSByte(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 1:
                        value = (double)handler.ReadShort(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 2:
                        value = handler.ReadInt(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 3:
                        value = handler.ReadLong(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 4:
                        value = handler.ReadByte(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 5:
                        value = handler.ReadUShort(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 6:
                        value = handler.ReadUInt(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 7:
                        value = handler.ReadULong(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 8:
                        value = handler.ReadFloat(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString("F3");
                        break;

                    case 9:
                        value = handler.ReadDouble(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString("F6");
                        break;

                    default:
                        this.Value = "Invalid type!";
                        break;
                }
            }
            catch
            {
                this.Value = "Error!";
                this.AddDataPoint(double.NaN);
                throw;
            }
        }

        public void Write(MuComHandler handler)
        {
            switch (this._variableType)
            {
                case 0:
                    handler.WriteSByte(this.ID, sbyte.Parse(this.Value));
                    break;

                case 1:
                    handler.WriteShort(this.ID, short.Parse(this.Value));
                    break;

                case 2:
                    handler.WriteInt(this.ID, int.Parse(this.Value));
                    break;

                case 3:
                    handler.WriteLong(this.ID, long.Parse(this.Value));
                    break;

                case 4:
                    handler.WriteByte(this.ID, byte.Parse(this.Value));
                    break;

                case 5:
                    handler.WriteUShort(this.ID, ushort.Parse(this.Value));
                    break;

                case 6:
                    handler.WriteUInt(this.ID, uint.Parse(this.Value));
                    break;

                case 7:
                    handler.WriteULong(this.ID, ulong.Parse(this.Value));
                    break;

                case 8:
                    handler.WriteFloat(this.ID, float.Parse(this.Value));
                    break;

                case 9:
                    handler.WriteDouble(this.ID, float.Parse(this.Value));
                    break;
            }
        }

        public void AddDataPoint(double value)
        {
            var timestamp = TimeSpanAxis.ToDouble(DateTime.Now - GUI.graphStartTime);

            Array.Copy(this.DataPoints, 0, this.DataPoints, 1, this.DataPoints.Length - 1);

            this.DataPoints[0] = new DataPoint(timestamp, value);
        }

        private byte[] GetByteData()
        {
            if (double.TryParse(this.Value, out double value) == true)
            {
                switch (this._variableType)
                {
                    case 0:
                        return BitConverter.GetBytes((sbyte)value);

                    case 1:
                        return BitConverter.GetBytes((short)value);

                    case 2:
                        return BitConverter.GetBytes((int)value);

                    case 3:
                        return BitConverter.GetBytes((long)value);

                    case 4:
                        return BitConverter.GetBytes((byte)value);

                    case 5:
                        return BitConverter.GetBytes((ushort)value);

                    case 6:
                        return BitConverter.GetBytes((uint)value);

                    case 7:
                        return BitConverter.GetBytes((ulong)value);

                    case 8:
                        return BitConverter.GetBytes((float)value);
                }
            }

            return null;
        }

        private void SetByteData(byte[] data)
        {
            switch (this._variableType)
            {
                case 0:
                    this.Value = ((sbyte)data[0]).ToString();
                    this.AddDataPoint((double)(sbyte)data[0]);
                    break;

                case 1:
                    long iVar = BitConverter.ToInt16(data, 0);
                    this.Value = iVar.ToString();
                    this.AddDataPoint((double)iVar);
                    break;

                case 2:
                    iVar = BitConverter.ToInt32(data, 0);
                    this.Value = iVar.ToString();
                    this.AddDataPoint((double)iVar);
                    break;

                case 3:
                    iVar = BitConverter.ToInt64(data, 0);
                    this.Value = iVar.ToString();
                    this.AddDataPoint((double)iVar);
                    break;

                case 4:
                    this.Value = data[0].ToString();
                    this.AddDataPoint((double)data[0]);
                    break;

                case 5:
                    ulong uVar = BitConverter.ToUInt16(data, 0);
                    this.Value = uVar.ToString();
                    this.AddDataPoint((double)uVar);
                    break;

                case 6:
                    uVar = BitConverter.ToUInt32(data, 0);
                    this.Value = uVar.ToString();
                    this.AddDataPoint((double)uVar);
                    break;

                case 7:
                    uVar = BitConverter.ToUInt64(data, 0);
                    this.Value = uVar.ToString();
                    this.AddDataPoint((double)uVar);
                    break;

                case 8:
                    float fVar = BitConverter.ToSingle(data, 0);
                    this.Value = fVar.ToString();
                    this.AddDataPoint((double)fVar);
                    break;
            }
        }

        #endregion
    }
}
