using Microsoft.EntityFrameworkCore;
using N5Now.Domain.Entities;

namespace N5Now.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<PermissionType> PermissionTypes => Set<PermissionType>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.PermissionType)
                .WithMany(pt => pt.Permissions)
                .HasForeignKey(p => p.PermissionTypeId);

            // Seed initial PermissionTypes
            modelBuilder.Entity<PermissionType>().HasData(
                new PermissionType { Id = 1, Description = "Read Access" },
                new PermissionType { Id = 2, Description = "Write Access" },
                new PermissionType { Id = 3, Description = "Admin Access" }
            );
        }
    }
}
