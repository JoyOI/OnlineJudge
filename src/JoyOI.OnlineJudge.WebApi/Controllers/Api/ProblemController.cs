using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using JoyOI.ManagementService.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.OnlineJudge.WebApi.Hubs;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ProblemController : BaseController
    {
        public const string LocalProblemSetTag = "按题库:本地";

        #region Problem
        [HttpGet("all")]
        public async Task<IActionResult> Get(string title, int? difficulty, string tag, int? page, CancellationToken token)
        {
            IQueryable<Problem> ret = DB.Problems;

            if (User.Current != null)
            {
                if (!IsMasterOrHigher)
                {
                    var editableProblemIds = await DB.UserClaims
                        .Where(x => x.UserId == User.Current.Id && x.ClaimType == Constants.ProblemEditPermission)
                        .Select(x => x.ClaimValue)
                        .ToListAsync(token);

                    ret = ret.Where(x => x.IsVisible || editableProblemIds.Contains(x.Id));
                }
            }
            else
            {
                ret = ret.Where(x => x.IsVisible);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                ret = ret.Where(x => x.Title.Contains(title));
            }

            if (difficulty.HasValue)
            {
                ret = ret.Where(x => x.Difficulty == difficulty.Value);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var tags = tag.Split(",").Select(x => x.Trim());
                if (tags.Count() > 0)
                {
                    foreach (var t in tags.GroupBy(x => x.Split(':')[0]))
                    {
                        ret = ret.Where(ContainsWhere<Problem>(x => x.Tags, t));
                    }
                }
            }

            if (!page.HasValue)
            {
                page = 1;
            }

            return await Paged(ret.OrderBy(x => x.Source).ThenBy(x => x.CreatedTime), page.Value, 100, token);
        }

        [HttpGet("title")]
        public async Task<object> GetTitles(string problemIds, CancellationToken token)
        {
            var ids = problemIds
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            var ret = await DB.Problems
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.Title })
                .ToDictionaryAsync(x => x.Id, token);

            return Result(ret);
        }

        [HttpGet("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Get(string id, string contestId, CancellationToken token)
        {
            this.HasOwnership = await HasPermissionToProblemAsync(id, token);
            var ret = await DB.Problems.SingleOrDefaultAsync(x => x.Id == id, token);
            if (!string.IsNullOrEmpty(contestId) && !await IsContestAttendeeAbleToAccessProblem(id, contestId, token))
            {
                return Result(403, "You don't have the permission to view this problem.");
            }
            if ((ret == null || !ret.IsVisible && !this.HasOwnership) && string.IsNullOrEmpty(contestId))
            {
                return Result(404, "Not Found");
            }
            else
            {
                return Result(ret);
            }
        }

        [HttpPost("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        [HttpPatch("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Patch(
            string id, 
            [FromServices] ManagementServiceClient MgmtSvc,
            [FromServices] StateMachineAwaiter Awaiter,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(id, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                var problem = await DB.Problems.SingleOrDefaultAsync(x => x.Id == id, token);
                if (problem == null)
                {
                    return Result(404, "Not Found");
                }

                var fields = PatchEntity(problem, RequestBody);

                // Check difficulty range
                if (fields.Any(x => x == nameof(Problem.Difficulty)) && (problem.Difficulty < 0 || problem.Difficulty > 9))
                {
                    return Result(400, "The difficulty must be 0-9.");
                }

                // Update validator
                if (fields.Any(x => x == nameof(Problem.ValidatorCode)) || fields.Any(x => x == nameof(Problem.ValidatorLanguage)))
                {
                    if (string.IsNullOrWhiteSpace(problem.ValidatorCode))
                    {
                        problem.ValidatorBlobId = null;
                        problem.ValidatorCode = null;
                        problem.ValidatorError = null;
                        problem.ValidatorLanguage = null;
                    }
                    else
                    {
                        var validatorCodeId = await MgmtSvc.PutBlobAsync("validator-" + problem.Id, Encoding.UTF8.GetBytes(problem.ValidatorCode), token);
                        var stateMachineId = await MgmtSvc.PutStateMachineInstanceAsync("CompileOnlyStateMachine", "http://joyoitest.1234.sh", new BlobInfo[] { new BlobInfo(validatorCodeId, "Main" + Constants.GetSourceExtension(problem.ValidatorLanguage)) });
                        var result = await Awaiter.GetStateMachineResultAsync(stateMachineId, true, token);
                        if (result.StartedActors.Any(x => x.Name == "CompileActor" && x.Status == JoyOI.ManagementService.Model.Enums.ActorStatus.Succeeded))
                        {
                            problem.ValidatorBlobId = result.StartedActors.Last().Outputs.Single(x => x.Name.StartsWith("Main.")).Id;
                            problem.ValidatorError = null;
                        }
                        else
                        {
                            problem.ValidatorBlobId = null;
                            problem.ValidatorError = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(result.StartedActors.Last().Outputs.Single(x => x.Name == "runner.txt").Id, token)).Body)).Error;
                        }
                    }
                }

                // Update standard
                if (fields.Any(x => x == nameof(Problem.StandardCode)) || fields.Any(x => x == nameof(Problem.StandardLanguage)))
                {
                    if (string.IsNullOrWhiteSpace(problem.StandardCode))
                    {
                        problem.StandardBlobId = null;
                        problem.StandardCode = null;
                        problem.StandardError = null;
                        problem.StandardLanguage = null;
                    }
                    else
                    {
                        var standardCodeId = await MgmtSvc.PutBlobAsync("standard-" + problem.Id, Encoding.UTF8.GetBytes(problem.ValidatorCode), token);
                        var stateMachineId = await MgmtSvc.PutStateMachineInstanceAsync("CompileOnlyStateMachine", "http://joyoitest.1234.sh", new BlobInfo[] { new BlobInfo(standardCodeId, "Main" + Constants.GetSourceExtension(problem.StandardLanguage)) });
                        var result = await Awaiter.GetStateMachineResultAsync(stateMachineId, true, token);
                        if (result.StartedActors.Any(x => x.Name == "CompileActor" && x.Status == JoyOI.ManagementService.Model.Enums.ActorStatus.Succeeded))
                        {
                            problem.StandardBlobId = result.StartedActors.Last().Outputs.Single(x => x.Name.StartsWith("Main.")).Id;
                            problem.StandardError = null;
                        }
                        else
                        {
                            problem.StandardBlobId = null;
                            problem.StandardError = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(result.StartedActors.Last().Outputs.Single(x => x.Name == "runner.txt").Id, token)).Body)).Error;
                        }
                    }
                }

                // Update range
                if (fields.Any(x => x == nameof(Problem.RangeCode)) || fields.Any(x => x == nameof(Problem.RangeLanguage)))
                {
                    if (string.IsNullOrWhiteSpace(problem.RangeCode))
                    {
                        problem.RangeBlobId = null;
                        problem.RangeCode = null;
                        problem.RangeError = null;
                        problem.RangeLanguage = null;
                    }
                    else
                    {
                        var rangeCodeId = await MgmtSvc.PutBlobAsync("range-" + problem.Id, Encoding.UTF8.GetBytes(problem.RangeCode), token);
                        var stateMachineId = await MgmtSvc.PutStateMachineInstanceAsync("CompileOnlyStateMachine", "http://joyoitest.1234.sh", new BlobInfo[] { new BlobInfo(rangeCodeId, "Main" + Constants.GetSourceExtension(problem.RangeLanguage)) });
                        var result = await Awaiter.GetStateMachineResultAsync(stateMachineId, true, token);
                        if (result.StartedActors.Any(x => x.Name == "CompileActor" && x.Status == JoyOI.ManagementService.Model.Enums.ActorStatus.Succeeded))
                        {
                            problem.RangeBlobId = result.StartedActors.Last().Outputs.Single(x => x.Name.StartsWith("Main.")).Id;
                            problem.RangeError = null;
                        }
                        else
                        {
                            problem.RangeBlobId = null;
                            problem.RangeError = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(result.StartedActors.Last().Outputs.Single(x => x.Name == "runner.txt").Id, token)).Body)).Error;
                        }
                    }
                }

                if ((string.IsNullOrWhiteSpace(problem.Tags) || problem.Tags.IndexOf(LocalProblemSetTag) < 0) && problem.Source == ProblemSource.Local)
                {
                    problem.Tags += "," + LocalProblemSetTag;
                    problem.Tags = problem.Tags.Trim(',');
                }

                await DB.SaveChangesAsync(token);

                hub.Clients.All.InvokeAsync("ItemUpdated", "problem", problem.Id);
                if (fields.Any(x => x == nameof(Problem.Title)) || fields.Any(x => x == nameof(Problem.IsVisible)))
                {
                    hub.Clients.All.InvokeAsync("ItemUpdated", "problem-list", problem.Id);
                }

                return Result(200, "Patch Succeeded");
            }
        }
        
        [HttpPut("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Put(string id, CancellationToken token)
        {
            if (await DB.Problems.AnyAsync(x => x.Id == id, token))
            {
                return Result(400, "The problem id is already exists, please pick another one.");
            }

            var reservedConfig = await DB.Configurations
                .SingleOrDefaultAsync(x => x.Key == "reservedproblemidprefixes", token);

            if (reservedConfig != null)
            {
                var reserved = JsonConvert.DeserializeObject<IEnumerable<string>>(reservedConfig.Value);
                if (reserved.Any(x => id.ToLower().StartsWith(x)))
                {
                    return Result(400, "The problem id is already exists, please pick another one.");
                }
            }

            var problem = PutEntity<Problem>(RequestBody).Entity;
            problem.Id = id;
            problem.Tags = LocalProblemSetTag;

            // 处理比较器
            if (problem.ValidatorBlobId.HasValue)
            {
                // 检查使用的Blob是否合法
                if (!await DB.SharedValidators.AnyAsync(x => x.Id == problem.ValidatorBlobId.Value, token) && await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles))
                {
                    return Result(401, "You don't have the permission to the specified validator, id=" + problem.ValidatorBlobId.Value);
                }
            }
            else if (string.IsNullOrWhiteSpace(problem.ValidatorCode) && string.IsNullOrEmpty(problem.ValidatorLanguage))
            {
                problem.ValidatorBlobId = null;
            }
            else
            {
                // 如果用户上传了比较器代码
                if (Constants.SupportedLanguages.Contains(problem.ValidatorLanguage))
                {
                    // 编译自定义比较器并缓存编译后的blob
                    await CompileAsync(id, problem.ValidatorCode, problem.ValidatorLanguage, token);
                }
                else
                {
                    // 直接缓存比较器脚本的blob
                    problem.ValidatorBlobId = await ManagementService.PutBlobAsync(id + ".validator", Encoding.UTF8.GetBytes(problem.ValidatorCode), token);
                }
            }

            DB.Problems.Add(problem);
            await DB.SaveChangesAsync(token);
            await User.Manager.AddClaimAsync(User.Current, new System.Security.Claims.Claim(Constants.ProblemEditPermission, problem.Id));
            return Result(200, "Put Succeeded");
        }
        
        [HttpDelete("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Delete(string id, CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(id, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                await DB.UserClaims
                    .Where(x => x.ClaimType == Constants.ProblemEditPermission)
                    .Where(x => x.ClaimValue == id)
                    .DeleteAsync(token);
                await DB.Problems
                    .Where(x => x.Id == id)
                    .DeleteAsync(token);
                return Result(200, "Delete Succeeded");
            }
        }
        #endregion

        #region Test Case
        [HttpGet("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/all")]
        public async Task<IActionResult> GetTestCase(
            [FromServices] ManagementServiceClient MgmtSvc,
            string problemId,
            string contestId,
            TestCaseType? type, 
            bool? showContent,
            CancellationToken token)
        {
            this.HasOwnership = await HasPermissionToProblemAsync(problemId, token);
            var problem = await DB.Problems.SingleOrDefaultAsync(x => x.Id == problemId, token);

            if (!string.IsNullOrEmpty(contestId) && !await IsContestAttendeeAbleToAccessProblem(problemId, contestId, token))
            {
                return Result(403, "You don't have the permission to view this problem.");
            }
            if ((problem == null || !problem.IsVisible && !this.HasOwnership) && string.IsNullOrEmpty(contestId))
            {
                return Result<Problem>(404, "Not Found");
            }
            
            IQueryable<TestCase> testCases = DB.TestCases
                .Where(x => x.ProblemId == problemId);

            if (type.HasValue)
            {
                testCases = testCases.Where(x => x.Type == type.Value);
            }

            testCases = testCases.OrderBy(x => x.Type);

            var ret = await testCases.Select(x => new TestCaseWithContent
            {
                Id = x.Id,
                ContestId = x.ContestId,
                InputBlobId = x.InputBlobId,
                InputSizeInByte = x.InputSizeInByte,
                OutputBlobId = x.OutputBlobId,
                OutputSizeInByte = x.OutputSizeInByte,
                ProblemId = x.ProblemId,
                Type = x.Type
            })
            .ToListAsync(token);

            if (showContent.HasValue && showContent.Value)
            {
                foreach (var x in ret)
                {
                    if (x.Type == TestCaseType.Sample || await IsAbleToAccessTestCaseContentAsync(x.Id, token))
                    {
                        x.Input = Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(x.InputBlobId, token)).Body);
                        x.Output = Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(x.OutputBlobId, token)).Body);
                    }
                }
            }
            
            return Result(ret);
        }

        [HttpGet("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}")]
        public async Task<IActionResult> GetTestCase(
            [FromServices] ManagementServiceClient MgmtSvc,
            string problemId, 
            Guid id,
            bool? showContent,
            CancellationToken token)
        {
            this.HasOwnership = await HasPermissionToProblemAsync(problemId, token);
            var problem = await DB.Problems.SingleOrDefaultAsync(x => x.Id == problemId, token);
            if (problem == null || !problem.IsVisible && !this.HasOwnership)
            {
                return Result<Problem>(404, "Not Found");
            }

            var ret = await DB.TestCases
                .Select(x => new TestCaseWithContent
                {
                    Id = x.Id,
                    ContestId = x.ContestId,
                    InputBlobId = x.InputBlobId,
                    InputSizeInByte = x.InputSizeInByte,
                    OutputBlobId = x.OutputBlobId,
                    OutputSizeInByte = x.OutputSizeInByte,
                    ProblemId = x.ProblemId,
                    Type = x.Type
                })
                .SingleOrDefaultAsync(x => x.ProblemId == problemId && x.Id == id, token);

            if (ret == null)
            {
                return Result<TestCaseWithContent>(404, "Not Found");
            }
            else if (!await HasPermissionToProblemAsync(problemId, token)
                || !await DB.TestCasePurchases.AnyAsync(x => x.TestCaseId == id && x.UserId == User.Current.Id, token))
            {
                return Result<TestCaseWithContent>(401, "No Permission");
            }
            else
            {
                if (showContent.HasValue && showContent.Value)
                {
                    if (await IsAbleToAccessTestCaseContentAsync(ret.Id, token))
                    {
                        ret.Input = Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(ret.InputBlobId, token)).Body);
                        ret.Output = Encoding.UTF8.GetString((await MgmtSvc.GetBlobAsync(ret.OutputBlobId, token)).Body);
                    }
                }

                return Result(ret);
            }
        }

        [HttpPut("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase")]
        public async Task<IActionResult> PutTestCase(
            string problemId,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(problemId, token))
            {
                return Result<Guid>(401, "No Permission");
            }
            else
            {
                var value = JsonConvert.DeserializeObject<TestCaseUpload>(RequestBody);
                var testCase = new TestCase
                {
                    ContestId = value.ContestId,
                    InputSizeInByte = value.Input.Length,
                    OutputSizeInByte = value.Output.Length,
                    ProblemId = problemId,
                    Type = value.Type
                };

                testCase.InputBlobId = await ManagementService.PutBlobAsync("input.txt", Encoding.UTF8.GetBytes(value.Input), token);
                testCase.OutputBlobId = await ManagementService.PutBlobAsync("output.txt", Encoding.UTF8.GetBytes(value.Output), token);

                DB.TestCases.Add(testCase);
                await DB.SaveChangesAsync(token);

                if (value.Type == TestCaseType.Sample)
                {
                    hub.Clients.All.InvokeAsync("ItemUpdated", "problem-sample-data", problemId);
                }

                return Result(testCase.Id);
            }
        }

        [HttpPut("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/zip")]
        public async Task<IActionResult> PutTestCaseZip(
            string problemId,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            var value = JsonConvert.DeserializeObject<TestCaseZipUpload>(RequestBody);

            value.Zip = value.Zip.Substring(value.Zip.IndexOf("base64,") + "base64,".Length);

            var path = Path.Combine(Path.GetTempPath(), "joyoi_" + Guid.NewGuid().ToString().Replace("-", "") + ".zip");
            System.IO.File.WriteAllBytes(path, Convert.FromBase64String(value.Zip));

            var count = 0;
            using (var zip = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var zipArchive = new ZipArchive(zip, ZipArchiveMode.Update))
            {
                var inputs = zipArchive.Entries.Where(x => x.FullName.EndsWith(".in"));

                foreach (var input in inputs)
                {
                    var output = zipArchive.Entries.SingleOrDefault(x => x.FullName == input.FullName.Substring(0, input.FullName.Length - 3) + ".out" || x.FullName == input.FullName.Substring(0, input.FullName.Length - 3) + ".ans");
                    if (output == null)
                    {
                        continue;
                    }

                    var testCase = new TestCase
                    {
                        ProblemId = problemId,
                        Type = value.Type
                    };

                    using (var stream = input.Open())
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        testCase.InputSizeInByte = (int)stream.Length;
                        testCase.InputBlobId = await ManagementService.PutBlobAsync("input.txt", bytes, token);
                    }

                    using (var stream = output.Open())
                    {
                        var bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        testCase.OutputSizeInByte = (int)stream.Length;
                        testCase.OutputBlobId = await ManagementService.PutBlobAsync("output.txt", bytes, token);
                    }

                    DB.TestCases.Add(testCase);
                    ++count;
                }
                await DB.SaveChangesAsync(token);
            }

            System.IO.File.Delete(path);

            if (value.Type == TestCaseType.Sample)
            {
                hub.Clients.All.InvokeAsync("ItemUpdated", "problem-sample-data", problemId);
            }

            return Result(200, count + " test cases uploaded.");
        }

        [HttpPost("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}")]
        [HttpPatch("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}")]
        public async Task<IActionResult> PatchTestCase(
            string problemId, 
            Guid id,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(problemId, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                var value = RequestBody;

                var testCase = await DB.TestCases.SingleOrDefaultAsync(x => x.Id == id, token);
                if (testCase == null)
                {
                    return Result(404, "Not Found");
                }

                var jsonToDictionary = JsonConvert.DeserializeObject<IDictionary<string, string>>(value);
                if (jsonToDictionary.ContainsKey("ContestId"))
                {
                    if (!string.IsNullOrWhiteSpace(jsonToDictionary["ContestId"]))
                    {
                        if (!await HasPermissionToContestAsync(jsonToDictionary["ContestId"], token))
                        {
                            return Result(401, "No Permission");
                        }
                        else
                        {
                            testCase.ContestId = jsonToDictionary["ContestId"];
                        }
                    }
                }
                if (jsonToDictionary.ContainsKey("Input"))
                {
                    testCase.InputBlobId = await ManagementService.PutBlobAsync("input.txt", Encoding.UTF8.GetBytes(jsonToDictionary["Input"]));
                    testCase.InputSizeInByte = jsonToDictionary["Input"].Length;
                }
                if (jsonToDictionary.ContainsKey("Output"))
                {
                    testCase.InputBlobId = await ManagementService.PutBlobAsync("output.txt", Encoding.UTF8.GetBytes(jsonToDictionary["Output"]));
                    testCase.InputSizeInByte = jsonToDictionary["Output"].Length;
                }
                if (jsonToDictionary.ContainsKey("Type"))
                {
                    testCase.Type = Enum.Parse<TestCaseType>(jsonToDictionary["Type"]);
                }

                await DB.SaveChangesAsync(token);

                hub.Clients.All.InvokeAsync("ItemUpdated", "problem-sample-data", problemId);

                return Result(200, "Succeeded");
            }
        }

        [HttpDelete("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}")]
        public async Task<IActionResult> DeleteTestCase(
            string problemId, 
            Guid id,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(problemId, token))
            {
                return Result(401, "No permission");
            }
            else
            {
                await DB.TestCases
                    .Where(x => x.Id == id)
                    .DeleteAsync(token);

                hub.Clients.All.InvokeAsync("ItemUpdated", "problem-sample-data", problemId);

                return Result(200, "Succeeded");
            }
        }
        #endregion

        #region Test Case Purchase
        [HttpPut("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}/purchase")]
        [HttpPost("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}/purchase")]
        [HttpPatch("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/testcase/{id:Guid}/purchase")]
        public async Task<IActionResult> PutTestCasePurchase(string problemId, Guid id, CancellationToken token)
        {
            // 判断是否已经购买
            if (await DB.TestCasePurchases.AnyAsync(x => x.TestCaseId == id && x.UserId == User.Current.Id, token))
            {
                return Result(400, "Already purchased");
            }
            else if (!await ProblemIsVisiableAsync(problemId, token))
            {
                return Result(401, "No permission");
            }
            else
            {
                // TODO: 扣除积分
                if (true == false)
                {
                    return Result(400, "Not enough point");
                }
                else
                {
                    DB.TestCasePurchases.Add(new TestCasePurchase
                    {
                        TestCaseId = id,
                        CreatedTime = DateTime.UtcNow,
                        UserId = User.Current.Id
                    });
                    await DB.SaveChangesAsync(token);
                    return Result(200, "Succeeded");
                }
            }
        }
        #endregion

        #region Resolutions
        [HttpGet("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/resolution")]
        public async Task<IActionResult> GetResolution(
            [FromServices] ExternalApi XApi,
            string problemId, 
            int? page,
            CancellationToken token)
        {
            return Json(await XApi.GetProblemResolutionsAsync(problemId, page.HasValue ? page.Value : 1, token));
        }
        #endregion

        #region Claims
        [HttpGet("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/all")]
        public async Task<IActionResult> GetClaims(string problemId, CancellationToken token)
        {
            var ret = await DB.UserClaims
                .Where(x => x.ClaimType == Constants.ProblemEditPermission)
                .Where(x => x.ClaimValue == problemId)
                .ToListAsync(token);
            return Result(ret);
        }

        [HttpPut("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim")]
        public async Task<IActionResult> PutClaims(string problemId, [FromBody] IdentityUserClaim<Guid> value, CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(problemId, token))
            {
                return Result(401, "No permission");
            }
            else if (await DB.UserClaims.AnyAsync(x => x.ClaimValue == problemId && x.ClaimType == Constants.ProblemEditPermission && x.UserId == value.UserId, token))
            {
                return Result(400, "Already exists");
            }
            else
            {
                DB.UserClaims.Add(new IdentityUserClaim<Guid>
                {
                    ClaimType = Constants.ProblemEditPermission,
                    UserId = value.UserId,
                    ClaimValue = problemId
                });
                await DB.SaveChangesAsync(token);
                return Result(200, "Succeeded");
            }
        }

        [HttpPut("{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/{userId:Guid}")]
        public async Task<IActionResult> DeleteClaim(Guid userId, string problemId, CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(problemId, token))
            {
                return Result(401, "No permission");
            }
            else if (!await DB.UserClaims.AnyAsync(x => x.ClaimValue == problemId && x.ClaimType == Constants.ProblemEditPermission && x.UserId == userId, token))
            {
                return Result(404, "Claim not found");
            }
            else if (userId == User.Current.Id)
            {
                return Result(400, "Cannot remove yourself");
            }
            else
            {
                await DB.UserClaims
                    .Where(x => x.ClaimValue == problemId && x.ClaimType == Constants.ProblemEditPermission && x.UserId == userId)
                    .DeleteAsync(token);

                return Result(200, "Delete succeeded");
            }
        }
        #endregion

        #region Private Functions
        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        private Task<bool> ProblemIsVisiableAsync(string problemId, CancellationToken token = default(CancellationToken))
            => DB.Problems.AnyAsync(x => x.Id == problemId && x.IsVisible, token);

        private async Task<bool> HasPermissionToProblemAsync(string problemId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == problemId, token));

        private async Task<bool> HasPermissionToContestAsync(string contestId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ContestEditPermission
                   && x.ClaimValue == contestId, token));

        private async Task<bool> IsAbleToAccessTestCaseContentAsync(Guid testCaseId, CancellationToken token)
            => await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles) || await DB.TestCasePurchases.AnyAsync(x => x.UserId == User.Current.Id && x.TestCaseId == testCaseId);

        private async Task CompileAsync(string id, string code, string language, CancellationToken token)
        {
            // 1. 上传code
            var codeBlobId = await ManagementService.PutBlobAsync("Main" + Constants.GetSourceExtension(language), Encoding.UTF8.GetBytes(code), token);

            // 2. 创建状态机
            var statemachineId = await ManagementService.PutStateMachineInstanceAsync(
                Constants.CompileOnlyStateMachine,
                Configuration["Host:Url"], new BlobInfo[]
                {
                        new BlobInfo(codeBlobId, "Main" + Constants.GetSourceExtension(language), JsonConvert.SerializeObject(new { Id = id, Type = "Validator" }))
                });

            // 3. 存储状态机ID
            DB.StateMachines.Add(new StateMachine
            {
                Id = statemachineId,
                CreatedTime = DateTime.UtcNow,
                Name = Constants.CompileOnlyStateMachine
            });

            await DB.SaveChangesAsync(token);
        }

        private async Task<bool> IsContestAttendee(string contestId, CancellationToken token)
        {
            if (!User.IsSignedIn())
                return false;

            if (string.IsNullOrEmpty(contestId))
                return false;

            return await DB.Attendees.AnyAsync(x => x.ContestId == contestId && x.UserId == User.Current.Id, token);
        }

        private async Task<bool> IsAttendeeActive(string contestId, CancellationToken token)
        {
            if (!await IsContestAttendee(contestId, token))
                return false;

            if (string.IsNullOrEmpty(contestId))
                return false;

            var attendee = await DB.Attendees.SingleAsync(x => x.UserId == User.Current.Id && x.ContestId == contestId, token);
            var contest = await DB.Contests.SingleAsync(x => x.Id == contestId, token);
            if (contest.Status == ContestStatus.Pending)
                return false;
            if (!attendee.IsVirtual && contest.Status == ContestStatus.Live)
                return true;
            else if (!contest.DisableVirtual && attendee.IsVirtual && attendee.RegisterTime.Add(contest.Duration) > DateTime.UtcNow)
                return true;
            else
                return false;
        }

        private async Task<bool> IsContestProblem(string problemId, string contestId, CancellationToken token)
        {
            if (string.IsNullOrEmpty(problemId) || string.IsNullOrEmpty(contestId))
                return false;

            return await DB.ContestProblems.AnyAsync(x => x.ContestId == contestId && x.ProblemId == problemId, token);
        }

        private async Task<bool> IsContestAttendeeAbleToAccessProblem(string problemId, string contestId, CancellationToken token)
        {
            return await IsContestProblem(problemId, contestId, token) && await IsAttendeeActive(contestId, token);
        }
        #endregion
    }
}
