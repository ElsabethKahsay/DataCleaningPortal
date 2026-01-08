using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADDPerformance.Models
{
    public class DateMaster:AuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Status Status { get; set; }
        public DateTime Date { get; set; }
        public string? Month { get; set; }
        public string? MonthName { get; set; }
        public int MonthNum { get; set; }
        public int Year { get; set; }

    }
}
