using System.ComponentModel.DataAnnotations;

namespace AnyWichWay.Enums
{
    public enum Fillings
    {
        Apple,
        Avocado,
        Bacon,
        Banana,
        Basil,
        Cheese,
        [Display(Name = "Cherry Tomatoes")]
        CherryTomatoes,
        Chorizo,
        Cucumber,
        Egg,
        [Display(Name = "Fried Fillet")]
        FriedFillet,
        [Display(Name = "Green Bell Pepper")]
        GreenBellPepper,
        Ham,
        Hamburger,
        [Display(Name = "Herbed Sausage")]
        HerbedSausage,
        Jalapeno,
        Kiwi,
        [Display(Name = "Klawf Stick")]
        KlawfStick,
        Lettuce,
        Noodles,
        Onion,
        Pickle,
        Pineapple,
        [Display(Name = "Potato Salad")]
        PotatoSalad,
        [Display(Name = "Potato Tortilla")]
        PotatoTortilla,
        Prosciutto,
        [Display(Name = "Red Bell Pepper")]
        RedBellPepper,
        [Display(Name = "Red Onion")]
        RedOnion,
        Rice,
        [Display(Name = "Smoked Fillet")]
        SmokedFillet,
        Strawberry,
        Tofu,
        Tomato,
        Watercress,
        [Display(Name = "Yellow Bell Pepper")]
        YellowBellPepper
    }
}
