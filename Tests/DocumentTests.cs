using System.Diagnostics;
using App;
using Domain.DocumentStore;
using Marten;
using Marten.Exceptions;
using Weasel.Core.Migrations;
using Xunit.Abstractions;

namespace MartenPresentation.Tests;

public class DocumentTests(ITestOutputHelper output)
{
	/// <summary>
	/// This is an example of the simplest setup for a DocumentStore.
	/// 1 line of code is all that is required to get a working DocumentStore in Marten.
	///  Default is setup to automatically apply changes needed to the database. (Not recommended for production)
	/// </summary>
	[Fact]
	public async Task Simplest_Setup()
	{
		const string orderId = "1";

		// the following line is all that is _required_ to get a working DocumentStore
		await using var store = DocumentStore.For(_ => { _.Connection(Constants.Connectionstring); });

		// create 2 sessions (unit of works) to ensure is stored and fetched correctly without caching
		await using var insertingSession = store.LightweightSession();
		await using var fetchingSession = store.LightweightSession();

		// add an order to the database
		var order = new OrderRecord(orderId, "test");
		insertingSession.Store(order);
		await insertingSession.SaveChangesAsync();

		// retrieve the order from the database and assert it is the same as the one we added
		var fetchedOrder = await fetchingSession.LoadAsync<OrderRecord>(orderId);
		fetchedOrder.Should().BeEquivalentTo(order);
	}

	[Fact]
	public void Pre_Generate_Source_Code_For_Better_Runtime_Performance()
	{
		var startInfo = new ProcessStartInfo
		{
			WorkingDirectory = Path.Combine(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent?.Parent?.Parent?.Parent?.FullName ?? string.Empty, "App.CodeGeneration"),
			FileName = "dotnet",
			Arguments = "run -- codegen write",
			RedirectStandardOutput = true, // To capture the output
			RedirectStandardError = true, // To capture any errors
			UseShellExecute = false, // Required for redirections
			CreateNoWindow = true // Prevents the window from showing up
		};

		using var process = new Process();
		process.StartInfo = startInfo;
		process.Start();

		// To read the output to ensure the command is executed successfully
		var result = process.StandardOutput.ReadToEnd();
		var error = process.StandardError.ReadToEnd();

		process.WaitForExit(); // Waits for the process to finish

		output.WriteLine(result);
		if (!string.IsNullOrEmpty(error))
		{
			output.WriteLine("Error: " + error);
		}

		process.ExitCode.Should().Be(0);
	}

	[Fact]
	public async Task Add_new_order()
	{
		var order = Order.Create("test");

		var store = await CreateStoreAndInitializeDatabase();
		var session = store.LightweightSession();

		session.Store(order);
		await session.SaveChangesAsync();
	}

	[Fact]
	public async Task Get_Order()
	{
		var order = Order.Create("test");

		await using var store = await CreateStoreAndInitializeDatabase();
		await using var session = store.LightweightSession();

		session.Store(order);
		await session.SaveChangesAsync();

		await using var session2 = store.LightweightSession();
		session2.Load<Order>(order.Id).Should().BeEquivalentTo(order);
	}

	[Fact]
	public async Task Generate_new_migration_file()
	{
		await CreateStoreAndInitializeDatabase();
		var storeOptions = Constants.DefaultOptions;
		storeOptions.Schema.For<Order>().Duplicate(x => x.Name);
		await using var documentStore = new DocumentStore(storeOptions);

		// Generate SQL for schema updates (doesn't apply them)
		await documentStore.Storage.Database.WriteMigrationFileAsync("migration.sql");
	}

	[Fact]
	public async Task Automatically_Apply_all_changes_to_database()
	{
		var storeOptions = new StoreOptions();
		storeOptions.Connection(Constants.Connectionstring);

		storeOptions.Schema.For<Order>().Duplicate(x => x.Name);

		await using var documentStore = new DocumentStore(storeOptions);

		await documentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
	}

	[Fact]
	public async Task Add_new_required_property_on_order_without_default_value()
	{
		var store = await CreateStoreAndInitializeDatabase();

		await using var session = store.LightweightSession();
		session.QueueSqlCommand("UPDATE public.mt_doc_order SET data = jsonb_set(data, '{Description}', '\"Default Description\"') WHERE data->>'Description' IS NULL;");
		await session.SaveChangesAsync();
	}

	[Fact]
	public async Task Adding_alias_to_Renamed_Class_Used_as_a_document()
	{
		var oldOptions = Constants.DefaultOptions;
		oldOptions.Schema.For<Order>();

		await using var documentStore = new DocumentStore(oldOptions);
		await documentStore.Advanced.Clean.DeleteAllDocumentsAsync();
		await documentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

		var oldOrder = Order.Create("test");
		await using var session = documentStore.LightweightSession();
		session.Store(oldOrder);
		await session.SaveChangesAsync();

		var newOptions = Constants.DefaultOptions;
		newOptions.Schema.For<OrderV2>().DocumentAlias("order");

		await using var newStore = new DocumentStore(newOptions);
		await using var session2 = newStore.LightweightSession();
		var newOrder = await session2.LoadAsync<OrderV2>(oldOrder.Id) ?? throw new Exception("Order not found");

		newOrder.Id.Should().Be(oldOrder.Id);
		newOrder.Name.Should().Be(oldOrder.Name);
	}

	[Fact]
	public async Task Setting_Metadata_for_document()
	{
		var store = await CreateStoreAndInitializeDatabase();

		var orders = Enumerable.Range(0, 10).Select(i => Order.Create($"test-{i}"));

		var i = 0;
		foreach (var order in orders)
		{
			i++;
			var session = store.LightweightSession();

			session.CorrelationId = $"my-correlation-Id-{i}";
			session.CausationId = $"my-causation-Id-{i}";
			session.LastModifiedBy = $"my-awesome-marten-presentation-{i}";
			session.Store(order);
			await session.SaveChangesAsync();
		}
	}

	[Fact]
	public async Task Optimistic_Concurrency_checks()
	{
		var store = await CreateStoreAndInitializeDatabase();

		await using var arrangeSession = store.LightweightSession();

		var order = Order.Create("test");
		arrangeSession.Store(order);
		await arrangeSession.SaveChangesAsync();

		await using var concurrentSession1 = store.LightweightSession();
		await using var concurrentSession2 = store.LightweightSession();

		var order1 = await concurrentSession1.LoadAsync<Order>(order.Id) ?? throw new Exception("Order not found");
		var order2 = await concurrentSession2.LoadAsync<Order>(order.Id) ?? throw new Exception("Order not found");

		order1.ChangeName("newTest");
		concurrentSession1.Store(order1);
		await concurrentSession1.SaveChangesAsync();

		order2.ChangeName("newTest");
		concurrentSession2.Store(order1);
		var act = () => concurrentSession2.SaveChangesAsync();

		await act.Should().ThrowAsync<ConcurrencyException>();
	}

	private static async Task<DocumentStore> CreateStoreAndInitializeDatabase(StoreOptions? options = null)
	{
		var store = new DocumentStore(options ?? Constants.DefaultOptions);

		await store.Advanced.Clean.CompletelyRemoveAsync(typeof(Order));
		await store.Advanced.Clean.CompletelyRemoveAsync(typeof(OrderRecord));
		await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
		return store;
	}
}
