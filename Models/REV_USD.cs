using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADDPerformance.Models
{
    public class REV_USD:AuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Status Status { get; set; }
        public DateTime Date { get; set; }
        public string? Month { get; set; }
        public double CY_USD { get; set; }
        public double LY_USD { get; set; }
        public double Target_USD { get; set; }
        public double AT { get; set; }
        public double ALY { get; set; }
        public string? Total { get; set; }
        public string? MonthName { get; set; }
        public int MonthNum { get; set; }
        public int Year { get; set; }


    }
}
