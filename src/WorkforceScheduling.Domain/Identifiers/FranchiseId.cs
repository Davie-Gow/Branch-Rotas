namespace WorkforceScheduling.Domain.Identifiers;

/// <summary>A stable franchise integration key; display names are never identifiers.</summary>
public sealed record FranchiseId
{
    private FranchiseId(string value) => Value = value;

    /// <summary>Gets the unchanged stable identifier value.</summary>
    public string Value { get; }

    /// <summary>Creates an identifier without normalizing or guessing its format.</summary>
    /// <param name="value">The authoritative franchise key.</param>
    /// <returns>The validated stable franchise identifier.</returns>
    public static FranchiseId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A franchise identifier cannot contain surrounding whitespace.", nameof(value));
        }

        return new FranchiseId(value);
    }

    public override string ToString() => Value;
}
