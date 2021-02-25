using System;
using MuCom;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace MuComTests
{
    public class MuComHandlerTests
    {
        #region Variables used for linking

        public byte variableByte;
        public ushort variableUShort;
        public uint variableUInt;
        public ulong variableULong;
        public byte variableSByte { get; set; }
        public ushort variableShort { get; set; }
        public uint variableInt { get; set; }
        public ulong variableLong { get; set; }
        public float variableFloat { get; set; }
        public int variableDouble;
        public byte[] variableByteArray = new byte[11];

        #endregion

        #region Methods used for linking

        private void DoStuff(byte[] data)
        {
            //Do nothing
        }

        #endregion

        [Fact]
        public void LinkVariableShouldThrowNoException()
        {
            //Arrange
            var serial = Substitute.For<ISerial>();
            var handler = new MuComHandler(serial);

            //Act
            handler.LinkVariable(this, 0, nameof(this.variableByte));
            handler.LinkVariable(this, 1, nameof(this.variableFloat));
            handler.LinkVariable(this, 2, nameof(this.variableInt));

            //Assert
            handler.ReadInt(0);
        }

        [Fact]
        public void LinkMethodShouldThrowNoException()
        {
            //Arrange
            var serial = Substitute.For<ISerial>();
            var handler = new MuComHandler(serial);

            //Act
            handler.LinkMethod(1, this.DoStuff);

            //Assert
        }

        [Theory]
        [InlineData("variableByteArray", new byte[] { 0xDC, 0x4A, 0x01, 0x11, 0x51, 0x2C, 0x78, 0x4D, 0x2F, 0x1B, 0x6F }, new byte[8] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF })]
        public void WritingLinkedByte_ShouldWriteCorrectValue(string name, byte[] frame, object value)
        {
            //Arrange
            var serial = Substitute.For<ISerial>();
            var handler = new MuComHandler(serial);

            handler.LinkVariable(this, 0, name);

            //Act


            //Assert
        }
    }
}
