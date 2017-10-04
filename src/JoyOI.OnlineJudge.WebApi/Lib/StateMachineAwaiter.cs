using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using JoyOI.ManagementService.Model.Dtos;
using JoyOI.ManagementService.SDK;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    public class StateMachineAwaiter
    {
        private IConfiguration _configuration;

        private ManagementServiceClient _managementServiceClient;

        public StateMachineAwaiter(IConfiguration configuration, ManagementServiceClient managementServiceClient)
        {
            _configuration = configuration;
            _managementServiceClient = managementServiceClient;
        }

        public ConcurrentDictionary<Guid, TaskCompletionSource<StateMachineInstanceOutputDto>> Semaphores { get; set; } = new ConcurrentDictionary<Guid, TaskCompletionSource<StateMachineInstanceOutputDto>>();

        public async Task<StateMachineInstanceOutputDto> GetStateMachineResultAsync(Guid statemachineId, CancellationToken token)
        {
            if (_configuration["ManagementService:Mode"] == "Polling")
            {
                var retryCount = 30;
                while (--retryCount >= 0)
                {
                    await Task.Delay(1000);
                    var result = await _managementServiceClient.GetStateMachineInstanceAsync(statemachineId, token);
                    if (result.Status != ManagementService.Model.Enums.StateMachineStatus.Running)
                    {
                        return result;
                    }
                }
                throw new Exception("Polling State Machine Failed, ID=" + statemachineId);
            }
            else if (_configuration["ManagementService:Mode"] == "Callback")
            {
                var taskCompletionSource = new TaskCompletionSource<StateMachineInstanceOutputDto>();
                token.Register(() =>
                {
                    TaskCompletionSource<StateMachineInstanceOutputDto> semaphore;
                    if (Semaphores.TryRemove(statemachineId, out semaphore))
                    {
                        semaphore.TrySetCanceled();
                    }
                });
                Semaphores.AddOrUpdate(statemachineId, taskCompletionSource, (a, b) => taskCompletionSource);
                return await taskCompletionSource.Task;
            }
            else
            {
                throw new InvalidOperationException(_configuration["ManagementService:Mode"] + " is invalid");
            }
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StateMachineAwaiterExtensions
    {
        public static IServiceCollection AddStateMachineAwaiter(this IServiceCollection self)
        {
            return self.AddSingleton<JoyOI.OnlineJudge.WebApi.Lib.StateMachineAwaiter>();
        }
    }
}