namespace Domain.Models
{
    public class UploadedFile
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string FileHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string UploadedByUser { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
    }
}
