using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateDiscussionAnswerDto
    {
        public int DiscussionId { get; set; }
        public string DiscussionAnswerContent { get; set; }
        public string? DiscussionFile { get; set; }
        public int UserId { get; set; }
        public int? ParentId { get; set; }
    }
    public class DiscussionHandleVoteDto
    {
        public int UserId { get; set; }
        public int DiscussionId { get; set; }
        public DiscussionVoteType DiscussionVoteType { get; set; }
    }
    public class DiscussionAnswerHandleVoteDto
    {
        public int UserId { get; set; }
        public int DiscussionAnswerId { get; set; }
        public int DiscussionId { get; set; }
        public DiscussionVoteType DiscussionVoteType { get; set; }
    }
    public class DiscussionAnswerDto
    {
        public int AnswerId { get; set; }
        public int DiscussionId { get; set; }
        public string DiscussionAnswerContent { get; set; }
        public string? DiscussionFile { get; set; }
        public int UserId { get; set; }
        public int? ParentId { get; set; }
        public int Votes { get; set; }
        public bool? IsVotedUp { get; set; }
        public bool? IsVotedDown { get; set; }
        public string UserName { get; set; }
        public string UserProfilePic { get; set; }
        public List<DiscussionAnswerDto> Replies { get; set; } = new List<DiscussionAnswerDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
    }
}
