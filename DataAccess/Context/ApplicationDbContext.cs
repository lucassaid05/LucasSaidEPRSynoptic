using DataAccess.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LucasSaidEPRSynoptic.Models;

namespace DataAccess.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<FileUploadEntity> UploadedFiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure FileUploadEntity
            modelBuilder.Entity<FileUploadEntity>(entity =>
            {
                entity.ToTable("UploadedFiles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.StoredFileName)
                      .IsUnique()
                      .HasDatabaseName("IX_UploadedFiles_StoredFileName");

                entity.HasIndex(e => e.UploadedAt)
                      .HasDatabaseName("IX_UploadedFiles_UploadedAt");

                entity.HasIndex(e => e.UploadedByUser)
                      .HasDatabaseName("IX_UploadedFiles_UploadedByUser");

                entity.HasIndex(e => e.IsActive)
                      .HasDatabaseName("IX_UploadedFiles_IsActive");

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.OriginalFileName)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.StoredFileName)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.FileExtension)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.Property(e => e.ContentType)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.StoragePath)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(e => e.FileHash)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.UploadedByUser)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.IPAddress)
                      .HasMaxLength(45);

                entity.Property(e => e.UpdatedByUser)
                      .HasMaxLength(100);

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UploadedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Add relationship to ApplicationUser
                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(e => e.UploadedByUser)
                      .HasPrincipalKey(u => u.Id)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
            });

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("Roles");
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });

            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = this.ChangeTracker.Entries<FileUploadEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UploadedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}