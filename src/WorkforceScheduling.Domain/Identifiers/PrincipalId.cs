namespace WorkforceScheduling.Domain.Identifiers;

/// <summary>The authenticated Entra object identifier for a requesting principal.</summary>
public sealed record PrincipalId
{
    private PrincipalId(string value) => Value = value;

    /// <summary>Gets the unchanged Entra object identifier.</summary>
    public string Value { get; }

    /// <summary>Creates an identifier without normalizing or guessing its format.</summary>
    /// <param name="value">The authenticated principal's Entra object identifier.</param>
    /// <returns>The validated principal identifier.</returns>
    public static PrincipalId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("A principal identifier cannot contain surrounding whitespace.", nameof(value));
        }

        return new PrincipalId(value);
    }

    public override string ToString() => Value;
}
