using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JoyOI.ManagementService.Model.Dtos;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    public static class StateMachineAwaiter
    {
        public static ConcurrentDictionary<Guid, TaskCompletionSource<StateMachineInstanceOutputDto>> Semaphores { get; set; } = new ConcurrentDictionary<Guid, TaskCompletionSource<StateMachineInstanceOutputDto>>();

        public static Task<StateMachineInstanceOutputDto> GetStateMachineResultAsync(Guid statemachineId, CancellationToken token)
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
            return taskCompletionSource.Task;
        }
    }
}
