using Microsoft.AspNetCore.SignalR;

namespace OmegaStreamWebAPI.Hubs
{
    public class BaseHub: Hub
    {
        protected virtual async Task SendErrorMessage(string connectionId, string message)
        {
            await Clients.Clients(connectionId).SendAsync("Error", message);
        }
    }
}
