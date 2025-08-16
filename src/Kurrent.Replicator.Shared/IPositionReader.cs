// Copyright (c) Kurrent, Inc and/or licensed to Kurrent, Inc under one or more agreements.
// Kurrent, Inc licenses this file to you under the Kurrent License v1 (see LICENSE.md).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kurrent.Replicator.Shared;

public interface IPositionReader {
    Task<long?> GetLastPosition(CancellationToken cancellationToken);
    public Task<LogPosition> GetLastFullPositionBasedOnPrevious(LogPosition position, CancellationToken cancellationToken);
}
