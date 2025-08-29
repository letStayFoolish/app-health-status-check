using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DeepCheck.Hubs
{
    // Simple marker hub - server pushes heartbeats to connected clients.
    public class UptimeHub : Hub
    {
        // Intentionally empty - heartbeats are pushed from a hosted service via IHubContext<UptimeHub>.

    }
}
