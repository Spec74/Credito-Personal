namespace Credito.Modern.Infrastructure;

public sealed class SqlDatabaseOptions
{
    public const string SectionName = "CreditoDatabase";

    public string ConnectionString { get; set; } = string.Empty;
}
