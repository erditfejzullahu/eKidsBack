using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdatePackages
    {
        public string PackageName { get; set; }
        public string PackageContent { get; set; }
        public string PackageValue { get; set; }
        public string PackageFeatured { get; set; }

    }
}
