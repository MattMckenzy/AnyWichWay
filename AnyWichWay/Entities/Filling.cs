using AnyWichWay.Enums;

namespace AnyWichWay.Entities
{
    public class Filling
    {
        public Fillings Name { get; set; }
        public Shops Shop { get; set; }
        public int Cost { get; set; }
        public int Count { get; set; }
        public List<TasteValue> TasteValues { get; } = new List<TasteValue>();
        public List<PowerValue> PowerValues { get; } = new List<PowerValue>();
        public List<TypeValue> TypeValues { get; } = new List<TypeValue>();
    }
}