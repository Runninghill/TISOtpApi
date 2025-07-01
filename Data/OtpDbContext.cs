using Microsoft.EntityFrameworkCore;
using TISOtpApi.Models;

namespace TISOtpApi.Data
{
    public class OtpDbContext : DbContext
    {
        public OtpDbContext(DbContextOptions<OtpDbContext> options) : base(options) { }
        public DbSet<OtpEntry> OtpEntries => Set<OtpEntry>();
    }
}
