using Domain.Models;
using Microsoft.AspNetCore.Http;

namespace Domain.Interfaces
{
    public interface IFileService
    {
        Task<UploadedFile> StoreFileAsync(string title, IFormFile file, string? description = null, string? userId = null, string? ipAddress = null);
        Task<(Stream fileStream, string contentType, string fileName)> RetrieveFileAsync(int fileId);
        Task<(Stream fileStream, string contentType, string fileName)> RetrieveFileAsync(string storedFileName);
        Task<bool> DeleteFileAsync(int fileId);
        Task<bool> FileExistsAsync(string storedFileName);
        string GetSecureFilePath(string storedFileName);
        Task<UploadedFile?> GetFileInfoAsync(int fileId);
        Task<IEnumerable<UploadedFile>> GetAllFilesAsync();
        string GenerateSecureFileName(string originalFileName);
        Task<string> CalculateFileHashAsync(Stream fileStream);
    }
}