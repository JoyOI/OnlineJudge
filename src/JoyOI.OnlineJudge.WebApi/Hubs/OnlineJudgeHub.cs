using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi.Hubs
{
    public class OnlineJudgeHub : Hub
    {
        private SmartUser<User,Guid> _user;

        public OnlineJudgeHub(SmartUser<User, Guid> user)
        {
            _user = user;
        }

        public async Task JoinGroup(string groupName)
        {
            if (groupName != "Masters")
                await Groups.AddAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            if (_user.IsSignedIn() && await _user.Manager.IsInAnyRolesAsync(_user.Current, Constants.MasterOrHigherRoles))
            {
                await Groups.AddAsync(Context.ConnectionId, "Masters");
            }
            await base.OnConnectedAsync();
        }
    }
}
