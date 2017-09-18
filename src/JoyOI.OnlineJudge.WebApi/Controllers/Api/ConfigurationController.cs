using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ConfigurationController : BaseController
    {
        [HttpGet("{key:regex(^[[a-zA-Z0-9-_]]{{1,32}}$)}")]
        public async Task<ApiResult<Configuration>> Get(string key, CancellationToken token)
        {
            var configuration = await DB.Configurations.SingleOrDefaultAsync(x => x.Key == key, token);
            if (configuration == null)
            {
                return Result<Configuration>(404, "Configuration not found");
            }
            else
            {
                return Result(configuration);
            }
        }

        [HttpPut]
        public async Task<ApiResult> Put(CancellationToken token)
        {
            var configuration = PutEntity<Configuration>(RequestBody).Entity;
            if (await DB.Configurations.AnyAsync(x => x.Key == configuration.Key, token))
            {
                return Result(400, "The key already exists");
            }
            else
            {
                DB.Configurations.Add(configuration);
                await DB.SaveChangesAsync(token);
                return Result(200, "Configuration added successfully.");
            }
        }

        [HttpPost("{key:regex(^[[a-zA-Z0-9-_]]{{1,32}}$)}")]
        [HttpPatch("{key:regex(^[[a-zA-Z0-9-_]]{{1,32}}$)}")]
        public async Task<ApiResult> Patch(string key, CancellationToken token)
        {
            if (!IsRoot)
            {
                return Result(401, "No permission");
            }

            var configuration = await DB.Configurations.SingleOrDefaultAsync(x => x.Key == key, token);
            if (configuration == null)
            {
                return Result(404, "Configuration not found");
            }

            PatchEntity(configuration, RequestBody);
            await DB.SaveChangesAsync(token);

            return Result(200, "Configuration patched successfully.");
        }

        [HttpDelete("{key:regex(^[[a-zA-Z0-9-_]]{{1,32}}$)}")]
        public async Task<ApiResult> Delete(string key, CancellationToken token)
        {
            if (!IsRoot)
            {
                return Result(401, "No permission");
            }

            var configuration = await DB.Configurations.SingleOrDefaultAsync(x => x.Key == key, token);
            if (configuration == null)
            {
                return Result(404, "Configuration not found");
            }

            await DB.Configurations
                .Where(x => x.Key == key)
                .DeleteAsync(token);

            return Result(200, "Configuration deleted");
        }
    }
}
