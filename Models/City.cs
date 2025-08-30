using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using HotelAPI.Models;

public class City
{
    [Column("Id")]
    public int Id { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("CountryId")]
    public int CountryId { get; set; }
    [JsonIgnore]
    public Country Country { get; set; } = null!;
    public ICollection<HotelInfo> Hotels { get; set; } = new List<HotelInfo>();
}
