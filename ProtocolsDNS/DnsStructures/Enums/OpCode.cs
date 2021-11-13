using System;
using System.Diagnostics.CodeAnalysis;

namespace ProtocolsDNS
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum OpCode : byte
    {
        Query = 0b_0000_0000,
        IQuery = 0b_0000_1000,
        Status = 0b_0001_0000,
        Unassigned = 0b_0001_1000,
        Notify = 0b_0010_0000,
        Update = 0b_0010_1000,
        DNSStatefulOperations = 0b_0011_0000
    }
}