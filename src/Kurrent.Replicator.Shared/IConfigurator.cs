namespace Kurrent.Replicator.Shared; 

public interface IConfigurator {
    string            Protocol { get; }
    IEventReader      ConfigureReader(string connectionString, string? certificate, string? certificatePrivateKey);
    IEventWriter      ConfigureWriter(string connectionString, string? certificate, string? certificatePrivateKey);
}