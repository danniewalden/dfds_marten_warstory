namespace Domain.EventSourcing;

public class AggregateRoot
{
	private readonly List<object> _events = new();
	public IReadOnlyCollection<object> Events => _events.AsReadOnly();

	protected void Raise(object @event)
	{
		_events.Add(@event);
	}
}
