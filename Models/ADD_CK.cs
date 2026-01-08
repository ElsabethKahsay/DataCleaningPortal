using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADDPerformance.Models
{
    public class ADD_CK : AuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Status Status { get; set; }
        public DateTime Date { get; set; }
        public string? Month { get; set; }
        public double CY { get; set; }
        public double LY { get; set; }
        public double Target { get; set; }
        public double AT { get; set; }
        public double ALY { get; set; }
        public string? Total { get; set; }
        public string? MonthName { get; set; }
        public int MonthNum { get; set; }
        public int Year { get; set; }


    }
}
