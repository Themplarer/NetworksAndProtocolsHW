using System;

namespace ProtocolsDNS
{
    [Flags]
    public enum ResponseCode : byte
    {
        NoError = 0b_0000_0000,
        FormatError = 0b_0000_0001,
        ServerFailure = 0b_0000_0010,
        NameError = 0b_0000_0011,
        NotImplemented = 0b_0000_0100,
        Refused = 0b_0000_0101

        // others are reserved
    }
}