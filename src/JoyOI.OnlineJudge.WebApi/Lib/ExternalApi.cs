using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    public class ExternalApi
    {
        private IConfiguration _config;

        public ExternalApi(IConfiguration config)
        {
            _config = config;
        }

        public async Task<object> GetForumSummaryAsync(CancellationToken token)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(_config["JoyOI:ForumUrl"]) })
            {
                var response = await client.GetAsync("/summary", token);
                var json = JsonConvert.DeserializeObject<object>(await response.Content.ReadAsStringAsync());
                return json;
            }
        }

        public async Task<ApiResult<PagedResult<IEnumerable<object>>>> GetProblemResolutionsAsync(string problemId, int page, CancellationToken token)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(_config["JoyOI:BlogUrl"]) })
            {
                var response = await client.GetAsync("/api/resolution/" + problemId, token);
                var json = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                return new ApiResult<PagedResult<IEnumerable<object>>>
                {
                    code = 200,
                    data = new PagedResult<IEnumerable<object>>
                    {
                        count = json.pageCount,
                        size = json.pageSize,
                        current = page,
                        result = json.data,
                        total = json.total
                    }
                };
            }
        }

        public async Task<string> GetUserBlogDomainAsync(string username, CancellationToken token)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(_config["JoyOI:BlogUrl"]) })
            {
                var response = await client.GetAsync("/api/blogdomain/" + username, token);
                var text = await response.Content.ReadAsStringAsync();
                return text;
            }
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ExternalApiExtensions
    {
        public static IServiceCollection AddExternalApi(this IServiceCollection self)
        {
            return self.AddSingleton<ExternalApi>();
        }
    }
}