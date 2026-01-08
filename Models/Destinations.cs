using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADDPerformance.Models
{
    public class Destinations:AuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Status Status { get; set; }
        public string Destination { get; set; }
        public string Origin { get; set; }
        public string? DestCity { get; set; }
        public string? OriginCity { get; set; }
        public DateTime Month { get; set; }
        public long paxCount { get; set; }
        public string? MonthName { get; set; }
        public int MonthNum { get; set; }
        public int Year { get; set; }
  
    }
}
