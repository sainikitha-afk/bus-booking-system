using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("locations")]
    public class Location
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("city_name")]
        public string CityName { get; set; } = "";
    }
}
