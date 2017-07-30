using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using JoyOI.ManagementService.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ProblemController : BaseController
    {
        #region Problem
        [HttpGet("all")]
        [HttpGet("all/page/{page:int?}")]
        public async Task<ApiResult<PagedResult<IEnumerable<Problem>>>> Get(string title, int? difficulty, string tag, int? page, CancellationToken token)
        {
            IQueryable<Problem> ret = DB.Problems;

            if (User.Current == null || !await User.Manager.IsInAnyRolesAsync(User.Current, "Root, Master"))
            {
                var editableProblemIds = await DB.UserClaims
                    .Where(x => x.UserId == User.Current.Id && x.ClaimType == Constants.ProblemEditPermission)
                    .Select(x => x.ClaimValue)
                    .ToListAsync(token);

                ret = ret.Where(x => x.IsVisiable || editableProblemIds.Contains(x.Id));
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                ret = ret.Where(x => x.Title.Contains(title) || title.Contains(x.Title));
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
                    ret = ret.Where(ContainsWhere<Problem>(x => x.Tags, tags));
                }
            }

            if (!page.HasValue)
            {
                page = 1;
            }

            return await Paged(ret, page.Value, 100, token);
        }
        
        [HttpGet("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult<Problem>> Get(string id, CancellationToken token)
        {
            var ret = await DB.Problems.SingleOrDefaultAsync(x => x.Id == id, token);
            if (ret == null)
            {
                return Result<Problem>(404, "Not Found");
            }
            else
            {
                return Result(ret);
            }
        }

        [HttpPost("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        [HttpPatch("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Patch(string id, [FromBody]string value, CancellationToken token)
        {
            if (User.Current == null 
                || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles) 
                && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id 
                    && x.ClaimType == Constants.ProblemEditPermission
                    && x.ClaimValue == id, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                var problem = await DB.Problems.SingleOrDefaultAsync(x => x.Id == id, token);
                PatchEntity(value, problem);
                await DB.SaveChangesAsync(token);
                return Result(200, "Patch Succeeded");
            }
        }
        
        [HttpPut("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Put(string id, [FromBody]Problem value, CancellationToken token)
        {
            if (await DB.Problems.AnyAsync(x => x.Id == id, token))
            {
                return Result(403, "The problem id is already exists.");
            }

            // 处理比较器
            if (value.ValidatorBlobId.HasValue)
            {
                // 检查使用的Blob是否合法
                if (!await DB.SharedValidators.AnyAsync(x => x.Id == value.ValidatorBlobId.Value, token) && await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles))
                {
                    return Result(401, "You don't have the permission to the specified validator, id=" + value.ValidatorBlobId.Value);
                }
            }
            else if (string.IsNullOrWhiteSpace(value.ValidatorCode) && string.IsNullOrEmpty(value.ValidatorLanguage))
            {
                // 如果没有指定Validator，则自动使用默认Validator
                var defaultValidator = await DB.SharedValidators.FirstOrDefaultAsync(x => x.IsDefault, token);

                // 如果系统没有默认比较器，则触发Incident
                if (defaultValidator == null)
                {
                    await IcM.TriggerIncidentAsync(2, "默认比较器未设定", $"用户 { User.Current.UserName } 创建题目时未设置比较器，系统在尝试为其设置默认比较器时，没有找到默认比较器。");
                    return Result(500, "Default Validator Not Found");
                }
                value.ValidatorBlobId = defaultValidator.Id;
            }
            else
            {
                // 如果用户上传了比较器代码
                if (Constants.CompileNeededLanguages.Contains(value.ValidatorLanguage))
                {
                    // 编译自定义比较器并缓存编译后的blob
                    await CompileAsync(id, value.ValidatorCode, value.ValidatorLanguage, token);
                }
                else
                {
                    // 直接缓存比较器脚本的blob
                    value.ValidatorBlobId = await ManagementService.PutBlobAsync(id + ".validator", Encoding.UTF8.GetBytes(value.ValidatorCode), token);
                }
            }

            DB.Problems.Add(value);
            await DB.SaveChangesAsync(token);
            return Result(200, "Put Succeeded");
        }
        
        [HttpDelete("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Delete(string id, CancellationToken token)
        {
            if (User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == id, token))
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

        #region TestCase
        [HttpGet("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/all")]
        public async Task<ApiResult<List<TestCase>>> GetTestCase(string problemid, TestCaseType? type, CancellationToken token)
        {
            IQueryable<TestCase> ret = DB.TestCases
                .Where(x => x.ProblemId == problemid);

            if (type.HasValue)
            {
                ret = ret.Where(x => x.Type == type.Value);
            }

            return Result(await ret.ToListAsync(token));
        }

        [HttpGet("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/{id:Guid}")]
        public async Task<ApiResult<TestCase>> GetTestCase(string problemId, Guid id, CancellationToken token)
        {
            var ret = await DB.TestCases.SingleOrDefaultAsync(x => x.ProblemId == problemId && x.Id == id, token);
            if (ret == null)
            {
                return Result<TestCase>(404, "Not Found");
            }
            else if (!await HasPermissionToProblemAsync(problemId, token)
                || !await DB.TestCaseBuyLogs.AnyAsync(x => x.TestCaseId == id && x.UserId == User.Current.Id, token))
            {
                return Result<TestCase>(401, "No Permission");
            }
            else
            {
                return Result(ret);
            }
        }

        [HttpPut("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase")]
        public async Task<ApiResult<Guid>> PutTestCase(string problemId, [FromBody] TestCaseUpload value, CancellationToken token)
        {
            if (await HasPermissionToProblemAsync(problemId, token))
            {
                return Result<Guid>(401, "No Permission");
            }
            else
            {
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

                return Result(testCase.Id);
            }
        }

        [HttpPost("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/{id:Guid}")]
        [HttpPatch("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/{id:Guid}")]
        public async Task<ApiResult> PatchTestCase(string problemId, Guid id, [FromBody] string value, CancellationToken token)
        {
            if (!await HasPermissionToProblemAsync(problemId, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
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
                return Result(200, "Succeeded");
            }
        }

        [HttpDelete("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/{id:Guid}")]
        public async Task<ApiResult> DeleteTestCase(string problemId, Guid id, CancellationToken token)
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

                return Result(200, "Succeeded");
            }
        }
        #endregion

        private async Task<bool> HasPermissionToProblemAsync(string problemId, CancellationToken token = default(CancellationToken))
            => User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == problemId);

        private async Task<bool> HasPermissionToContestAsync(string contestId, CancellationToken token = default(CancellationToken))
            => User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ContestEditPermission
                   && x.ClaimValue == contestId);

        private async Task CompileAsync(string id, string code, string language, CancellationToken token)
        {
            // 1. 上传code
            var codeBlobId = await ManagementService.PutBlobAsync("Main" + Constants.GetExtension(language), Encoding.UTF8.GetBytes(code), token);

            // 2. 创建状态机
            var statemachineId = await ManagementService.PutStateMachineInstanceAsync(
                Constants.CompileOnlyStateMachine,
                Configuration["Host:Url"], new BlobInfo[]
                {
                        new BlobInfo(codeBlobId, "Main" + Constants.GetExtension(language), JsonConvert.SerializeObject(new { Id = id, Type = "Validator" }))
                });

            // 3. 存储状态机ID
            DB.StateMachines.Add(new StateMachine
            {
                Id = statemachineId,
                CreatedTime = DateTime.Now,
                Name = Constants.CompileOnlyStateMachine
            });

            await DB.SaveChangesAsync(token);
        }
    }
}
