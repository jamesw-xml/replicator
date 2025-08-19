using Kurrent.Replicator.Shared.Contracts;

namespace Kurrent.Replicator.Shared; 

public interface IEventReader : IPositionReader {
    string Protocol { get; }
        
    Task ReadEvents(LogPosition fromLogPosition, Func<BaseOriginalEvent, ValueTask> next, CancellationToken cancellationToken);
    ValueTask<bool> Filter(BaseOriginalEvent originalEvent);
}