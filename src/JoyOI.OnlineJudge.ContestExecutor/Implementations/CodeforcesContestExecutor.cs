using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using JoyOI.OnlineJudge.Models;
using JoyOI.ManagementService.SDK;
using Newtonsoft.Json;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class CodeforcesContestExecutor : DefaultContestExecutor
    {
        public ManagementServiceClient ManagementService;

        public IConfiguration Configuration;

        /*
         * Point: 最终得分
         * Point2: Hack成功数 - Hack失败数
         * Point3: Hack失败数
         * Point4: 提交次数
         */
        public override IDictionary<string, string> PointColumnDefinations => new Dictionary<string, string>
        {
            { "Point", "Point" },
            { "Point2", "Hack" }
        };

        public override bool AllowFilterByJudgeResult => false;

        public override PushNotificationType PushNotificationSetting => PushNotificationType.All;

        public override void OnShowJudgeResult(JudgeStatus status)
        {
            if (IsContestInProgress())
            {
                var testCases = GetValidTestCasesAsync(status.ProblemId, status.UserId, default(CancellationToken)).Result;
                var hack = DB.HackStatuses.FirstOrDefault(x => x.ContestId == ContestId && x.JudgeStatusId == status.Id && x.Result == HackResult.Succeeded);

                status.SubStatuses = DB.SubJudgeStatuses
                    .Where(x => x.StatusId == status.Id)
                    .Where(x => testCases.Contains(x.TestCase.Id))
                    .ToList();

                foreach (var x in status.SubStatuses)
                {
                    x.Hint = "Codeforces赛制不提供详细测试点信息";
                }

                status.Result = status.SubStatuses.Max(x => x.Result);
                status.TimeUsedInMs = status.SubStatuses.Sum(x => x.TimeUsedInMs);
                status.MemoryUsedInByte = status.SubStatuses.Max(x => x.MemoryUsedInByte);
            }
        }

        public override void OnJudgeCompleted(JudgeStatus status)
        {
            var attendee = DB.Attendees
                .Single(x => x.ContestId == status.ContestId && x.UserId == status.UserId);

            var cpls = DB.ContestProblemLastStatuses
                .Include(x => x.Status)
                .SingleOrDefault(x => x.ProblemId == status.ProblemId && x.UserId == status.UserId && x.ContestId == status.ContestId);

            var contestProblem = DB.ContestProblems.Single(x => x.ContestId == status.ContestId && x.ProblemId == status.ProblemId);
            var duration = ComputeTimeSpan(status.CreatedTime, status.UserId);

            var validTestCases = GetValidTestCasesAsync(status.ProblemId, status.UserId, default(CancellationToken)).Result;
            bool isHackable = false;
            if (validTestCases.Count() == 0)
                isHackable = true;
            else
                isHackable = status.SubStatuses.Where(x => validTestCases.Contains(x.TestCaseId.Value)).Select(x => x.Result).Max() == JudgeResult.Accepted;
            
            if (cpls == null)
            {
                cpls = new ContestProblemLastStatus
                {
                    ProblemId = status.ProblemId,
                    ContestId = status.ContestId,
                    UserId = status.UserId,
                    StatusId = status.Id,
                    Point = status.Result == JudgeResult.Accepted ? CaculatePoint(contestProblem.Point, duration, 0) : 0,
                    Point2 = 0,
                    Point3 = 0,
                    Point4 = 1,
                    TimeSpan = duration,
                    IsAccepted = status.Result == JudgeResult.Accepted,
                    IsVirtual = attendee.IsVirtual,
                    IsHackable = isHackable
                };
                DB.ContestProblemLastStatuses.Add(cpls);
            }
            else
            {
                cpls.StatusId = status.Id;
                cpls.Point = status.Result == JudgeResult.Accepted ? CaculatePoint(contestProblem.Point, ComputeTimeSpan(status.CreatedTime, status.UserId), cpls.Point4) : 0;
                cpls.Point4++;
                cpls.TimeSpan = duration;
                cpls.IsAccepted = status.Result == JudgeResult.Accepted;
                cpls.IsHackable = isHackable;
                cpls.IsHacked = isHackable ? false : cpls.IsHacked;
            }
            DB.SaveChanges();
        }

        public override void OnHackCompleted(HackStatus status)
        {
            if (status.Result == HackResult.Succeeded)
            {
                // 1. Set the status to be non-hackable
                DB.ContestProblemLastStatuses
                    .Where(x => x.StatusId == status.JudgeStatusId && x.ContestId == ContestId)
                    .SetField(x => x.IsHackable).WithValue(false)
                    .SetField(x => x.IsHacked).WithValue(true)
                    .Update();

                // 2. Add the hack data to problem
                var input = status.HackDataBlobId.Value;
                var testCase = DB.TestCases.FirstOrDefault(x => x.InputBlobId == input && x.ProblemId == status.Status.ProblemId);
                var testCaseExisted = testCase != null;
                if (!testCaseExisted)
                {
                    var inputLength = ManagementService.GetBlobAsync(input).Result.Body.Length;
                    var stateMachine = ManagementService.GetStateMachineInstanceAsync(status.RelatedStateMachineIds.Last().StateMachineId).Result;
                    var output = stateMachine.StartedActors.First(x => x.Tag == "Standard").Outputs.First(x => x.Name == "stdout.txt").Id;
                    var outputLength = ManagementService.GetBlobAsync(output).Result.Body.Length;

                    testCase = new TestCase
                    {
                        ContestId = ContestId,
                        InputBlobId = input,
                        InputSizeInByte = inputLength,
                        OutputBlobId = output,
                        OutputSizeInByte = outputLength,
                        ProblemId = status.Status.ProblemId,
                        Type = TestCaseType.Hack
                    };
                    DB.TestCases.Add(testCase);
                    DB.SaveChanges();
                }

                // 3. Add the result into sub judge status
                if (!testCaseExisted)
                {
                    var sub = new SubJudgeStatus
                    {
                        SubId = DB.SubJudgeStatuses.Where(x => x.StatusId == status.JudgeStatusId).Count(),
                        Hint = status.Hint,
                        MemoryUsedInByte = status.MemoryUsedInByte,
                        TimeUsedInMs = status.TimeUsedInMs,
                        Result = status.HackeeResult,
                        InputBlobId = testCase.InputBlobId,
                        OutputBlobId = testCase.OutputBlobId,
                        TestCaseId = testCase.Id,
                        StatusId = status.JudgeStatusId
                    };
                    DB.SubJudgeStatuses.Add(sub);
                    DB.SaveChanges();
                }

                // 4. Add point for the hacker
                DB.ContestProblemLastStatuses
                    .Where(x => x.ContestId == ContestId && x.ProblemId == status.Status.ProblemId && x.UserId == status.UserId)
                    .SetField(x => x.Point2).Plus(1)
                    .Update();

                // 5. Hack all statuses
                if (!testCaseExisted && DB.Attendees.Any(x => x.ContestId == ContestId && x.UserId == status.UserId && !x.IsVirtual))
                {
                    var affectedStatuses = DB.ContestProblemLastStatuses
                        .Include(x => x.Status)
                        .Where(x => x.ProblemId == status.Status.ProblemId && x.ContestId == ContestId && x.Status.BinaryBlobId.HasValue && x.IsAccepted)
                        .ToList();

                    var problem = DB.Problems.Single(x => x.Id == status.Status.ProblemId);
                    var validatorId = problem.ValidatorBlobId.HasValue ? problem.ValidatorBlobId.Value : Guid.Parse(Configuration["JoyOI:StandardValidatorBlobId"]);

                    var blobs = new List<BlobInfo>(affectedStatuses.Count + 10);
                    blobs.Add(new BlobInfo
                    {
                        Id = ManagementService.PutBlobAsync("limit.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                        {
                            UserTime = status.Status.Problem.TimeLimitationPerCaseInMs,
                            PhysicalTime = status.Status.Problem.TimeLimitationPerCaseInMs * 4,
                            Memory = status.Status.Problem.MemoryLimitationPerCaseInByte
                        }))).Result,
                        Name = "limit.json",
                        Tag = "Problem=" + status.Status.ProblemId
                    });
                    blobs.Add(new BlobInfo(validatorId, problem.ValidatorBlobId.HasValue ? "Validator" + Constants.GetBinaryExtension(problem.ValidatorLanguage) : "Validator.out"));
                    blobs.Add(new BlobInfo(status.HackDataBlobId.Value, "data.txt", testCase.Id.ToString()));
                    blobs.Add(new BlobInfo(testCase.OutputBlobId, "std.txt", testCase.Id.ToString()));
                    foreach (var x in affectedStatuses)
                    {
                        blobs.Add(new BlobInfo(x.Status.BinaryBlobId.Value, "Hackee" + Constants.GetBinaryExtension(x.Status.Language), x.StatusId.ToString()));
                    }

                    ManagementService.PutStateMachineInstanceAsync("HackAllStateMachine", Configuration["ManagementService:CallBack"], blobs, 2);
                }
            }
            else if (status.Result == HackResult.Failed)
            {
                DB.ContestProblemLastStatuses
                    .Where(x => x.ContestId == ContestId && x.ProblemId == status.Status.ProblemId && x.UserId == status.UserId)
                    .SetField(x => x.Point2).Subtract(1)
                    .SetField(x => x.Point3).Plus(1)
                    .Update();
            }
        }

        public override bool IsAvailableToGetStandings(string username = null) => true;

        public override void GenerateProblemScoreDisplayText(Attendee src)
        {
            foreach (var x in src.detail.Values)
            {
                if (x.isAccepted)
                {
                    x.display = x.point + "\r\n" + string.Format("{0}:{1}", (int)x.timeSpan.TotalMinutes / 60, (int)x.timeSpan.TotalMinutes % 60 == 0 ? "00" : ((int)x.timeSpan.TotalMinutes % 60).ToString());
                }
                else if (x.point4 != 0)
                {
                    x.display = $"-{x.point4}";
                }
                else
                {
                    x.display = "";
                }
            }
        }

        public override void GenerateTotalScoreDisplayText(Attendee src)
        {
            src.pointDisplay = src.point.ToString();

            var hackSucceeded = src.point2 + src.point3;
            var hackFailed = src.point3;
            if (hackSucceeded == 0 && hackFailed == 0)
            {
                src.point2Display = "";
            }
            else if (hackSucceeded > 0 && hackFailed == 0)
            {
                src.point2Display = "+" + hackSucceeded;
            }
            else if (hackSucceeded == 0 && hackFailed > 0)
            {
                src.point2Display = "-" + hackFailed;
            }
            else
            {
                src.point2Display = string.Format("+{0} : -{1}", hackSucceeded, hackFailed);
            }
        }

        public override bool IsStandingsAvailable(string username = null)
        {
            return Contest.Status != ContestStatus.Pending;
        }

        public override string GenerateProblemStatusText(string problemId, string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null)
            {
                return null;
            }

            if (IsContestInProgress(username))
            {
                var cpls = DB.ContestProblemLastStatuses
                    .Include(x => x.Status)
                    .SingleOrDefault(x => x.ContestId == ContestId && x.UserId == user.Id && x.ProblemId == problemId);

                if (cpls == null)
                {
                    return null;
                }
                else if (cpls.IsLocked)
                {
                    return "Locked";
                }
                else
                {
                    var status = cpls.Status;
                    OnShowJudgeResult(status);
                    if (status.Result == JudgeResult.Accepted)
                    {
                        return "Accepted";
                    }
                    else
                    {
                        return $"{cpls.Point4} Fails";
                    }
                }
            }
            else
            {
                var cpls = DB.ContestProblemLastStatuses
                    .SingleOrDefault(x => x.ContestId == ContestId && x.UserId == user.Id && x.ProblemId == problemId);

                if (cpls == null)
                {
                    return null;
                }
                else
                {
                    if (cpls.IsLocked)
                    {
                        return "Locked";
                    }
                    else if (cpls.IsAccepted)
                    {
                        return "Accepted";
                    }
                    else
                    {
                        return $"{cpls.Point4} Fails";
                    }
                }

            }
        }

        private TimeSpan ComputeTimeSpan(DateTime? statusTime = null, Guid? userId = null)
        {
            if (statusTime == null)
            {
                statusTime = DateTime.UtcNow;
            }

            if (userId == null)
            {
                userId = User.Current == null ? null : (Guid?)User.Current.Id;
            }
            
            if (userId == null)
            {
                return statusTime.Value - Contest.Begin;
            }

            var attendee = DB.Attendees.SingleOrDefault(x => x.UserId == userId && x.ContestId == ContestId);
            if (attendee == null)
            {
                return statusTime.Value - Contest.Begin;
            }
            else
            {
                return statusTime.Value - (attendee.IsVirtual ? attendee.RegisterTime : Contest.Begin);
            }
        }

        public override bool IsAbleToSubmitProblem(string problemId, string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null || !IsContestInProgress(username))
            {
                return false;
            }

            var cpls = DB.ContestProblemLastStatuses.SingleOrDefault(x => x.UserId == user.Id && x.ContestId == ContestId && x.ProblemId == problemId);
            if (cpls == null)
            {
                return true;
            }
            else
            {
                return cpls.IsLocked == false;
            }
        }

        public override bool IsStatusHackable(JudgeStatus status)
        {
            var signedIn = User.IsSignedIn();
            var statusIsHackable = DB.ContestProblemLastStatuses.Any(x => x.ContestId == ContestId && x.StatusId == status.Id && x.IsHackable);
            var problemLocked = DB.ContestProblemLastStatuses.Any(x => x.ContestId == ContestId && x.UserId == User.Current.Id && x.ProblemId == status.ProblemId && x.IsLocked);
            return signedIn && statusIsHackable && problemLocked;
        }

        private int CaculatePoint(int full, TimeSpan duration, int submit)
        {
            var least = Convert.ToInt32(full * 0.2);
            var penalty = Convert.ToInt32(duration.TotalMinutes) * Convert.ToInt32(full * 0.01) + (submit - 1) * 50;
            var points = full - penalty;
            return points > least ? points : least;
        }

        public override async Task<IEnumerable<Attendee>> GenerateFullStandingsAsync(bool includingVirtual = true, CancellationToken token = default(CancellationToken))
        {
            var exceptNestedQuery = DB.Attendees
               .Where(x => x.ContestId == ContestId)
               .Where(x => x.IsVirtual)
               .Select(x => x.UserId);

            IQueryable<ContestProblemLastStatus> query = DB.ContestProblemLastStatuses
                .Include(x => x.Status)
                .ThenInclude(x => x.SubStatuses)
                .Include(x => x.Problem)
                .ThenInclude(x => x.TestCases)
                .Where(x => x.ContestId == ContestId);

            if (!includingVirtual)
                query = query.Where(x => !exceptNestedQuery.Contains(x.UserId));

            var source = await query
               .GroupBy(x => new { x.UserId, x.IsVirtual })
               .ToListAsync(token);


            List<CodeforcesAttendee> attendees;
            var hasPermission = HasPermissionToContest();
            if (!hasPermission && !IsContestEnded())
            {
                var contestProblems = await DB.ContestProblems
                    .Where(x => x.ContestId == ContestId)
                    .ToDictionaryAsync(x => x.ProblemId);

                foreach (var group in source)
                    foreach (var x in group)
                        await FilterCplsAsync(x, contestProblems, token);
            }

            attendees = source
              .Select(x => new CodeforcesAttendee
              {
                  userId = x.Key.UserId,
                  isVirtual = x.Key.IsVirtual,
                  detail = x.GroupBy(y => y.ProblemId)
                      .Select(y => new Detail
                      {
                          problemId = y.Key,
                          isAccepted = y.First().IsAccepted,
                          isHackable = x.First().IsHackable,
                          point = y.First().Point,
                          point2 = y.First().Point2,
                          point3 = y.First().Point3,
                          point4 = y.First().Point4,
                          statusId = y.First().StatusId.ToString(),
                          timeSpan = y.First().TimeSpan,
                          timeSpan2 = y.First().TimeSpan2
                      })
                      .ToDictionary(y => y.problemId)
              })
              .OrderByDescending(x => x.point)
              .ThenByDescending(x => x.point2)
              .ThenBy(x => x.point3)
              .ThenBy(x => x.point4)
              .ThenBy(x => x.timeSpan)
              .ThenBy(x => x.timeSpan2)
              .ToList();

            foreach (var x in attendees)
            {
                this.GenerateProblemScoreDisplayText(x);
                this.GenerateTotalScoreDisplayText(x);
            }

            return attendees;
        }

        public override async Task<Attendee> GenerateSingleStandingsAsync(string username = null, CancellationToken token = default(CancellationToken))
        {
            if (username == null && User.Current != null)
            {
                username = User.Current.UserName;
            }

            var statuses = await DB.ContestProblemLastStatuses
                .Where(x => x.ContestId == ContestId)
                .Where(x => x.User.UserName == username)
                .ToListAsync(token);

            if (statuses.Count == 0)
                return null;

            var ret = new Attendee
            {
                userId = statuses.First().UserId,
                detail = statuses
                    .GroupBy(x => x.ProblemId)
                    .Select(x => new Detail
                    {
                        isAccepted = x.First().IsAccepted,
                        isHackable = x.First().IsHackable,
                        point = x.First().Point,
                        point2 = x.First().Point2,
                        point3 = x.First().Point3,
                        point4 = x.First().Point4,
                        timeSpan = x.First().TimeSpan,
                        timeSpan2 = x.First().TimeSpan2,
                        problemId = x.First().ProblemId,
                        statusId = x.First().StatusId.ToString()
                    })
                    .ToDictionary(x => x.problemId)
            };
            
            this.GenerateTotalScoreDisplayText(ret);
            this.GenerateProblemScoreDisplayText(ret);

            return ret;
        }

        private async Task FilterCplsAsync(ContestProblemLastStatus cpls, IDictionary<string, ContestProblem> contestProblems, CancellationToken token)
        {
            var validTestCases = (await GetValidTestCasesAsync(cpls.ProblemId, cpls.UserId, token)).ToList();
            cpls.Status.SubStatuses = cpls.Status.SubStatuses
                .Where(x => x.TestCaseId.HasValue && validTestCases.Contains(x.TestCaseId.Value))
                .ToList();

            cpls.Status.Result = cpls.Status.SubStatuses.Max(x => x.Result);
            cpls.IsAccepted = cpls.Status.Result == JudgeResult.Accepted;
            cpls.Point = cpls.IsAccepted ? CaculatePoint(contestProblems[cpls.ProblemId].Point, ComputeTimeSpan(cpls.Status.CreatedTime, cpls.UserId), cpls.Point4) : 0;
        }

        private async Task<IEnumerable<Guid>> GetValidTestCasesAsync(string problem, Guid userId, CancellationToken token)
        {
            var ret = new List<Guid>(5);
            var inputIds = DB.HackStatuses
                .Where(x => x.ContestId == ContestId && x.Status.UserId == userId && x.Status.ProblemId == problem)
                .Select(x => x.HackDataBlobId);
            var testCases = await DB.TestCases
                .Where(x => x.ProblemId == problem)
                .Where(x => inputIds.Contains(x.InputBlobId) && x.Type != TestCaseType.Sample || x.Type == TestCaseType.Small)
                .Select(x => x.Id)
                .ToListAsync(token);
            return testCases;
        }

        public override void OnShowHackResult(HackStatus status)
        {
            status.HackDataBody = null;
        }
    }
}
