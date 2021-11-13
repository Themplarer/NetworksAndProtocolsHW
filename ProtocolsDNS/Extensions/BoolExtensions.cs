namespace ProtocolsDNS
{
    public static class BoolExtensions
    {
        // оптимизация вперёёёёёд
        public static unsafe byte ToByte(this bool b) => *(byte*) &b;
    }
}