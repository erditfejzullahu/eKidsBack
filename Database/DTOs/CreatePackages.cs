using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreatePackages
    {
        [Required(ErrorMessage = "PackageName is required")]
        public string PackageName { get; set; }

        [Required(ErrorMessage = "PackageContent is required")]
        public string PackageContent { get; set; }

        [Required(ErrorMessage = "PackageValue is required")]
        public string PackageValue { get; set; }

        [Required(ErrorMessage = "PackageFeatured is required")]
        public string PackageFeatured { get; set; }

    }
}
