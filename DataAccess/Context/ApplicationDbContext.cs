using DataAccess.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<FileUploadEntity> UploadedFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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
            var entries = ChangeTracker.Entries<FileUploadEntity>();

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
