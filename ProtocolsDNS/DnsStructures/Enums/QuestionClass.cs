namespace ProtocolsDNS
{
    public enum QuestionClass : ushort
    {
        ReservedFirst = 0,
        Internet = 1,
        Chaos = 3,
        Hesiod = 4,
        None = 254,
        Any = 255,
        ReservedLast = 65535
    }
}