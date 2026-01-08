using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ADDPerformance.Models
{
    public class ByTourCode:AuditTrail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Status Status { get; set; }
        public string TourCode { get; set; }
        public string CORP_TYPE { get; set; }
        public string CORPORATE_NAME { get; set; }
        public double Target { get; set; }
        public double ATPercent { get; set; }
        public DateTime Date { get; set; }
        public double MonthylyAmount { get; set; }
        public string? MonthName { get; set; }
        public int MonthNum { get; set; }
        public int Year { get; set; }

    }
}
