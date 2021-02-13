using MuCom;
using OxyPlot;
using OxyPlot.Axes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            "float"
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
                    this.DataPoints.Clear();
                }
                OnPropertyChanged();
            }
        }

        public readonly List<DataPoint> DataPoints = new List<DataPoint>(1000);

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
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 1:
                        value = (double)handler.ReadShort(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 2:
                        value = handler.ReadInt(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 3:
                        value = handler.ReadLong(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 4:
                        value = handler.ReadByte(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 5:
                        value = handler.ReadUShort(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 6:
                        value = handler.ReadUInt(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 7:
                        value = handler.ReadULong(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString();
                        break;

                    case 8:
                        value = handler.ReadFloat(this.ID);
                        this.AddValue(value);
                        this.Value = value.ToString("F3");
                        break;

                    default:
                        this.Value = "Invalid type!";
                        break;
                }
            }
            catch
            {
                this.Value = "Error!";
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
            }
        }

        private void AddValue(double value)
        {
            var timestamp = TimeSpanAxis.ToDouble(DateTime.Now - GUI.graphStartTime);

            if(this.DataPoints.Count < this.DataPoints.Capacity)
            {
                this.DataPoints.Add(new DataPoint(timestamp, value));
            }
            else
            {
                for (int i = 1; i < this.DataPoints.Count; i++)
                {
                    this.DataPoints[i - 1] = this.DataPoints[i];
                }
                this.DataPoints[this.DataPoints.Count - 1] = new DataPoint(timestamp, value);
            }
        }

        #endregion
    }
}
