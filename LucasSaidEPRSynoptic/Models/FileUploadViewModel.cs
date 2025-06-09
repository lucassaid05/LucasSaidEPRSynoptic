using System.ComponentModel.DataAnnotations;

namespace LucasSaidEPRSynoptic.Models
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "File Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a file to upload")]
        [Display(Name = "Select File")]
        public IFormFile FileUpload { get; set; } = null!;

        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }
}
