using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public enum DiscussionSortOptions
    {
        Latest,
        Active,
        Urgent,
        NoAnswers
    }
    public class DiscussionSorterDto
    {
        public DiscussionSortOptions SortBy { get; set; }
    }
}
