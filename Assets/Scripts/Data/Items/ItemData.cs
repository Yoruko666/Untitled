namespace Untitled
{
    // Runtime quantity; configuration and save serialization live in separate types.
    public sealed class ItemData
    {
        public int Id { get; }
        public int Num { get; private set; }

        public ItemData(int id, int num)
        {
            Id = id;
            Num = num;
        }

        internal void SetCount(int value) => Num = value;
    }
}
