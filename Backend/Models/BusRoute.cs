using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("routes")]
    public class BusRoute
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("source_id")]
        public int SourceId { get; set; }

        [Column("destination_id")]
        public int DestinationId { get; set; }

        [Column("source_name")]
        public string SourceName { get; set; } = "";

        [Column("destination_name")]
        public string DestinationName { get; set; } = "";
    }
}
