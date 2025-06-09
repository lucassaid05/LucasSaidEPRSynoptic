using Microsoft.AspNetCore.Mvc;
using LucasSaidEPRSynoptic.Models;
using Domain.Interfaces;
using Domain.Models;

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

        // GET: FileUpload
        public IActionResult Index()
        {
            return View(new FileUploadViewModel());
        }

        // POST: FileUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadViewModel model)
        {
            // Check if model state is valid
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Additional file validation
            if (model.FileUpload != null)
            {
                // Get validation settings from configuration
                var maxSizeMB = _configuration.GetValue<int>("FileUpload:MaxFileSizeInMB", 10);
                var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
                    ?? new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };

                // Check file size
                if (model.FileUpload.Length > maxSizeMB * 1024 * 1024)
                {
                    ModelState.AddModelError("FileUpload", $"File size cannot exceed {maxSizeMB}MB");
                    return View(model);
                }

                // Check file extension
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
                // Get client information for security tracking
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userId = User.Identity?.Name ?? "Anonymous";

                // Store the file using the file service (null-checked above)
                var uploadedFile = await _fileService.StoreFileAsync(
                    model.Title,
                    model.FileUpload!, // Non-null assertion since we checked above
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

        // GET: FileUpload/Success
        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"];
            ViewBag.UploadedFileId = TempData["UploadedFileId"];
            return View();
        }

        // GET: FileUpload/Download/5
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

        // GET: FileUpload/List
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

        // GET: FileUpload/Details/5
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

        // POST: FileUpload/Delete/5
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