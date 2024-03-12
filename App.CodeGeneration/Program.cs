using Domain.DocumentStore;
using Domain.Policies;
using Domain.ViewModel;
using Marten;
using Marten.Events.Projections;
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

			options.Projections.Add(new StudentViewmodelProjection(), ProjectionLifecycle.Inline);
			options.Projections.Add(new CourseViewModelProjection(), ProjectionLifecycle.Inline);

			// add a policy for handling overbooked courses (async means it's applied after the transaction is committed)
			options.Projections.Add(new OverBookedCoursePolicy(), ProjectionLifecycle.Async);

			return options;
		});
	}).ApplyOaktonExtensions();
}
