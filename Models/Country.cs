using System.ComponentModel.DataAnnotations.Schema;
using HotelAPI.Models;

public class Country
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("Code")]
    public string Code { get; set; } = string.Empty;

    [Column("Flag")]
    public string Flag { get; set; } = string.Empty;

    [Column("PhoneCode")]
    public string PhoneCode { get; set; } = string.Empty;

    [Column("PhoneNumberDigits")]
    public int PhoneNumberDigits { get; set; }
    public string? Region { get; set; }

    public ICollection<City> Cities { get; set; } = new List<City>();
}
