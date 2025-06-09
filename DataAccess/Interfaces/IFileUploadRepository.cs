using DataAccess.Models;

namespace DataAccess.Interfaces
{
    public interface IFileUploadRepository
    {
        Task<FileUploadEntity> AddAsync(FileUploadEntity entity);
        Task<FileUploadEntity?> GetByIdAsync(int id);
        Task<FileUploadEntity?> GetByStoredFileNameAsync(string storedFileName);
        Task<IEnumerable<FileUploadEntity>> GetAllActiveAsync();
        Task<IEnumerable<FileUploadEntity>> GetByUserAsync(string userId);
        Task<FileUploadEntity> UpdateAsync(FileUploadEntity entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> SoftDeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByStoredFileNameAsync(string storedFileName);
        Task<long> GetTotalFileSizeByUserAsync(string userId);
        Task<int> GetFileCountByUserAsync(string userId);
        Task<IEnumerable<FileUploadEntity>> GetRecentFilesAsync(int count = 10);
        Task<IEnumerable<FileUploadEntity>> SearchByTitleAsync(string searchTerm);
    }
}
