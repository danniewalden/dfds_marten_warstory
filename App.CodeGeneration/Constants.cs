using Marten;

namespace App;

public static class Constants
{
	public const string Connectionstring = "Host=localhost;Port=5432;Username=martenpresentation;Password=martenpresentation;Database=martenpresentation;";

	public static StoreOptions DefaultOptions
	{
		get
		{
			var options = new StoreOptions();
			options.Serializer(MartenSerializer.Instance);
			options.Connection(Connectionstring);
			options.Policies.AllDocumentsEnforceOptimisticConcurrency();
			options.Policies.ForAllDocuments(p =>
			{
				p.Metadata.LastModified.Enabled = true;
				p.Metadata.LastModifiedBy.Enabled = true;
				p.Metadata.CreatedAt.Enabled = true;
				p.Metadata.CausationId.Enabled = true;
				p.Metadata.CorrelationId.Enabled = true;
			});
			return options;
		}
	}
}
