namespace Domain.DocumentStore;

public class Order
{
	private Order(string id, string name)
	{
		Id = id;
		Name = name;
	}

	public void ChangeName(string newName)
	{
		Name = newName;
	}

	// private Order(string id, string name, string description)
	// {
	// 	Id = id;
	// 	Name = name;
	// 	Description = description;
	// }

	public string Id { get; }
	public string Name { get; private set; }
	// public string Description { get; }

	public static Order Create(string name)
	{
		return new Order(IdGenerator.GenerateId(), name);
	}
}