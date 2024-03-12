using Marten;
using Marten.Services;
using Weasel.Core;

namespace App;

public static class MartenSerializer
{
	public static ISerializer Instance => new JsonNetSerializer
	{
		EnumStorage = EnumStorage.AsString,
		Casing = Casing.CamelCase,
		NonPublicMembersStorage = NonPublicMembersStorage.NonPublicConstructor,
		CollectionStorage = CollectionStorage.AsArray,
	};
	
	// public static ISerializer Instance => new SystemTextJsonSerializer
	// {
	// 	EnumStorage = EnumStorage.AsString,
	// 	Casing = Casing.CamelCase,
	// };
}
