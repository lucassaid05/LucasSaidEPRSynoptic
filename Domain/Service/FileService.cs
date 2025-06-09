using Domain.Models;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Service
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly string _secureStoragePath;
        private readonly string _tempStoragePath;
        private readonly bool _generateUniqueNames;
        private readonly List<UploadedFile> _files; 

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _secureStoragePath = _configuration["FileUpload:SecureStoragePath"] ?? "App_Data/SecureFiles";
            _tempStoragePath = _configuration["FileUpload:TempStoragePath"] ?? "App_Data/TempFiles";
            _generateUniqueNames = _configuration.GetValue<bool>("FileUpload:GenerateUniqueNames", true);
            _files = new List<UploadedFile>();

            EnsureDirectoriesExist();
        }

        public async Task<UploadedFile> StoreFileAsync(string title, IFormFile file, string? description = null, string? userId = null, string? ipAddress = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null");

                var storedFileName = GenerateSecureFileName(file.FileName);
                var filePath = Path.Combine(_secureStoragePath, storedFileName);

                var uploadedFile = new UploadedFile
                {
                    Id = _files.Count + 1,
                    Title = title,
                    OriginalFileName = file.FileName,
                    StoredFileName = storedFileName,
                    FileExtension = Path.GetExtension(file.FileName).ToLowerInvariant(),
                    FileSizeInBytes = file.Length,
                    ContentType = file.ContentType,
                    Description = description,
                    UploadedAt = DateTime.UtcNow,
                    StoragePath = filePath,
                    UploadedByUser = userId ?? "Anonymous",
                    IPAddress = ipAddress ?? "Unknown"
                };

                using var stream = file.OpenReadStream();
                uploadedFile.FileHash = await CalculateFileHashAsync(stream);
                stream.Position = 0; 

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(fileStream);
                }

                _files.Add(uploadedFile);

                _logger.LogInformation("File stored successfully: {FileName} -> {StoredFileName}",
                    file.FileName, storedFileName);

                return uploadedFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<(Stream fileStream, string contentType, string fileName)> RetrieveFileAsync(int fileId)
        {
            var fileInfo = _files.FirstOrDefault(f => f.Id == fileId && f.IsActive);
            if (fileInfo == null)
                throw new FileNotFoundException($"File with ID {fileId} not found");

            return await RetrieveFileAsync(fileInfo.StoredFileName);
        }

        public async Task<(Stream fileStream, string contentType, string fileName)> RetrieveFileAsync(string storedFileName)
        {
            var fileInfo = _files.FirstOrDefault(f => f.StoredFileName == storedFileName && f.IsActive);
            if (fileInfo == null)
                throw new FileNotFoundException($"File {storedFileName} not found in records");

            var filePath = Path.Combine(_secureStoragePath, storedFileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Physical file {storedFileName} not found");

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return (fileStream, fileInfo.ContentType, fileInfo.OriginalFileName);
        }

        public async Task<bool> DeleteFileAsync(int fileId)
        {
            var fileInfo = _files.FirstOrDefault(f => f.Id == fileId);
            if (fileInfo == null)
                return false;

            try
            {
                fileInfo.IsActive = false;

                var filePath = Path.Combine(_secureStoragePath, fileInfo.StoredFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                _logger.LogInformation("File deleted: {StoredFileName}", fileInfo.StoredFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {StoredFileName}", fileInfo.StoredFileName);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string storedFileName)
        {
            var filePath = Path.Combine(_secureStoragePath, storedFileName);
            return File.Exists(filePath) && _files.Any(f => f.StoredFileName == storedFileName && f.IsActive);
        }

        public async Task<string> GetSecureFilePathAsync(string storedFileName)
        {
            return Path.Combine(_secureStoragePath, storedFileName);
        }

        public async Task<UploadedFile?> GetFileInfoAsync(int fileId)
        {
            return _files.FirstOrDefault(f => f.Id == fileId && f.IsActive);
        }

        public async Task<IEnumerable<UploadedFile>> GetAllFilesAsync()
        {
            return _files.Where(f => f.IsActive).OrderByDescending(f => f.UploadedAt);
        }

        public string GenerateSecureFileName(string originalFileName)
        {
            if (!_generateUniqueNames)
                return SanitizeFileName(originalFileName);

            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8]; 

            return $"{timestamp}_{uniqueId}{extension}";
        }

        public async Task<string> CalculateFileHashAsync(Stream fileStream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToBase64String(hashBytes);
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(_secureStoragePath))
                {
                    Directory.CreateDirectory(_secureStoragePath);
                    _logger.LogInformation("Created secure storage directory: {Path}", _secureStoragePath);
                }

                if (!Directory.Exists(_tempStoragePath))
                {
                    Directory.CreateDirectory(_tempStoragePath);
                    _logger.LogInformation("Created temp storage directory: {Path}", _tempStoragePath);
                }

                var htaccessPath = Path.Combine(_secureStoragePath, ".htaccess");
                if (!File.Exists(htaccessPath))
                {
                    File.WriteAllTextAsync(htaccessPath, "deny from all");
                }

                var webConfigPath = Path.Combine(_secureStoragePath, "web.config");
                if (!File.Exists(webConfigPath))
                {
                    var webConfigContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
    </handlers>
  </system.webServer>
</configuration>";
                    File.WriteAllTextAsync(webConfigPath, webConfigContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating storage directories");
                throw;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            if (sanitized.Length > 100)
                sanitized = sanitized[..100];

            return sanitized;
        }
    }
}
