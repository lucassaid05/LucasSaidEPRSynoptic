using Domain.Models;
using Domain.Interfaces;
using System.Security.Cryptography;
using DataAccess.Interfaces;

namespace Domain.Service
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly IFileUploadRepository _repository;
        private readonly string _secureStoragePath;
        private readonly string _tempStoragePath;
        private readonly bool _generateUniqueNames;

        public FileService(
            IConfiguration configuration,
            ILogger<FileService> logger,
            IFileUploadRepository repository)
        {
            _configuration = configuration;
            _logger = logger;
            _repository = repository;
            _secureStoragePath = _configuration["FileUpload:SecureStoragePath"] ?? "App_Data/SecureFiles";
            _tempStoragePath = _configuration["FileUpload:TempStoragePath"] ?? "App_Data/TempFiles";
            _generateUniqueNames = _configuration.GetValue<bool>("FileUpload:GenerateUniqueNames", true);

            // Ensure directories exist
            EnsureDirectoriesExist();
        }

        public async Task<UploadedFile> StoreFileAsync(string title, IFormFile file, string? description = null, string? userId = null, string? ipAddress = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null");

                // Generate secure file name
                var storedFileName = GenerateSecureFileName(file.FileName);
                var filePath = Path.Combine(_secureStoragePath, storedFileName);

                // Create the domain model
                var uploadedFile = new UploadedFile
                {
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

                // Calculate file hash for integrity
                using var stream = file.OpenReadStream();
                uploadedFile.FileHash = await CalculateFileHashAsync(stream);
                stream.Position = 0; // Reset stream position

                // Store the file physically
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Save to database
                var entity = FileMapper.ToDataEntity(uploadedFile);
                var savedEntity = await _repository.AddAsync(entity);
                var result = FileMapper.ToDomainModel(savedEntity);

                _logger.LogInformation("File stored successfully: {FileName} -> {StoredFileName} (ID: {Id})",
                    file.FileName, storedFileName, result.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<(Stream fileStream, string contentType, string fileName)> RetrieveFileAsync(int fileId)
        {
            var entity = await _repository.GetByIdAsync(fileId);
            if (entity == null || !entity.IsActive)
                throw new FileNotFoundException($"File with ID {fileId} not found");

            return await RetrieveFileAsync(entity.StoredFileName);
        }

        public async Task<(Stream fileStream, string contentType, string fileName)> RetrieveFileAsync(string storedFileName)
        {
            var entity = await _repository.GetByStoredFileNameAsync(storedFileName);
            if (entity == null || !entity.IsActive)
                throw new FileNotFoundException($"File {storedFileName} not found in records");

            var filePath = Path.Combine(_secureStoragePath, storedFileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Physical file {storedFileName} not found");

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await Task.FromResult((fileStream, entity.ContentType, entity.OriginalFileName));
        }

        public async Task<bool> DeleteFileAsync(int fileId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(fileId);
                if (entity == null)
                    return false;

                // Soft delete in database
                var success = await _repository.SoftDeleteAsync(fileId);

                if (success)
                {
                    // Optionally delete physical file
                    var filePath = Path.Combine(_secureStoragePath, entity.StoredFileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    _logger.LogInformation("File deleted: {StoredFileName} (ID: {Id})", entity.StoredFileName, fileId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileId}", fileId);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string storedFileName)
        {
            try
            {
                var entity = await _repository.GetByStoredFileNameAsync(storedFileName);
                if (entity == null || !entity.IsActive)
                    return false;

                var filePath = Path.Combine(_secureStoragePath, storedFileName);
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file exists: {StoredFileName}", storedFileName);
                return false;
            }
        }

        public string GetSecureFilePath(string storedFileName)
        {
            return Path.Combine(_secureStoragePath, storedFileName);
        }

        public async Task<UploadedFile?> GetFileInfoAsync(int fileId)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(fileId);
                return entity != null && entity.IsActive ? FileMapper.ToDomainModel(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file info: {FileId}", fileId);
                return null;
            }
        }

        public async Task<IEnumerable<UploadedFile>> GetAllFilesAsync()
        {
            try
            {
                var entities = await _repository.GetAllActiveAsync();
                return FileMapper.ToDomainModels(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all files");
                return Enumerable.Empty<UploadedFile>();
            }
        }

        public string GenerateSecureFileName(string originalFileName)
        {
            if (!_generateUniqueNames)
                return SanitizeFileName(originalFileName);

            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8]; // First 8 characters

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

                // Create .htaccess file to prevent direct access (for Apache servers)
                var htaccessPath = Path.Combine(_secureStoragePath, ".htaccess");
                if (!File.Exists(htaccessPath))
                {
                    File.WriteAllText(htaccessPath, "deny from all");
                }

                // Create web.config for IIS to prevent direct access
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
                    File.WriteAllText(webConfigPath, webConfigContent);
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
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Limit length
            if (sanitized.Length > 100)
                sanitized = sanitized[..100];

            return sanitized;
        }
    }
}
