// Copyright (c) Kurrent, Inc and/or licensed to Kurrent, Inc under one or more agreements.
// Kurrent, Inc licenses this file to you under the Kurrent License v1 (see LICENSE.md).

using EventStore.Client;
using Kurrent.Replicator.KurrentDb;
using Kurrent.Replicator.Prepare;
using Kurrent.Replicator.Shared.Logging;
using Kurrent.Replicator.Shared.Observe;
using Kurrent.Replicator.Sink;
using Kurrent.Replicator.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ubiquitous.Metrics;
using Kurrent.Replicator.Tests.Logging;

namespace Kurrent.Replicator.Tests;

[ClassDataSource<KurrentContainerFixture>]
public class SwitchoverTests {

    readonly KurrentContainerFixture Fixture;
    public SwitchoverTests(KurrentContainerFixture fixture) {
        Fixture = fixture;
        LogProvider.SetCurrentLogProvider(new SerilogLogProvider());
    }

    [Before(Test)]
    public async Task Start() {
        await Fixture.StartContainers();
        TestContext.Current.InitialiseEventCount();
    }

    [After(Test)]
    public async Task Stop() => await Fixture.StopContainers();

    [Test]
    public async Task ShouldSwitchOver() {
        //Arrange
        var client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        var client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        var eventsToAdd = new List<EventData>();
        for (var i = 0; i < 100; i++) {
            eventsToAdd.Add(new EventData(Uuid.NewUuid(), "event-type", Encoding.UTF8.GetBytes($"Event {i}"), null));
        }

        await client1.AppendToStreamAsync("stream", StreamState.Any, eventsToAdd);

        await Task.Delay(TimeSpan.FromSeconds(1)); // give it some time to write
        var events = await client1.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(100);


        var file = Path.GetTempFileName();
        var store = new FileCheckpointStore(file, 100);

        ReplicationMetrics.Configure(Metrics.Instance);

        await Replicator.Replicate(
            new GrpcEventReader(client1),
            new GrpcEventWriter(client2),
            new (),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, false, TimeSpan.FromSeconds(5), TimeSpan.Zero, false, ""),
            TestContext.Current?.CancellationToken ?? CancellationToken.None
        );

        var pos = await File.ReadAllTextAsync(file);
        Console.WriteLine($"Checkpoint position: {pos} before switch");
        await Assert.That(pos).IsNotEmpty();


        events = await client2.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(100);


        //Now switch over

        eventsToAdd = new List<EventData>();
        for (var i = 0; i < 100; i++) {
            await client2.AppendToStreamAsync("stream", StreamState.Any, new[] { new EventData(Uuid.NewUuid(), "event-type", Encoding.UTF8.GetBytes($"Event {i + 100}"), null) });
        }

        await Task.Delay(TimeSpan.FromSeconds(1)); // give it some time to write

        //reset clients
        client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        await Replicator.Replicate(
            new GrpcEventReader(client2),
            new GrpcEventWriter(client1),
            new (),
            new (null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, false, TimeSpan.FromSeconds(5), TimeSpan.Zero, true, "reader"),
            TestContext.Current?.CancellationToken ?? CancellationToken.None
        );

        var pos2 = await File.ReadAllTextAsync(file);
        Console.WriteLine($"Checkpoint position: {pos2} after switch");
        await Assert.That(pos2).IsNotEmpty();
        await Assert.That(pos2).IsNotEqualTo(pos);

