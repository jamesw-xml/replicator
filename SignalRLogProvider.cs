public class SignalRLogProvider : ILogProvider
{
    private readonly IHubContext<LogHub> _hubContext;

    public SignalRLogProvider(IHubContext<LogHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void Log(string message)
    {
        _hubContext.Clients.All.SendAsync("ReceiveLog", message);
    }
}
