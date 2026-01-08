using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
namespace ADDPerformance.Models { 
    public class AuditTrail
    {
  
        public DateTime CreatedAt { get; set; }

 
        public DateTime? UpdatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
