using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Database.Repository
{
    public class DiscussionAnswerService : IDiscussionAnswerService
    {
        private readonly ILogger<DiscussionAnswerService> _logger;
        private readonly ApplicationDbContext _context;

        public DiscussionAnswerService(ILogger<DiscussionAnswerService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<int> HandleDiscussionVoteStatusAsync(int userId, int discussionId, DiscussionVoteType voteType, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var discussion = await _context.Discussions.Include(c => c.DiscussionVotes).FirstOrDefaultAsync(c => c.ID == discussionId, token);

                if (discussion == null)
                {
                    throw new KeyNotFoundException("Discussion not found");
                }

                var existingVote = discussion.DiscussionVotes.FirstOrDefault(c => c.UserId == userId);
                if (existingVote == null)
                {
                    var newVote = new DiscussionVotes
                    {
                        UserId = userId,
                        DiscussionId = discussionId,
                        IsVotedDown = voteType == DiscussionVoteType.VoteDown,
                        IsVotedUp = voteType == DiscussionVoteType.VoteUp,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    if(voteType == DiscussionVoteType.VoteDown)
                    {
                        discussion.Votes -= 1;
                    }
                    else if(voteType == DiscussionVoteType.VoteUp)
                    {
                        discussion.Votes += 1;
                    }
                    discussion.DiscussionVotes.Add(newVote);
                    _context.Discussions.Update(discussion);
                }
                else
                {
                    existingVote.LastModified = DateTime.UtcNow;
                    if (existingVote.IsVotedUp && voteType == DiscussionVoteType.VoteUp)
                    {
                        discussion.Votes -= 1;
                        existingVote.IsVotedUp = false;
                        _context.Discussions.Update(discussion);
                        _context.DiscussionVotes.Update(existingVote);
                    }
                    else if (existingVote.IsVotedDown && voteType == DiscussionVoteType.VoteDown)
                    {
                        discussion.Votes -= 1;
                        existingVote.IsVotedDown = false;
                        _context.Discussions.Update(discussion);
                        _context.DiscussionVotes.Update(existingVote);
                    }
                    else if (existingVote.IsVotedUp && voteType == DiscussionVoteType.VoteDown)
                    {
                        discussion.Votes -= 2;
                        existingVote.IsVotedUp = false;
                        existingVote.IsVotedDown = true;
                        _context.Discussions.Update(discussion);
                        _context.DiscussionVotes.Update(existingVote);
                    }
                    else if (existingVote.IsVotedDown && voteType == DiscussionVoteType.VoteUp)
                    {
                        discussion.Votes += 2;
                        existingVote.IsVotedUp = true;
                        existingVote.IsVotedDown = false;
                        _context.Discussions.Update(discussion);
                        _context.DiscussionVotes.Update(existingVote);
                    }
                }
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return voteType == DiscussionVoteType.VoteUp ? 0 : 1;   //0 for voteup, 1 for votedown
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in handling discussion vote");
                throw new ApplicationException("Error in handling discussion vote");
            }
        }

        public async Task<int> HandleAnswerVoteStatusAsync(int userId, int discussionAnswerId, int discussionId, DiscussionVoteType voteType, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var discussionAnswer = await _context.DiscussionAnswers
                    .Include(c => c.DiscussionAnswerVotes)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.DiscussionId == discussionId && c.ID == discussionAnswerId, token);

                if (discussionAnswer == null)
                {
                    throw new KeyNotFoundException("Discussion answer not found");   
                }

                var existingVote = discussionAnswer.DiscussionAnswerVotes.FirstOrDefault();
                if(existingVote == null)
                {
                    var newVote = new DiscussionAnswerVotes
                    {
                        DiscussionCommentId = discussionAnswerId,
                        UserId = userId,
                        IsVotedDown = voteType == DiscussionVoteType.VoteDown,
                        IsVotedUp = voteType == DiscussionVoteType.VoteUp,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    if(voteType == DiscussionVoteType.VoteUp)
                    {
                        discussionAnswer.Votes += 1;
                    }
                    else
                    {
                        discussionAnswer.Votes -= 1;
                    }
                    discussionAnswer.DiscussionAnswerVotes.Add(newVote);
                    _context.DiscussionAnswers.Update(discussionAnswer);
                }
                else
                {
                    existingVote.LastModified = DateTime.UtcNow;
                    if (existingVote.IsVotedUp && voteType == DiscussionVoteType.VoteUp)
                    {
                        discussionAnswer.Votes -= 1;
                        existingVote.IsVotedUp = false;
                        _context.DiscussionAnswers.Update(discussionAnswer);
                        _context.DiscussionAnswerVotes.Update(existingVote);
                    } 
                    else if (existingVote.IsVotedDown && voteType == DiscussionVoteType.VoteDown)
                    {
                        discussionAnswer.Votes -= 1;
                        existingVote.IsVotedDown = false;
                        _context.DiscussionAnswers.Update(discussionAnswer);
                        _context.DiscussionAnswerVotes.Update(existingVote);
                    }
                    else if(existingVote.IsVotedUp && voteType == DiscussionVoteType.VoteDown)
                    {
                        discussionAnswer.Votes -= 2;
                        existingVote.IsVotedUp = false;
                        existingVote.IsVotedDown = true;
                        _context.DiscussionAnswers.Update(discussionAnswer);
                        _context.DiscussionAnswerVotes.Update(existingVote);
                    }
                    else if(existingVote.IsVotedDown && voteType == DiscussionVoteType.VoteUp)
                    {
                        discussionAnswer.Votes += 2;
                        existingVote.IsVotedUp = true;
                        existingVote.IsVotedDown = false;
                        _context.DiscussionAnswers.Update(discussionAnswer);
                        _context.DiscussionAnswerVotes.Update(existingVote);
                    }
                }
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return voteType == DiscussionVoteType.VoteUp ? 0 : 1;   //0 for voteup, 1 for votedown
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in handling vote status");
                throw new ApplicationException("Error in handling vote status");
            }
        }

        public async Task<(List<DiscussionAnswerDto>, bool hasMore)> GetDiscussionAnswersDtoAsync(int discussionId, int userId, PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var answersCount = await _context.DiscussionAnswers.Where(c => c.DiscussionId == discussionId).CountAsync(token);
                var answers = await _context.DiscussionAnswers
                    .Where(c => c.DiscussionId.Equals(discussionId))
                    .Select(c => new DiscussionAnswerDto
                    {
                        AnswerId = c.ID,
                        DiscussionId = c.DiscussionId,
                        DiscussionAnswerContent = c.Content,
                        DiscussionFile = c.Item_Url,
                        UserId = c.UserId,
                        ParentId = c.ParentId,
                        Votes = c.Votes,
                        IsVotedUp = c.DiscussionAnswerVotes.Any(dv => dv.UserId == userId && dv.DiscussionCommentId == c.ID && dv.IsVotedUp == true),
                        IsVotedDown = c.DiscussionAnswerVotes.Any(dv => dv.UserId == userId && dv.DiscussionCommentId == c.ID && dv.IsVotedDown == true),
                        UserName = c.User.Firstname + " " + c.User.Lastname,
                        UserProfilePic = c.User.ProfilePictureUrl,
                        CreatedAt = c.CreatedAt,
                        LastModified = c.LastModified,
                        Replies = new List<DiscussionAnswerDto>()
                    })
                    .OrderByDescending(c => c.Votes)
                    .ToListAsync(token);

                var answerLookup = answers.ToLookup(c => c.ParentId);

                List<DiscussionAnswerDto> BuildHierarky(int? parentId)
                {
                    return answerLookup[parentId]
                        .Select(c => new DiscussionAnswerDto
                        {
                            AnswerId = c.AnswerId,
                            DiscussionId = c.DiscussionId,
                            DiscussionAnswerContent = c.DiscussionAnswerContent,
                            DiscussionFile = c.DiscussionFile,
                            UserId = c.UserId,
                            ParentId = c.ParentId,
                            Votes = c.Votes,
                            IsVotedUp = c.IsVotedUp,
                            IsVotedDown = c.IsVotedDown,
                            UserName = c.UserName,
                            UserProfilePic = c.UserProfilePic,
                            CreatedAt = c.CreatedAt,
                            LastModified = c.LastModified,
                            Replies = BuildHierarky(c.AnswerId)
                        })
                        .OrderByDescending(c => c.Votes)
                        .Skip(paginationDto.Skip)
                        .Take(paginationDto.Take)
                        .ToList();
                }

                bool hasMore = BuildHierarky(null).Count == paginationDto.Take && BuildHierarky(null).Count < answersCount;
                return (BuildHierarky(null), hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting discussion answers");
                throw new ApplicationException("Error getting discussion answers");
            }
        }
    }
}
