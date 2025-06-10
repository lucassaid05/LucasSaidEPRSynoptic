using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using DataAccess.Interfaces;
using DataAccess.Models; // Changed from LucasSaidEPRSynoptic.Models
using System.Security.Claims;
using LucasSaidEPRSynoptic.Models;

namespace LucasSaidEPRSynoptic.Filters
{
    public class FileOwnershipFilter : IAsyncActionFilter
    {
        private readonly IFileUploadRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FileOwnershipFilter> _logger;

        public FileOwnershipFilter(
            IFileUploadRepository repository,
            UserManager<ApplicationUser> userManager,
            ILogger<FileOwnershipFilter> logger)
        {
            _repository = repository;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity!.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized access attempt to file download");
                context.Result = new UnauthorizedResult();
                return;
            }

            // Get the file ID from the action parameters
            if (!context.ActionArguments.TryGetValue("id", out var idValue) ||
                !int.TryParse(idValue?.ToString(), out var fileId))
            {
                _logger.LogWarning("Invalid or missing file ID in download request");
                context.Result = new BadRequestObjectResult("Invalid file ID");
                return;
            }

            try
            {
                // Get the current user
                var currentUser = await _userManager.GetUserAsync(context.HttpContext.User);
                if (currentUser == null)
                {
                    _logger.LogWarning("Could not find user for authenticated request");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // Get the file entity
                var fileEntity = await _repository.GetByIdAsync(fileId);
                if (fileEntity == null || !fileEntity.IsActive)
                {
                    _logger.LogWarning("File not found or inactive: {FileId}", fileId);
                    context.Result = new NotFoundObjectResult($"File with ID {fileId} not found");
                    return;
                }

                // Check if user is admin (admins can access all files)
                var isAdmin = context.HttpContext.User.IsInRole("Admin");

                // Check ownership or admin privilege
                if (!isAdmin && fileEntity.UploadedByUser != currentUser.Id)
                {
                    _logger.LogWarning("Access denied: User {UserId} attempted to access file {FileId} owned by {OwnerId}",
                        currentUser.Id, fileId, fileEntity.UploadedByUser);

                    context.Result = new ForbidResult();
                    return;
                }

                _logger.LogInformation("File access granted: User {UserId} accessing file {FileId} (IsAdmin: {IsAdmin})",
                    currentUser.Id, fileId, isAdmin);

                // If we get here, access is granted - continue to the action
                await next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file ownership for file {FileId}", fileId);
                context.Result = new StatusCodeResult(500);
                return;
            }
        }
    }

    // Attribute version for easier application
    public class RequireFileOwnershipAttribute : TypeFilterAttribute
    {
        public RequireFileOwnershipAttribute() : base(typeof(FileOwnershipFilter))
        {
        }
    }

    // Extension method to register the filter
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileOwnershipFilter(this IServiceCollection services)
        {
            services.AddScoped<FileOwnershipFilter>();
            return services;
        }
    }
}