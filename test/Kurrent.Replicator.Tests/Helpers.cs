// Copyright (c) Kurrent, Inc and/or licensed to Kurrent, Inc under one or more agreements.
// Kurrent, Inc licenses this file to you under the Kurrent License v1 (see LICENSE.md).

using EventStore.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Kurrent.Replicator.Tests {
    public static class Helpers {
        public static IEnumerable<Task<IWriteResult>> CreateRandomEvents(this EventStoreClient client) {
            var rnd = new Random();
            var events = new List<Tuple<string, EventData>> { };
            // Generate 100 random events over 3 different streams
            for (int i = 0; i < 100; i++) {
                var stream = $"stream-{rnd.Next(1, 4)}";
                    var eventData = new EventData(
                        Uuid.NewUuid(),
                        "random-event",
                        Encoding.UTF8.GetBytes($"Event {i}"),
                        Encoding.UTF8.GetBytes($"{{\"value\":{i}}}")
                        );
                events.Add(new Tuple<string, EventData>(stream, eventData));
                TestContext.Current.ObjectBag["EventCount"] = TestContext.Current.GetEventCount() + 1;
            }
            return events.GroupBy(x => x.Item1)
                .Select(g => client.AppendToStreamAsync(g.Key, StreamState.Any, g.Select(f => f.Item2)));
        }

        public static async Task DeleteSomeStreams(this EventStoreClient client, int streamsToDelete = 1) {
            var streamData = new Dictionary<string, int>(); // key: stream name, value: number of events
            //var streams = await client.ReadAllAsync(Direction.Forwards, Position.Start, resolveLinkTos: false).ToArrayAsync().ConfigureAwait(false);
            //foreach (var item in streams) {
            //    if (@event.Event.EventType.StartsWith("stream-") && streamsToDelete > 0) {
            //        var streamName = @event.Event.EventType;
            //        if (streamData.TryGetValue(streamName, out var state)) {
            //            streamData[streamName] = state + 1;
            //        }
            //        else {
            //            streamData[streamName] = 1;
            //        }
            //    }
            //}
            await client.ReadAllAsync(Direction.Forwards, Position.Start).ForEachAsync(async (@event) => {
                if (@event.Event.EventStreamId.StartsWith("stream-") && streamsToDelete > 0) {
                    var streamName = @event.Event.EventStreamId;
                    if (streamData.TryGetValue(streamName, out var state)) {
                        streamData[streamName] = state + 1;
                    }
                    else {
                        streamData[streamName] = 1;
                    }
                }
            }).ConfigureAwait(false);
            foreach (var stream in streamData.Keys.Take(streamsToDelete)) {
                await client.DeleteAsync(stream, StreamState.StreamExists);
                TestContext.Current.ObjectBag["EventCount"] = TestContext.Current.GetEventCount() - streamData[stream];
            }
        }

        public static void InitialiseEventCount(this TestContext context) {
            if (!context.ObjectBag.ContainsKey("EventCount")) {
                context.ObjectBag["EventCount"] = 0;
            }
        }

        public static int GetEventCount(this TestContext context) {
            if (context.ObjectBag.TryGetValue("EventCount", out var value) && value is int count) {
                return count;
            }
            context.ObjectBag["EventCount"] = 0;
            return 0;
        }
    }
}
