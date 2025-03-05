using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.DAL.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<LoginToken> LoginTokens { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
        public DbSet<AdminRequestLog> AdminRequests { get; set; }
        public DbSet<RequestLogDetail> RequestLogDetails { get; set; }
        public DbSet<AuthLog> AuthLogs { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<LoginToken>().HasOne(t => t.User).WithMany(u => u.LoginTokens).HasForeignKey(t => t.UserId);

            modelBuilder.Entity<LoginToken>().OwnsOne(t => t.DeviceInfo);

            modelBuilder.Entity<RequestLog>().HasIndex(r => r.Date).IsUnique();

            modelBuilder.Entity<AdminRequestLog>().HasIndex(r => r.Date).IsUnique();
        }
    }
}