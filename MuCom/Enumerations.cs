using System;
using System.Collections.Generic;
using System.Text;

namespace MuCom
{
    public enum MuComError
    {
        None = 0,
        Misc = -1,
        Timeout = -2,
        Communication = -3
    }

    public enum MuComFrameDesc
    {
        ReadResponse = 0x00,
        ReadRequest = 0x20,
        WriteRequest = 0x40,
        ExecuteRequest = 0x60
    }
}
