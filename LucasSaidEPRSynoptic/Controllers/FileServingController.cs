using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LucasSaidEPRSynoptic.Models;
using LucasSaidEPRSynoptic.Filters;
using Domain.Interfaces;
using DataAccess.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System;

namespace LucasSaidEPRSynoptic.Controllers
{
    [Authorize]
    public class FileServingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public FileServingController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("serve/{id:int}")]
        [RequireFileOwnership]
        public async Task<IActionResult> ServeFile(
            int id,
            [FromServices] IFileService fileService,
            [FromServices] ILogger<FileServingController> logger)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            logger.LogInformation("ServeFile method called by user {UserId} - File ID: {FileId}",
                currentUser?.Id, id);

            try
            {
                var fileInfo = await fileService.GetFileInfoAsync(id);
                if (fileInfo == null)
                {
                    logger.LogWarning("File not found using Method-Injected service: {FileId}", id);
                    return NotFound($"File with ID {id} not found");
                }

                var (fileStream, contentType, fileName) = await fileService.RetrieveFileAsync(id);

                logger.LogInformation("File served successfully by user {UserId}: {FileId} - {FileName}",
                    currentUser?.Id, id, fileName);

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
                return File(fileStream, contentType, fileName);
            }
            catch (FileNotFoundException)
            {
                logger.LogError("File not found in storage: {FileId}", id);
                return NotFound($"Physical file for ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error serving file: {FileId}", id);
                return StatusCode(500, "An error occurred while serving the file");
            }
        }

        [HttpGet("download/{id:int}")]
        [RequireFileOwnership]
        public async Task<IActionResult> ForceDownload(
            int id,
            [FromServices] IFileService fileService,
            [FromServices] ILogger<FileServingController> logger)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            logger.LogInformation("ForceDownload method called by user {UserId} - File ID: {FileId}",
                currentUser?.Id, id);

            try
            {
                var (fileStream, contentType, fileName) = await fileService.RetrieveFileAsync(id);

                logger.LogInformation("Force download initiated by user {UserId}: {FileId} - {FileName}",
                    currentUser?.Id, id, fileName);

                return File(fileStream, contentType, fileName, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in force download: {FileId}", id);
                return StatusCode(500, "An error occurred while downloading the file");
            }
        }
    }
}