        var eventsAfterSwitch = await client1.ReadAllAsync(Direction.Forwards, Position.Start)
            .Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(eventsAfterSwitch).HasCount(200);

    }


    [Test]
    public async Task ShouldSwitchOverAndRunContinuously() {
        //Arrange
        var client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        var client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        var eventsToAdd = new List<EventData>();
        for (var i = 0; i < 100; i++) {
            eventsToAdd.Add(new EventData(Uuid.NewUuid(), "event-type", Encoding.UTF8.GetBytes($"Event {i}"), null));
        }

        await client1.AppendToStreamAsync("stream", StreamState.Any, eventsToAdd);

        await Task.Delay(TimeSpan.FromSeconds(1)); // give it some time to write
        var events = await client1.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(100);


        var file = Path.GetTempFileName();
        var store = new FileCheckpointStore(file, 100);

        ReplicationMetrics.Configure(Metrics.Instance);

        await Replicator.Replicate(
            new GrpcEventReader(client1),
            new GrpcEventWriter(client2),
            new(),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, false, TimeSpan.FromSeconds(5), TimeSpan.Zero, false, ""),
            TestContext.Current?.CancellationToken ?? CancellationToken.None
        );

        var pos = await File.ReadAllTextAsync(file);
        Console.WriteLine($"Checkpoint position: {pos} before switch");
        await Assert.That(pos).IsNotEmpty();


        events = await client2.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(100);


        //Now switch over

        //reset clients
        client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        var cts = new CancellationTokenSource();
        Replicator.Replicate(
            new GrpcEventReader(client2),
            new GrpcEventWriter(client1),
            new(),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, true, TimeSpan.FromSeconds(5), TimeSpan.Zero, true, "reader"),
            cts.Token
        );

        eventsToAdd = new List<EventData>();
        for (var i = 0; i < 100; i++) {
            await client2.AppendToStreamAsync("stream", StreamState.Any, new[] { new EventData(Uuid.NewUuid(), "event-type", Encoding.UTF8.GetBytes($"Event {i + 100}"), null) });
        }

        await Task.Delay(TimeSpan.FromSeconds(1)); // give it some time to write

        int eventsCount = 0;
        client1.SubscribeToAllAsync(FromAll.Start, async (subscription, evt, cancellationToken) => {
            if (evt.Event.EventType.StartsWith('$')) return;
            // This is just to ensure the subscription is working
            eventsCount += 1;
            if(eventsCount == 200) {
                cts.Cancel();
            }
        }, false, cancellationToken: cts.Token);

        while(eventsCount != 200) {
        }

        await Assert.That(eventsCount).IsEqualTo(200);

    }

    [Test]
    public async Task ShouldSwitchOverAndRunContinuouslyWithDifferentStreams() {
        //Arrange
        var client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        var client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        await Task.WhenAll(client1.CreateRandomEvents());

        await Task.Delay(TimeSpan.FromSeconds(1)); // give it some time to write
        var events = await client1.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(TestContext.Current.GetEventCount());


        var file = Path.GetTempFileName();
        var store = new FileCheckpointStore(file, 100);

        await Replicator.Replicate(
            new GrpcEventReader(client1),
            new GrpcEventWriter(client2),
            new(),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, false, TimeSpan.FromSeconds(5), TimeSpan.Zero, false, ""),
            TestContext.Current?.CancellationToken ?? CancellationToken.None
        );


        events = await client2.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(TestContext.Current.GetEventCount());


        //Now switch over

        //reset clients
        client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        var cts = new CancellationTokenSource();
        Replicator.Replicate(
            new GrpcEventReader(client2),
            new GrpcEventWriter(client1),
            new(),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, true, TimeSpan.FromSeconds(5), TimeSpan.Zero, true, "reader"),
            cts.Token
        );

        await Task.Delay(TimeSpan.FromSeconds(2)); // give it some time to initialise


        await Task.WhenAll(client2.CreateRandomEvents());

        var eventsCount = 0;

        cts.Token.Register(async () => {
            await Assert.That(eventsCount).IsEqualTo(TestContext.Current.GetEventCount());
        });
        await client1.SubscribeToAllAsync(FromAll.Start, async (subscription, evt, cancellationToken) => {
            if (evt.Event.EventType.StartsWith('$')) return;
            eventsCount += 1;
            if (TestContext.Current.GetEventCount() == eventsCount) {
                cts.Cancel();
            }
        }, false, cancellationToken: cts.Token);
    }

    [Test]
    public async Task ShouldSwitchOverAndRunContinuouslyWithDifferentStreamsAfterDeletion() {
        //Arrange
        var client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        var client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        await Task.WhenAll(client1.CreateRandomEvents());

        await Task.Delay(TimeSpan.FromSeconds(1)); // give it some time to write
        var events = await client1.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(TestContext.Current.GetEventCount());


        var file = Path.GetTempFileName();
        var store = new FileCheckpointStore(file, 100);

        await Replicator.Replicate(
            new GrpcEventReader(client1),
            new GrpcEventWriter(client2),
            new(),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, false, TimeSpan.FromSeconds(5), TimeSpan.Zero, false, ""),
            TestContext.Current?.CancellationToken ?? CancellationToken.None
        );


        events = await client2.ReadAllAsync(Direction.Forwards, Position.Start).Where(evt => !evt.Event.EventType.StartsWith('$'))
            .ToArrayAsync(TestContext.Current!.CancellationToken);
        await Assert.That(events).HasCount(TestContext.Current.GetEventCount());


        //Now switch over


        //reset clients
        client1 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer1);
        client2 = Fixture.GetKurrentClient(Fixture._kurrentDbContainer2);

        var cts = new CancellationTokenSource();
        Replicator.Replicate(
            new GrpcEventReader(client2),
            new GrpcEventWriter(client1),
            new(),
            new(null, null),
            new NoCheckpointSeeder(),
            store,
            new(false, true, TimeSpan.FromSeconds(5), TimeSpan.Zero, true, "reader"),
            cts.Token
        );

        await Task.Delay(TimeSpan.FromSeconds(2)); // give it some time to initialise


        await client2.DeleteSomeStreams();


        var eventsCount = 0;

        cts.Token.Register(async () => {
            await Assert.That(eventsCount).IsEqualTo(TestContext.Current.GetEventCount());
            Console.WriteLine("Test completed successfully.");
        });
        await client1.SubscribeToAllAsync(FromAll.Start, async (subscription, evt, cancellationToken) => {
            if (evt.Event.EventType.StartsWith('$')) return;
            eventsCount += 1;
            if (TestContext.Current.GetEventCount() == eventsCount) {
                cts.Cancel();
            }
        }, false, cancellationToken: cts.Token);
    }
}

