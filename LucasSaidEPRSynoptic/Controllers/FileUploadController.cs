using LucasSaidEPRSynoptic.Models;
using Microsoft.AspNetCore.Mvc;

namespace LucasSaidEPRSynoptic.Controllers
{
    public class FileUploadController : Controller
    {
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
                if (model.FileUpload.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("FileUpload", "File size cannot exceed 10MB");
                    return View(model);
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.FileUpload.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("FileUpload",
                        "Only the following file types are allowed: " + string.Join(", ", allowedExtensions));
                    return View(model);
                }
            }

            try
            {
                TempData["SuccessMessage"] = $"File '{model.Title}' uploaded successfully!";
                return RedirectToAction("Success");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while uploading the file. Please try again.");
                return View(model);
            }
        }
        public IActionResult Success()
        {
            ViewBag.Message = TempData["SuccessMessage"];
            return View();
        }
    }
}
