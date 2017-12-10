using JoyOI.OnlineJudge.ContestExecutor;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ContestExecutorServiceCollectionExtensions
    {
        public static IServiceCollection AddContestExecutorFactory(this IServiceCollection self)
        {
            return self.AddScoped<ContestExecutorFactory>();
        }
    }
}
