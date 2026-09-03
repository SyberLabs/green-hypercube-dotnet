using GreenHypercube;
using Xunit;

namespace GreenHypercube.Tests;

public sealed class Pcg32Tests
{
    [Fact]
    public void Same_stream_is_deterministic()
    {
        var a = Pcg32.Stream(42, 3, 9);
        var b = Pcg32.Stream(42, 3, 9);
        Assert.Equal(a.NextUInt32(), b.NextUInt32());
        Assert.Equal(a.NextUInt32(), b.NextUInt32());
    }

    [Fact]
    public void Different_landscapes_are_not_the_same_stream()
    {
        var a = Pcg32.Stream(42, 0, RngPurpose.Generate);
        var b = Pcg32.Stream(42, 1, RngPurpose.Generate);
        Assert.NotEqual(a.NextUInt32(), b.NextUInt32());
    }
}
