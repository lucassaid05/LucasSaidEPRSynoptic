using Microsoft.AspNetCore.Mvc;
using LucasSaidEPRSynoptic.Models;
using Domain.Interfaces;
using Domain.Models;
using System.IO;
using System.Threading.Tasks;
using System;

namespace LucasSaidEPRSynoptic.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadController> _logger;

        public FileUploadController(IFileService fileService, IConfiguration configuration, ILogger<FileUploadController> logger)
        {
            _fileService = fileService;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(new FileUploadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.FileUpload != null)
            {
                var maxSizeMB = _configuration.GetValue<int>("FileUpload:MaxFileSizeInMB", 10);
                var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
                    ?? new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };

                if (model.FileUpload.Length > maxSizeMB * 1024 * 1024)
                {
                    ModelState.AddModelError("FileUpload", $"File size cannot exceed {maxSizeMB}MB");
                    return View(model);
                }

                var fileExtension = Path.GetExtension(model.FileUpload.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("FileUpload",
                        "Only the following file types are allowed: " + string.Join(", ", allowedExtensions));
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("FileUpload", "Please select a file to upload");
                return View(model);
            }

            try 
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userId = User.Identity?.Name ?? "Anonymous";

                var uploadedFile = await _fileService.StoreFileAsync(
                    model.Title,
                    model.FileUpload!,
                    model.Description,
                    userId,
                    ipAddress);

                _logger.LogInformation("File uploaded successfully: {FileId} - {Title}",
                    uploadedFile.Id, uploadedFile.Title);

                TempData["SuccessMessage"] = $"File '{model.Title}' uploaded successfully! File ID: {uploadedFile.Id}";
                TempData["UploadedFileId"] = uploadedFile.Id;

                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {Title}", model.Title);
                ModelState.AddModelError("", "An error occurred while uploading the file. Please try again.");
                return View(model);
            }
        }

        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"];
            ViewBag.UploadedFileId = TempData["UploadedFileId"];
            return View();
        }

        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var fileResult = await _fileService.RetrieveFileAsync(id);

                _logger.LogInformation("File downloaded: {FileId} - {FileName}", id, fileResult.fileName);

                return File(fileResult.fileStream, fileResult.contentType, fileResult.fileName);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("Attempted to download non-existent file: {FileId}", id);
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileId}", id);
                return StatusCode(500, "An error occurred while downloading the file");
            }
        }

        public async Task<IActionResult> List()
        {
            try
            {
                var files = await _fileService.GetAllFilesAsync();
                return View(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file list");
                return StatusCode(500, "An error occurred while retrieving the file list");
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var fileInfo = await _fileService.GetFileInfoAsync(id);
                if (fileInfo == null)
                    return NotFound("File not found");

                return View(fileInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file details: {FileId}", id);
                return StatusCode(500, "An error occurred while retrieving file details");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _fileService.DeleteFileAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "File deleted successfully";
                    _logger.LogInformation("File deleted: {FileId}", id);
                }
                else
                {
                    TempData["ErrorMessage"] = "File not found or could not be deleted";
                    _logger.LogWarning("Failed to delete file: {FileId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the file";
            }

            return RedirectToAction("List");
        }
    }
}