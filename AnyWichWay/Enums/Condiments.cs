using System.ComponentModel.DataAnnotations;

namespace AnyWichWay.Enums
{
    public enum Condiments
    {
        Butter,
        [Display(Name = "Chili Sauce")]
        ChiliSauce,
        [Display(Name = "Cream Cheese")]
        CreamCheese,
        [Display(Name = "Curry Powder")]
        CurryPowder,
        Horseradish,
        Jam,
        Ketchup,
        Marmalade,
        Mayonnaise,
        Mustard,
        [Display(Name = "Olive Oil")]
        OliveOil,
        [Display(Name = "Peanut Butter")]
        PeanutButter,
        Pepper,
        Salt,
        Vinegar,
        Wasabi,
        [Display(Name = "Whipped Cream")]
        WhippedCream,
        Yogurt,
        [Display(Name = "Bitter Herba Mystica")]
        BitterHerbaMystica,
        [Display(Name = "Spicy Herba Mystica")]
        SpicyHerbaMystica,
        [Display(Name = "Salty Herba Mystica")]
        SaltyHerbaMystica,
        [Display(Name = "Sour Herba Mystica")]
        SourHerbaMystica,
        [Display(Name = "Sweet Herba Mystica")]
        SweetHerbaMystica,
        None
    }
}
