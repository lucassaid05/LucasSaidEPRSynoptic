using Microsoft.AspNetCore.Mvc;
using Domain.Interfaces;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System;

namespace LucasSaidEPRSynoptic.Controllers
{
    public class FileServingController : Controller
    {
        public FileServingController()
        {
            
        }

        [HttpGet("serve/{id:int}")]
        public async Task<IActionResult> ServeFile(
            int id,
            [FromServices] IFileService fileService, 
            [FromServices] ILogger<FileServingController> logger) 
        {
            logger.LogInformation("ServeFile method called with Method Injection - File ID: {FileId}", id);

            try
            {
                var fileInfo = await fileService.GetFileInfoAsync(id);
                if (fileInfo == null)
                {
                    logger.LogWarning("File not found using Method-Injected service: {FileId}", id);
                    return NotFound($"File with ID {id} not found");
                }
                var (fileStream, contentType, fileName) = await fileService.RetrieveFileAsync(id);

                logger.LogInformation("File served successfully using Method Injection: {FileId} - {FileName}",
                    id, fileName);

                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                return File(fileStream, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                logger.LogError("File not found in storage using Method-Injected service: {FileId}", id);
                return NotFound($"Physical file for ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error serving file using Method-Injected services: {FileId}", id);
                return StatusCode(500, "An error occurred while serving the file");
            }
        }

        [HttpGet("download/{id:int}")]
        public async Task<IActionResult> ForceDownload(
            int id,
            [FromServices] IFileService fileService, 
            [FromServices] ILogger<FileServingController> logger)
        {
            logger.LogInformation("ForceDownload method called with Method Injection - File ID: {FileId}", id);

            try
            {
                var (fileStream, contentType, fileName) = await fileService.RetrieveFileAsync(id);

                logger.LogInformation("Force download initiated using Method Injection: {FileId} - {FileName}",
                    id, fileName);

                return File(fileStream, contentType, fileName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in force download using Method-Injected services: {FileId}", id);
                return StatusCode(500, "An error occurred while downloading the file");
            }
        }

        [HttpGet("info/{id:int}")]
        public async Task<IActionResult> GetFileInfo(int id)
        {
            var serviceProvider = HttpContext.RequestServices;
            var fileService = serviceProvider.GetRequiredService<IFileService>();
            var repository = serviceProvider.GetRequiredService<IFileUploadRepository>();
            var logger = serviceProvider.GetRequiredService<ILogger<FileServingController>>();

            logger.LogInformation("GetFileInfo method called with Method Injection via IServiceProvider - File ID: {FileId}", id);

            try { 
                var fileInfo = await fileService.GetFileInfoAsync(id);
                if (fileInfo == null)
                {
                    logger.LogWarning("File info not found using Method-Injected service: {FileId}", id);
                    return NotFound($"File with ID {id} not found");
                }
                var exists = await repository.ExistsAsync(id);

                logger.LogInformation("File info retrieved using Method Injection: {FileId} - {Title}",
                    id, fileInfo.Title);

                var response = new
                {
                    Id = fileInfo.Id,
                    Title = fileInfo.Title,
                    OriginalFileName = fileInfo.OriginalFileName,
                    FileExtension = fileInfo.FileExtension,
                    FileSizeInBytes = fileInfo.FileSizeInBytes,
                    ContentType = fileInfo.ContentType,
                    Description = fileInfo.Description,
                    UploadedAt = fileInfo.UploadedAt,
                    UploadedByUser = fileInfo.UploadedByUser,
                    Exists = exists,
                    ServeUrl = Url.Action("ServeFile", new { id = fileInfo.Id }),
                    DownloadUrl = Url.Action("ForceDownload", new { id = fileInfo.Id })
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving file info using Method-Injected services: {FileId}", id);
                return StatusCode(500, "An error occurred while retrieving file information");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListFiles(
            [FromServices] IFileService fileService, 
            [FromServices] ILogger<FileServingController> logger)
        {
            logger.LogInformation("ListFiles method called with Method Injection");

            try
            {
                var repository = HttpContext.RequestServices.GetRequiredService<IFileUploadRepository>();
                var files = await fileService.GetAllFilesAsync();

                var allEntities = await repository.GetAllActiveAsync();
                var totalCount = allEntities.Count();
                var totalSize = allEntities.Sum(f => f.FileSizeInBytes);

                logger.LogInformation("File list retrieved using Method Injection - Count: {Count}, Total Size: {Size}",
                    totalCount, totalSize);

                var response = new
                {
                    Files = files.Select(f => new
                    {
                        Id = f.Id,
                        Title = f.Title,
                        OriginalFileName = f.OriginalFileName,
                        FileSizeInBytes = f.FileSizeInBytes,
                        UploadedAt = f.UploadedAt,
                        ServeUrl = Url.Action("ServeFile", new { id = f.Id }),
                        DownloadUrl = Url.Action("ForceDownload", new { id = f.Id }),
                        InfoUrl = Url.Action("GetFileInfo", new { id = f.Id })
                    }),
                    Statistics = new
                    {
                        TotalFiles = totalCount,
                        TotalSizeInBytes = totalSize,
                        TotalSizeFormatted = FormatFileSize(totalSize)
                    }
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing files using Method-Injected services");
                return StatusCode(500, "An error occurred while listing files");
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetFileStatistics()
        {
            var serviceProvider = HttpContext.RequestServices;
            var logger = serviceProvider.GetService<ILogger<FileServingController>>();
            var repository = serviceProvider.GetService<IFileUploadRepository>();
            var fileService = serviceProvider.GetService<IFileService>();

            if (repository == null || fileService == null)
            {
                logger?.LogError("Required services not available for method injection");
                return StatusCode(500, "Required services not configured");
            }

            logger?.LogInformation("GetFileStatistics called with conditional Method Injection");

            try
            {
                var allFiles = await repository.GetAllActiveAsync();
                var recentFiles = await repository.GetRecentFilesAsync(5);

                var stats = new
                {
                    TotalActiveFiles = allFiles.Count(),
                    TotalSize = allFiles.Sum(f => f.FileSizeInBytes),
                    AverageFileSize = allFiles.Any() ? allFiles.Average(f => f.FileSizeInBytes) : 0,
                    LargestFile = allFiles.Any() ? allFiles.Max(f => f.FileSizeInBytes) : 0,
                    SmallestFile = allFiles.Any() ? allFiles.Min(f => f.FileSizeInBytes) : 0,
                    RecentUploads = recentFiles.Count(),
                    MostCommonExtensions = allFiles
                        .GroupBy(f => f.FileExtension)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => new { Extension = g.Key, Count = g.Count() })
                        .ToList(),
                    MethodInjectionDemo = "This data was retrieved using Method Injection pattern"
                };

                logger?.LogInformation("File statistics calculated using Method-Injected services: {TotalFiles} files",
                    stats.TotalActiveFiles);

                return Json(stats);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error calculating statistics using Method-Injected services");
                return StatusCode(500, "An error occurred while calculating statistics");
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}