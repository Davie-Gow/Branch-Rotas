using WorkforceScheduling.Domain.Identifiers;
using Xunit;

namespace WorkforceScheduling.Domain.Tests.Identifiers;

public sealed class StableIdentifierTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" branch-001")]
    [InlineData("branch-001 ")]
    public void BranchId_rejects_empty_or_padded_values(string value)
    {
        Assert.Throws<ArgumentException>(() => BranchId.Create(value));
    }

    [Fact]
    public void BranchId_preserves_stable_identifier_without_name_normalization()
    {
        var id = BranchId.Create("Br-001");

        Assert.Equal("Br-001", id.Value);
    }
}
