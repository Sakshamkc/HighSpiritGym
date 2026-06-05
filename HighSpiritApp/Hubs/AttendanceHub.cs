using Microsoft.AspNetCore.SignalR;

namespace HighSpiritApp.Hubs
{
    public class AttendanceHub : Hub
    {
        public async Task JoinDisplayGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Display");
        }
    }
}
