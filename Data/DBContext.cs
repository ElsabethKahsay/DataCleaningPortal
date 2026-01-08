using ADDPerformance.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace ADDPerformance.Data
{
    public class DBContext:DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }

        public DbSet<ADD_CK> ADD_CK { get; set; }
       public DbSet<OnlineSales> OnlineSales { get; set; }

        public DbSet<REV_USD> REV_USD { get; set; }
        public DbSet<Destinations> Destinations { get; set; }
        public DbSet<CorporateSales> CorporateSales { get; set; }
        public DbSet<ByTourCode> ByTourCode { get; set; }
        public DbSet<DateMaster> DateMaster { get; set; }
    }
}
