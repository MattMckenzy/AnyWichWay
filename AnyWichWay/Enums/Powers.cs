using System.ComponentModel.DataAnnotations;

namespace AnyWichWay.Enums
{
    public enum Powers
    {
		Egg = 0,
		Catching = 1 << 0,
        Exp = 1 << 1,
        Raid = 1 << 2,
		[Display(Name="Item Drop")]
        ItemDrop = 1 << 3,
        Humungo = 1 << 4,
        Teensy = 1 << 5,
        Encounter = 1 << 6,
        Title = 1 << 7,
        Sparkling = 1 << 8
    }
}
