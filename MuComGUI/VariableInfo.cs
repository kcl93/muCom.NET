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
                this.LinkVariable();
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
                this.LinkVariable();
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
                this.LinkVariable();
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

        public void Read()
        {
            if (GUI.MuComHandler is null) return;

            try
            {
                double value;
                switch (this._variableType)
                {
                    case 0:
                        value = (float)GUI.MuComHandler.ReadSByte(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 1:
                        value = (double)GUI.MuComHandler.ReadShort(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 2:
                        value = GUI.MuComHandler.ReadInt(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 3:
                        value = GUI.MuComHandler.ReadLong(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 4:
                        value = GUI.MuComHandler.ReadByte(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 5:
                        value = GUI.MuComHandler.ReadUShort(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 6:
                        value = GUI.MuComHandler.ReadUInt(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 7:
                        value = GUI.MuComHandler.ReadULong(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString();
                        break;

                    case 8:
                        value = GUI.MuComHandler.ReadFloat(this.ID);
                        this.AddDataPoint(value);
                        this.Value = value.ToString("F3");
                        break;

                    case 9:
                        value = GUI.MuComHandler.ReadDouble(this.ID);
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

        public void Write()
        {
            if (GUI.MuComHandler is null) return;

            switch (this._variableType)
            {
                case 0:
                    GUI.MuComHandler.WriteSByte(this.ID, sbyte.Parse(this.Value));
                    break;

                case 1:
                    GUI.MuComHandler.WriteShort(this.ID, short.Parse(this.Value));
                    break;

                case 2:
                    GUI.MuComHandler.WriteInt(this.ID, int.Parse(this.Value));
                    break;

                case 3:
                    GUI.MuComHandler.WriteLong(this.ID, long.Parse(this.Value));
                    break;

                case 4:
                    GUI.MuComHandler.WriteByte(this.ID, byte.Parse(this.Value));
                    break;

                case 5:
                    GUI.MuComHandler.WriteUShort(this.ID, ushort.Parse(this.Value));
                    break;

                case 6:
                    GUI.MuComHandler.WriteUInt(this.ID, uint.Parse(this.Value));
                    break;

                case 7:
                    GUI.MuComHandler.WriteULong(this.ID, ulong.Parse(this.Value));
                    break;

                case 8:
                    GUI.MuComHandler.WriteFloat(this.ID, float.Parse(this.Value));
                    break;

                case 9:
                    GUI.MuComHandler.WriteDouble(this.ID, float.Parse(this.Value));
                    break;
            }
        }

        public void AddDataPoint(double value)
        {
            var timestamp = TimeSpanAxis.ToDouble(DateTime.Now - GUI.graphStartTime);

            Array.Copy(this.DataPoints, 0, this.DataPoints, 1, this.DataPoints.Length - 1);

            this.DataPoints[0] = new DataPoint(timestamp, value);
        }

        public void LinkVariable()
        {
            GUI.MuComHandler?.LinkVariable(this, this.ID, "ByteData");
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

                    case 9:
                        return BitConverter.GetBytes((double)value);
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
                    this.Value = fVar.ToString("F3");
                    this.AddDataPoint((double)fVar);
                    break;

                case 9:
                    double dVar = BitConverter.ToDouble(data, 0);
                    this.Value = dVar.ToString("F6");
                    this.AddDataPoint((double)dVar);
                    break;
            }
        }

        #endregion
    }
}
