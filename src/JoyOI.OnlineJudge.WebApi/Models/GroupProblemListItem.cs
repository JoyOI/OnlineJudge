using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class GroupProblemListItem : Problem
    {
        public GroupProblemListItem(Problem problem)
        {
            this.Id = problem.Id;
            this.Body = problem.Body;
            this.CachedAcceptedCount = problem.CachedAcceptedCount;
            this.CachedSubmitCount = problem.CachedSubmitCount;
            this.CreatedTime = problem.CreatedTime;
            this.Difficulty = problem.Difficulty;
            this.IsVisible = problem.IsVisible;
            this.Title = problem.Title;
            this.Tags = problem.Tags;
        }

        public bool IsAddedToGroup { get; set; }
    }
}
