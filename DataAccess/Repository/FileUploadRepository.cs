using DataAccess.Context;
using DataAccess.Interfaces;
using DataAccess.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repository
{
    public class FileUploadRepository : IFileUploadRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileUploadRepository> _logger;

        //METHOD INJECTION
        public FileUploadRepository(ApplicationDbContext context, ILogger<FileUploadRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("FileUploadRepository instantiated with Constructor Injection");
        }

        public async Task<FileUploadEntity> AddAsync(FileUploadEntity entity)
        {
            try
            {
                _context.UploadedFiles.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File entity added to database via Constructor-Injected context: {Id} - {StoredFileName}",
                    entity.Id, entity.StoredFileName);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding file entity to database: {StoredFileName}",
                    entity.StoredFileName);
                throw;
            }
        }

        public async Task<FileUploadEntity?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving file entity by ID using Constructor-Injected context: {Id}", id);
                return await _context.UploadedFiles
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file entity by ID: {Id}", id);
                throw;
            }
        }

        public async Task<FileUploadEntity?> GetByStoredFileNameAsync(string storedFileName)
        {
            try
            {
                _logger.LogDebug("Retrieving file entity by stored filename using Constructor-Injected context: {StoredFileName}",
                    storedFileName);
                return await _context.UploadedFiles
                    .FirstOrDefaultAsync(f => f.StoredFileName == storedFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file entity by stored filename: {StoredFileName}",
                    storedFileName);
                throw;
            }
        }

        public async Task<IEnumerable<FileUploadEntity>> GetAllActiveAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all active files using Constructor-Injected context");
                return await _context.UploadedFiles
                    .Where(f => f.IsActive)
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all active files");
                throw;
            }
        }

        public async Task<IEnumerable<FileUploadEntity>> GetByUserAsync(string userId)
        {
            try
            {
                _logger.LogDebug("Retrieving files for user using Constructor-Injected context: {UserId}", userId);
                return await _context.UploadedFiles
                    .Where(f => f.UploadedByUser == userId && f.IsActive)
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<FileUploadEntity> UpdateAsync(FileUploadEntity entity)
        {
            try
            {
                _context.UploadedFiles.Update(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File entity updated using Constructor-Injected context: {Id} - {StoredFileName}",
                    entity.Id, entity.StoredFileName);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file entity: {Id}", entity.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return false;

                _context.UploadedFiles.Remove(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("File entity deleted from database using Constructor-Injected context: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file entity: {Id}", id);
                return false;
            }
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                    return false;

                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("File entity soft deleted using Constructor-Injected context: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting file entity: {Id}", id);
                return false;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.UploadedFiles
                    .AnyAsync(f => f.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists: {Id}", id);
                return false;
            }
        }

        public async Task<bool> ExistsByStoredFileNameAsync(string storedFileName)
        {
            try
            {
                return await _context.UploadedFiles
                    .AnyAsync(f => f.StoredFileName == storedFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists by stored filename: {StoredFileName}",
                    storedFileName);
                return false;
            }
        }

        public async Task<long> GetTotalFileSizeByUserAsync(string userId)
        {
            try
            {
                return await _context.UploadedFiles
                    .Where(f => f.UploadedByUser == userId && f.IsActive)
                    .SumAsync(f => f.FileSizeInBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total file size for user: {UserId}", userId);
                return 0;
            }
        }

        public async Task<int> GetFileCountByUserAsync(string userId)
        {
            try
            {
                return await _context.UploadedFiles
                    .CountAsync(f => f.UploadedByUser == userId && f.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting files for user: {UserId}", userId);
                return 0;
            }
        }

        public async Task<IEnumerable<FileUploadEntity>> GetRecentFilesAsync(int count = 10)
        {
            try
            {
                return await _context.UploadedFiles
                    .Where(f => f.IsActive)
                    .OrderByDescending(f => f.UploadedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent files");
                throw;
            }
        }

        public async Task<IEnumerable<FileUploadEntity>> SearchByTitleAsync(string searchTerm)
        {
            try
            {
                return await _context.UploadedFiles
                    .Where(f => f.IsActive && f.Title.Contains(searchTerm))
                    .OrderByDescending(f => f.UploadedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching files by title: {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}
