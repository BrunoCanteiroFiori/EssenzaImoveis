namespace Backend.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
