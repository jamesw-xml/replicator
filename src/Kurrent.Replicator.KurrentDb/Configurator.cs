namespace Kurrent.Replicator.KurrentDb; 

public class GrpcConfigurator : IConfigurator {
    public string Protocol => "grpc";

    public IEventReader ConfigureReader(string connectionString, string? certificate, string? certificatePrivateKey)
        => new GrpcEventReader(ConfigureEventStoreGrpc(connectionString, true, certificate, certificatePrivateKey));

    public IEventWriter ConfigureWriter(string connectionString, string? certificate, string? certificatePrivateKey)
        => new GrpcEventWriter(ConfigureEventStoreGrpc(connectionString, false, certificate, certificatePrivateKey));

    static EventStoreClient ConfigureEventStoreGrpc(string connectionString, bool follower, string? certificate, string? certificatePrivateKey) {
        var settings = EventStoreClientSettings.Create(connectionString);

        if (follower) {
            settings.ConnectivitySettings.NodePreference = NodePreference.Follower;
        }

        if(Ensure.NotNullOrEmpty(certificate, out var cert) && Ensure.NotNullOrEmpty(certificatePrivateKey, out var privateKey)) {
            var brandedCertificate = CertManager.GetCertificate(cert, privateKey);
            settings.CreateHttpMessageHandler = () => {
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(brandedCertificate);
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                return handler;
            };
        }
        return new(settings);
    }
}