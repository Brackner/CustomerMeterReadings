using Microsoft.EntityFrameworkCore;
using CMR_API.Entities;


namespace CMR_API.DataConnections
{
    public class ENSEK_DbContext : DbContext
    {
        public ENSEK_DbContext(DbContextOptions<ENSEK_DbContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<MeterReading> meterReadings { get; set; }
    }
}
