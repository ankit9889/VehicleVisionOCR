using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace VehicleVisionOCR.Backend.Hubs
{
    public class ScannerHub : Hub
    {
        // Clients can connect to this hub. 
        // We don't need any client-to-server methods yet, as the server will broadcast to clients.
        
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
