using Database.DTOs;
using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IDiscussionAnswerService
    {
        Task<int> HandleAnswerVoteStatusAsync(int userId, int discussionAnswerId, int discussionId, DiscussionVoteType voteType, CancellationToken token);
        Task<(List<DiscussionAnswerDto>, bool hasMore)> GetDiscussionAnswersDtoAsync(int discussionId, int userId, PaginationDto paginationDto, CancellationToken token);
        Task<int> HandleDiscussionVoteStatusAsync(int userId, int discussionId, DiscussionVoteType voteType, CancellationToken token);
    }
}
