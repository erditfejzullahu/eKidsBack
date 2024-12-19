using Database.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ICourseCompletationService
    {
        Task<CourseCompletationResponse> CompleteCourse(int courseId, int userId, CancellationToken token);
    }
}
