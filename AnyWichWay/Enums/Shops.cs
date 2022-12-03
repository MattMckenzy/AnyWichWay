using System.ComponentModel.DataAnnotations;

namespace AnyWichWay.Enums
{
    public enum Shops
    {
        [Display(Name = "Sure Cans")]
        SureCans,
        [Display(Name = "Deli Cioso")]
        DeliCioso,
        [Display(Name = "Artisan Bakery")]
        ArtisanBakery,
        [Display(Name = "Aquiesta Supermarket")]
        AquiestaSupermarket,
        Crater,
        None
    }
}
