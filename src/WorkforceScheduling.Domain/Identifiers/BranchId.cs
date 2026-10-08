namespace WorkforceScheduling.Domain.Identifiers;

/// <summary>A stable branch integration key; display names are never identifiers.</summary>
public sealed record BranchId
{
    private BranchId(string value) => Value = value;

    /// <summary>Gets the unchanged stable identifier value.</summary>
    public string Value { get; }

    /// <summary>Creates an identifier without normalizing or guessing its format.</summary>
    /// <param name="value">The authoritative branch key.</param>
    /// <returns>The validated stable branch identifier.</returns>
    public static BranchId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A branch identifier cannot contain surrounding whitespace.", nameof(value));
        }

        return new BranchId(value);
    }

    public override string ToString() => Value;
}
