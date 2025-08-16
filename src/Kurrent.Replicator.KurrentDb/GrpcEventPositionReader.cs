// Copyright (c) Kurrent, Inc and/or licensed to Kurrent, Inc under one or more agreements.
// Kurrent, Inc licenses this file to you under the Kurrent License v1 (see LICENSE.md).

using Kurrent.Replicator.Shared.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kurrent.Replicator.KurrentDb; 
public class GrpcEventPositionReader : IPositionReader {
    protected readonly EventStoreClient _original;
    protected readonly EventStoreClient _new;

    public GrpcEventPositionReader(EventStoreClient client) {
        _original = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<long?> GetLastPosition(CancellationToken cancellationToken) {
        var events = await _original
            .ReadAllAsync(Direction.Backwards, Position.End, 1, cancellationToken: cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return (long?)events[0].OriginalPosition?.CommitPosition;
    }

    public async Task<LogPosition> GetLastFullPositionBasedOnPrevious(LogPosition position, CancellationToken cancellationToken) {
        var events = await _original
            .ReadAllAsync(Direction.Forwards, Position.Start, cancellationToken: cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var newPosition = events.FirstOrDefault(evt => {
            var metaDataAsJson = Encoding.UTF8.GetString(evt.Event.Metadata.ToArray());
            var metaData = JsonConvert.DeserializeObject<JObject>(metaDataAsJson,
                new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
            if(metaData != null && metaData.TryGetValue(EventMetadata.PositionPropertyName, out var positionValue) && ulong.TryParse(positionValue.ToString(), out var parsedValue))
            {
                if(parsedValue == position.EventPosition) {
                    return true;
                }
            }
            return false;
        });
        return new LogPosition(
            newPosition!.OriginalEventNumber.ToInt64(),
            newPosition.OriginalPosition!.Value.CommitPosition
        );
    }
}
