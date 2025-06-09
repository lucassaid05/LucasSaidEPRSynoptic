using Domain.Models;
using DataAccess.Models;

namespace Domain.Service
{
    public static class FileMapper
    {
        public static UploadedFile ToDomainModel(FileUploadEntity entity)
        {
            return new UploadedFile
            {
                Id = entity.Id,
                Title = entity.Title,
                OriginalFileName = entity.OriginalFileName,
                StoredFileName = entity.StoredFileName,
                FileExtension = entity.FileExtension,
                FileSizeInBytes = entity.FileSizeInBytes,
                ContentType = entity.ContentType,
                Description = entity.Description,
                UploadedAt = entity.UploadedAt,
                StoragePath = entity.StoragePath,
                FileHash = entity.FileHash,
                IsActive = entity.IsActive,
                UploadedByUser = entity.UploadedByUser,
                IPAddress = entity.IPAddress ?? string.Empty
            };
        }
        public static FileUploadEntity ToDataEntity(UploadedFile domainModel)
        {
            return new FileUploadEntity
            {
                Id = domainModel.Id,
                Title = domainModel.Title,
                OriginalFileName = domainModel.OriginalFileName,
                StoredFileName = domainModel.StoredFileName,
                FileExtension = domainModel.FileExtension,
                FileSizeInBytes = domainModel.FileSizeInBytes,
                ContentType = domainModel.ContentType,
                Description = domainModel.Description,
                UploadedAt = domainModel.UploadedAt,
                StoragePath = domainModel.StoragePath,
                FileHash = domainModel.FileHash,
                IsActive = domainModel.IsActive,
                UploadedByUser = domainModel.UploadedByUser,
                IPAddress = domainModel.IPAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                UpdatedByUser = null
            };
        }

        public static IEnumerable<UploadedFile> ToDomainModels(IEnumerable<FileUploadEntity> entities)
        {
            return entities.Select(ToDomainModel);
        }
        
        public static IEnumerable<FileUploadEntity> ToDataEntities(IEnumerable<UploadedFile> domainModels)
        {
            return domainModels.Select(ToDataEntity);
        }
    }
}
