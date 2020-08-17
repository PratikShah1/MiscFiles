using System.ComponentModel.DataAnnotations;
using System.Web;

namespace FolderBrowserCopy.Models
{
    public class FileModel
    {
        [Required(ErrorMessage = "Please select file.")]
        [Display(Name = "Browse File")]
        public HttpPostedFileBase[] Files { get; set; }
    }
}