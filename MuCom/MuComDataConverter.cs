using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MuCom
{
    public static class MuComDataConverter
    {
        #region Structs

        [StructLayout(LayoutKind.Explicit)]
        private struct ByteArray
        {
            [FieldOffset(0)]
            public sbyte SByte;

            [FieldOffset(0)]
            public short Short;

            [FieldOffset(0)]
            public int Int;

            [FieldOffset(0)]
            public long Long;

            [FieldOffset(0)]
            public byte Byte;

            [FieldOffset(0)]
            public ushort UShort;

            [FieldOffset(0)]
            public uint UInt;

            [FieldOffset(0)]
            public ulong ULong;

            [FieldOffset(0)]
            public float Float;
        }

        #endregion

        #region Constants

        public static IReadOnlyDictionary<Type, int> VariableByteCount { get; } = new Dictionary<Type, int>
        {
            { typeof(sbyte),  1 },
            { typeof(short),  2 },
            { typeof(int),    4 },
            { typeof(long),   8 },
            { typeof(byte),   1 },
            { typeof(ushort), 2 },
            { typeof(uint),   4 },
            { typeof(ulong),  8 },
            { typeof(float),  4 },
            { typeof(double), 8 },
            { typeof(byte[]), 8 }
        };

        #endregion

        #region Methods

        #region Variables to byte arrays

        public static byte[] GetBytes(sbyte value)
        {
            return new byte[1] { (byte)value };
        }

        public static byte[] GetBytes(byte value)
        {
            return new byte[1] { value };
        }

        public static byte[] GetBytes(short value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(ushort value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(int value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(long value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(ulong value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(float value)
        {
            return BitConverter.GetBytes(value);
        }

        public static byte[] GetBytes(double value)
        {
            return BitConverter.GetBytes(value);
        }

        #endregion

        #region Byte array to variables

        public static sbyte GetSByte(byte[] data)
        {
            return (sbyte)data[0];
        }

        public static byte GetByte(byte[] data)
        {
            return data[0];
        }

        public static short GetShort(byte[] data)
        {
            return BitConverter.ToInt16(data, 0);
        }

        public static ushort GetUShort(byte[] data)
        {
            return BitConverter.ToUInt16(data, 0);
        }

        public static int GetInt(byte[] data)
        {
            return BitConverter.ToInt32(data, 0);
        }

        public static uint GetUInt(byte[] data)
        {
            return BitConverter.ToUInt32(data, 0);
        }

        public static long GetLong(byte[] data)
        {
            return BitConverter.ToInt64(data, 0);
        }

        public static ulong GetULong(byte[] data)
        {
            return BitConverter.ToUInt64(data, 0);
        }

        public static float GetFloat(byte[] data)
        {
            return BitConverter.ToSingle(data, 0);
        }

        public static double GetDouble(byte[] data)
        {
            return BitConverter.ToDouble(data, 0);
        }

        #endregion

        #endregion
    }
}
