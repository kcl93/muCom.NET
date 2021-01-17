using System;
using MuCom;
using Xunit;
using FluentAssertions;

namespace MuComTests
{
    public class MuComTests
    {
        #region Variables used for linking

        public byte variableByte;
        public float variableFloat { get; set; }
        public int variableInt { get; private set; }

        #endregion

        #region Methods used for linking

        public void DoStuff(byte[] data)
        {
            //Do nothing
        }

        #endregion

        [Fact]
        public void LinkVariableShouldThrowNoException()
        {
            //Arrange
            var handler = new MuComHandler("COM 3", 19200);

            //Act
            handler.LinkVariable(0, this, nameof(this.variableByte));
            handler.LinkVariable(1, this, nameof(this.variableFloat));
            handler.LinkVariable(2, this, nameof(this.variableInt));

            //Assert
        }

        [Fact]
        public void LinkMethodShouldThrowNoException()
        {
            //Arrange
            var handler = new MuComHandler("COM 3", 19200);

            //Act
            handler.LinkMethod(1, this.DoStuff);

            //Assert
        }
    }
}
