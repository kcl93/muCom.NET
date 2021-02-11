using MuCom;
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
                this._variableType = (MuComHandler.AllowedVariableTypes as List<Type>).IndexOf(value);
                OnPropertyChanged();
            }
        }
        public string VariableTypeName
        {
            get => VariableInfo.AllowedVariableTypeNames[this._variableType];
            set
            {
                this._variableType = (VariableInfo.AllowedVariableTypeNames as List<string>).IndexOf(value);
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
                OnPropertyChanged();
            }
        }

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
                switch (this._variableType)
                {
                    case 0:
                        this.Value = handler.ReadSByte(this.ID).ToString();
                        break;

                    case 1:
                        this.Value = handler.ReadShort(this.ID).ToString();
                        break;

                    case 2:
                        this.Value = handler.ReadInt(this.ID).ToString();
                        break;

                    case 3:
                        this.Value = handler.ReadLong(this.ID).ToString();
                        break;

                    case 4:
                        this.Value = handler.ReadByte(this.ID).ToString();
                        break;

                    case 5:
                        this.Value = handler.ReadUShort(this.ID).ToString();
                        break;

                    case 6:
                        this.Value = handler.ReadUInt(this.ID).ToString();
                        break;

                    case 7:
                        this.Value = handler.ReadULong(this.ID).ToString();
                        break;

                    case 8:
                        this.Value = handler.ReadFloat(this.ID).ToString();
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

        #endregion
    }
}
