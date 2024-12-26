using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class PaginationDto
    {
        private int _pageNumber = 1;
        private int _pageSize = 10;
        
        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 || value > MaxPageSize ? DefaultPageSize : value;
        }
        public int MaxPageSize { get; set; } = 100;
        public int DefaultPageSize { get; set; } = 10;
        public int Skip => (PageNumber - 1) * PageSize;
        public int Take => PageSize;

        public void Validate()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1 || PageSize > MaxPageSize) PageSize = DefaultPageSize;
        }

    }
}
