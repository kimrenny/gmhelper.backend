using MatHelper.CORE.Models;
using MatHelper.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MatHelper.DAL.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<LoginToken> LoginTokens { get; set; }
        public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }
        public DbSet<PasswordRecoveryToken> PasswordRecoveryTokens { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
        public DbSet<AdminRequestLog> AdminRequests { get; set; }
        public DbSet<RequestLogDetail> RequestLogDetails { get; set; }
        public DbSet<AuthLog> AuthLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<AdminSettings> AdminSettings { get; set; }
        public DbSet<AdminSection> AdminSections { get; set; }
        public DbSet<AdminSwitch> AdminSwitches { get; set; }
        public DbSet<TaskRequestLog> TaskRequestLogs { get; set; }
        public DbSet<TaskRating> TaskRatings { get; set; }
        public DbSet<EmailLoginCode> EmailLoginCodes { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<LoginToken>().HasOne(t => t.User).WithMany(u => u.LoginTokens).HasForeignKey(t => t.UserId);

            modelBuilder.Entity<LoginToken>().OwnsOne(t => t.DeviceInfo);

            modelBuilder.Entity<RequestLog>().HasIndex(r => r.Date).IsUnique();

            modelBuilder.Entity<AdminRequestLog>().HasIndex(r => r.Date).IsUnique();

            modelBuilder.Entity<AdminSettings>()
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<AdminSettings>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminSection>()
                .HasOne(s => s.AdminSettings)
                .WithMany(a => a.Sections)
                .HasForeignKey(s => s.AdminSettingsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminSwitch>()
                .HasOne(sw => sw.AdminSection)
                .WithMany(s => s.Switches)
                .HasForeignKey(sw => sw.AdminSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}