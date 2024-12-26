using MatHelper.CORE.Models;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.DAL.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<LoginToken> LoginTokens { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<LoginToken>().HasOne(t => t.User).WithMany(u => u.LoginTokens).HasForeignKey(t => t.UserId);

            modelBuilder.Entity<LoginToken>().OwnsOne(t => t.DeviceInfo);
        }
    }
}