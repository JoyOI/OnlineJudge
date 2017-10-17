using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoyOI.OnlineJudge.WebApi.Lib;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ForumController : BaseController
    {
        [HttpGet("Summary")]
        public async Task<IActionResult> Get(
            [FromServices] ExternalApi XApi,
            CancellationToken token)
        {
            var ret = await XApi.GetForumSummaryAsync(token);
            return Result(ret);
        }
    }
}
