using System;

namespace ADDPerformance.Models
{
    public class ImportResult
    {
        public string Message { get; set; } = string.Empty;
        public int Added { get; set; }
        public int Updated { get; set; }
        public int TotalProcessed { get; set; }
        public DateTime? Timestamp { get; set; }
        public bool Success { get; set; }
    }
}
