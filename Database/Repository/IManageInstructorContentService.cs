using Database.DTOs;
using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Database.Repository.ManageInstructorContentService;

namespace Database.Repository
{
    public interface IManageInstructorContentService
    {
        Task<(List<dynamic>, bool hasMore)> RetrieveInstructorActivities(InstructorManageUserDto userDto, InstructorsManageContentType manageType, SortQueryDto sortQueryDto, PaginationDto paginationDto);
    }
}
