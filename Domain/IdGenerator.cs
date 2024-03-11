namespace Domain;

public static class IdGenerator
{
	public static string GenerateId() => GenerateRandomInt().ToString();

	private static int GenerateRandomInt()
	{
		var random = new Random();
		return random.Next(1, int.MaxValue);
	}
}
