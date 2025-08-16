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
