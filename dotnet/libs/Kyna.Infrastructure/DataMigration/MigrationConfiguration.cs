using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

public class MigrationConfiguration(string source, MigrationMode mode, string[]? categories = null)
{
    public required string Source { get; init; } = source;
    public string[] Categories { get; init; } = categories ?? [];
    public MigrationMode Mode { get; init; } = mode;
    [JsonPropertyName("Source Deletion Mode")]
    public SourceDeletionMode SourceDeletionMode { get; init; } = SourceDeletionMode.None;
    [JsonPropertyName("Adjusted Price Mode")]
    public AdjustedPriceModes AdjustedPriceModes { get; init; } = AdjustedPriceModes.None;
    public int MaxParallelization { get; init; }
}

/// <summary>
/// References the mode in which data is migrated from the imports database to the financials database.
/// </summary>
public enum MigrationMode
{
    /// <summary>
    /// Migrate only the latest for each category/sub-category for the source
    /// </summary>
    Latest = 0,
    /// <summary>
    /// Migrate all identified records in chronological order
    /// </summary>
    Rolling
}

/// <summary>
/// Represents the rules for deletion of API transactions in the imports database.
/// </summary>
public enum SourceDeletionMode
{
    /// <summary>
    /// Delete NONE of the imported transactions.
    /// </summary>
    None = 0,
    /// <summary>
    /// Delete ALL imported transactions (after migration).
    /// </summary>
    All,
    /// <summary>
    /// Delete all imported transactions EXCEPT the latest.
    /// </summary>
    [Description("All Except Latest")]
    AllExceptLatest
}

/// <summary>
/// Represents rules for managing the EOD adjusted prices.
/// </summary>
[Flags]
public enum AdjustedPriceModes
{
    None = 0,
    /// <summary>
    /// Only price data for tickers with splits will be in the adjusted prices table.
    /// </summary>
    [Description("Only Splits")]
    OnlySplits = 1 << 0,
    /// <summary>
    /// All price data will be in the adjusted prices table.
    /// </summary>
    All = 1 << 1,
    /// <summary>
    /// Delete from the (non-adjusted) prices table any records that are in the adjusted prices table.
    /// </summary>
    [Description("Delete From Source")]
    DeleteFromSource = 1 << 2
}