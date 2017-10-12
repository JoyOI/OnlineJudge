using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace JoyOI.OnlineJudge.WebApi.Hubs
{
    public class OnlineJudgeHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddAsync(Context.ConnectionId, groupName);
        }
    }
}
