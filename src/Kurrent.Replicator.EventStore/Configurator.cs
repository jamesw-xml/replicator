namespace Kurrent.Replicator.EventStore;

public class TcpConfigurator(int pageSize) : IConfigurator {
    public string Protocol => "tcp";

    public IEventReader ConfigureReader(string connectionString, string? certificate, string? certificatePrivateKey)
        => new TcpEventReader(ConfigureEventStoreTcp(connectionString, true, certificate, certificatePrivateKey), pageSize);

    public IEventWriter ConfigureWriter(string connectionString, string? certificate, string? certificatePrivateKey)
        => new TcpEventWriter(ConfigureEventStoreTcp(connectionString, false, certificate, certificatePrivateKey));

    IEventStoreConnection ConfigureEventStoreTcp(string connectionString, bool follower, string? certificate, string? certificatePrivateKey) {
        var builder = ConnectionSettings.Create()
            .UseCustomLogger(new TcpClientLogger())
            .KeepReconnecting()
            .KeepRetrying();

        if (follower) {
            builder = builder.PreferFollowerNode();
        }

        if (Ensure.NotNullOrEmpty(certificate, out var cert) && Ensure.NotNullOrEmpty(certificatePrivateKey, out var privateKey)) {
            var brandedCertificate = CertManager.GetCertificate(cert, privateKey);
            var clientHandler = new HttpClientHandler();
            clientHandler.ClientCertificates.Add(brandedCertificate);
            clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            builder = builder.UseCustomHttpMessageHandler(clientHandler);
        }

        var connection = EventStoreConnection.Create(connectionString, builder);

        return connection;
    }
}
