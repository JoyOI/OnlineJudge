using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using JoyOI.OnlineJudge.WebApi.Lib;

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