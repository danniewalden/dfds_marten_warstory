namespace Domain.DocumentStore;

public class OrderV2
{
	private OrderV2(string id, string name)
	{
		Id = id;
		Name = name;
	}

	public string Id { get; }
	public string Name { get; }
}