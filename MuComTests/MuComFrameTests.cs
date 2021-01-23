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
        [InlineData(new byte[] { 0xE3, 0x5A, 0x43 }, MuComFrameDesc.ExecuteRequest, 237, new byte[1] { 0x43 })]
        [InlineData(new byte[] { 0xB1, 0x12 }, MuComFrameDesc.ReadRequest, 73, new byte[0])]
        [InlineData(new byte[] { 0xDC, 0x4A, 0x01, 0x11, 0x51, 0x2C, 0x78, 0x4D, 0x2F, 0x1B, 0x6F }, MuComFrameDesc.WriteRequest, 37, new byte[8] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF })]
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

        [Theory]
        [InlineData(MuComFrameDesc.ReadResponse, 249, 4, new byte[4] { 0x75, 0x12, 0x00, 0xFA })]
        [InlineData(MuComFrameDesc.WriteRequest, 19, 4, new byte[4] { 0x75, 0x12, 0x00, 0xFA })]
        [InlineData(MuComFrameDesc.ReadRequest, 0, 4, new byte[0])]
        public void ConstructorShouldConvertFineInBothDirections(MuComFrameDesc desc, byte ID, int dataCount, byte[] dataBytes)
        {
            //Arrange
            var frameOne = new MuComFrame(desc, ID, dataCount, dataBytes);

            //Act
            var frameTwo = new MuComFrame(frameOne.RawBuffer);

            //Assert
            frameOne.Description.Should().Be(desc);
            frameTwo.Description.Should().Be(desc);
            frameOne.ID.Should().Be(ID);
            frameTwo.ID.Should().Be(ID);
            frameOne.DataCount.Should().Be(dataCount);
            frameTwo.DataCount.Should().Be(dataCount);
            frameOne.DataBytes.Should().Equal(dataBytes);
            frameTwo.DataBytes.Should().Equal(dataBytes);
            frameOne.RawBuffer.Should().Equal(frameTwo.RawBuffer);
        }
    }
}
