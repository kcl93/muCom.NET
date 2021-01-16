using System;
using Xunit;
using MuCom;
using FluentAssertions;

namespace MuComTests
{
    public class MuComFrameTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        public void ConstructorMustThrowExceptions(byte[] input)
        {
            //Arrange

            //Act
            Assert.ThrowsAny<Exception>(() => new MuComFrame(input));

            //Assert
        }

        [Theory]
        [InlineData(new byte[] { 0xE3, 0x5A }, MuComFrameDesc.ExecuteRequest, 237, new byte[1] { 0 })]
        public void ConstructorMustDecodeFrameProperly(byte[] inputBuffer, MuComFrameDesc desc, byte ID, byte[] dataBytes)
        {
            //Arrange

            //Act
            var frame = new MuComFrame(inputBuffer);

            //Assert
            frame.ID.Should().Be(ID);
            frame.Description.Should().Be(desc);
            frame.DataBytes.Should().Equal(dataBytes);
        }
    }
}
