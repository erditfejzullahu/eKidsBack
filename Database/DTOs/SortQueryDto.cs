using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class SortQueryDto
    {
        public string? SortByName { get; set; }
        public string? SortNameOrder { get; set; }
        public string? SortByDate { get; set; }
        public string? SortDateOrder { get; set; }
        public string? SortByViews { get; set; }
        public string? SortViewOrder { get; set; }
    }

}
