using Kurrent.Replicator.Shared.Contracts;

namespace Kurrent.Replicator.Shared; 

public interface IEventWriter : IPositionReader {
    Task Start();
    Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken);
}