using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EventStore.Client;
using EventStore.ClientAPI;
using Testcontainers.EventStoreDb;

namespace Kurrent.Replicator.Tests.Fixtures;

public class KurrentContainerFixture {
    public EventStoreDbContainer _kurrentDbContainer1;
    public EventStoreDbContainer _kurrentDbContainer2;

    public async Task StartContainers() {
        _kurrentDbContainer1  = BuildV23Container(1);
        _kurrentDbContainer2  = BuildV23Container(2);

        await _kurrentDbContainer1.StartAsync();
        await _kurrentDbContainer2.StartAsync();
        await Task.Delay(TimeSpan.FromSeconds(2)); // give it some time to spin up
    }

    public async Task StopContainers() {
        await Task.WhenAll(_kurrentDbContainer1.StopAsync(), _kurrentDbContainer1.StopAsync());
        await _kurrentDbContainer1.DisposeAsync();
        await _kurrentDbContainer2.DisposeAsync();
    }

    public EventStoreClient GetKurrentClient(EventStoreDbContainer container) {
        var connectionString = container.GetConnectionString();
        var settings         = EventStoreClientSettings.Create(connectionString);
        settings.ConnectivitySettings.KeepAliveInterval = TimeSpan.FromSeconds(1000);

        return new(settings);
    }

    static EventStoreDbContainer BuildV23Container(int idx = 1) => new EventStoreDbBuilder()
        .WithImage("eventstore/eventstore:24.10")
        .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS", "None")
        .WithEnvironment("EVENTSTORE_START_STANDARD_PROJECTIONS", "false")
        .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", bool.TrueString)
        .WithPortBinding(2113, true)
        .WithName($"ev-{TestContext.Current.TestDetails.TestName}-{idx}")
        .Build();
   
}
