using Domain;
using Domain.DocumentStore;
using Marten;
using Microsoft.Extensions.Hosting;
using Oakton;

namespace App;

public static class Program
{
	public static Task<int> Main(string[] args)
	{
		return CreateHostBuilder(args).RunOaktonCommands(args);
	}

	private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureServices((_, services) =>
	{
		services.AddMarten(_ =>
		{
			var options = Constants.DefaultOptions;
			options.Schema.For<Order>();
			return options;
		});
	}).ApplyOaktonExtensions();
}
