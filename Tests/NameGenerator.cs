namespace MartenPresentation.Tests;

public class NameGenerator
{
	private static List<string> FirstNames = ["John", "Jane", "Robert", "Emily", "Michael", "Sarah", "William", "Jessica"];
	private static List<string> LastNames = ["Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia"];
	private static Random random = new();

	public static string GenerateName()
	{
		var firstName = FirstNames[random.Next(FirstNames.Count)];
		var lastName = LastNames[random.Next(LastNames.Count)];
		return $"{firstName} {lastName}";
	}
}
