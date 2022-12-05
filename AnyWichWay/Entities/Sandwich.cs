using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace AnyWichWay.Entities
{
    public class Sandwich
    {
		public int Key { get; set; }
        public string Fillings { get; set; } = string.Empty;
        public string Condiments { get; set; } = string.Empty;
        public int Cost { get; set; }
		public string MealPower1 { get; set; } = string.Empty;
        public string MealPower2 { get; set; } = string.Empty;
        public string MealPower3 { get; set; } = string.Empty;
        public string Taste { get; set; } = string.Empty;

        public int Sweet { get; set; }
		public int Salty { get; set; }
		public int Sour { get; set; }
		public int Bitter { get; set; }
		public int Hot { get; set; }
		public int Egg { get; set; }
		public int Catching { get; set; }
		public int Exp { get; set; }
		public int Raid { get; set; }
		public int ItemDrop { get; set; }
		public int Humungo { get; set; }
		public int Teensy { get; set; }
		public int Encounter { get; set; }
		public int Title { get; set; }
		public int Sparkling { get; set; }
		public int Normal { get; set; }
		public int Fighting { get; set; }
		public int Flying { get; set; }
		public int Poison { get; set; }
		public int Ground { get; set; }
		public int Rock { get; set; }
		public int Bug { get; set; }
		public int Ghost { get; set; }
		public int Steel { get; set; }
		public int Fire { get; set; }
		public int Water { get; set; }
		public int Grass { get; set; }
		public int Electric { get; set; }
		public int Psychic { get; set; }
		public int Ice { get; set; }
		public int Dragon { get; set; }
		public int Dark { get; set; }
		public int Fairy { get; set; }
    }
}
