using AnyWichWay.Enums;
using System.ComponentModel.DataAnnotations;

namespace AnyWichWay.Entities
{
    public class MealPower
    {
        public Powers Power { get; set; }

        public Types Type { get; set; }

        [Range(1, 3)]
        public int Level { get; set; }
    }
}
