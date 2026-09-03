using System.Reflection;
using GreenHypercube;
using Xunit;

namespace GreenHypercube.Tests;

public sealed class VaultSurfaceTests
{
    [Fact]
    public void Cue_view_has_no_reward_or_effort()
    {
        Assert.Null(typeof(ICueView).GetProperty("Reward"));
        Assert.Null(typeof(ICueView).GetProperty("Effort"));
        Assert.Null(typeof(ICueView).GetProperty("Assay"));
        var names = typeof(ICueView).GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToHashSet();
        Assert.Contains("N", names);
        Assert.Contains("SensorySalience", names);
    }

    [Fact]
    public void Search_environment_does_not_expose_the_vault()
    {
        Assert.Null(typeof(SearchEnvironment).GetProperty("Manifold"));
        Assert.Null(typeof(SearchEnvironment).GetProperty("World"));
        Assert.Null(typeof(SearchEnvironment).GetProperty("Reward"));
        Assert.Null(typeof(SearchEnvironment).GetProperty("Assay"));
        Assert.NotNull(typeof(SearchEnvironment).GetProperty("Cues"));
        Assert.NotNull(typeof(SearchEnvironment).GetMethod("Experiment"));
    }

    [Fact]
    public void Sensory_search_constructs_from_cue_view_only()
    {
        var ctor = typeof(SensorySearch).GetConstructors().Single();
        var args = ctor.GetParameters();
        Assert.Equal(typeof(ICueView), args[0].ParameterType);
    }

    [Fact]
    public void World_effort_is_not_on_the_cue_view()
    {
        var world = Landscape.Generate(40, 0.8, 0.2, seed: 7);
        Assert.Equal(world.Cues.N, world.Effort.Length);
        Assert.Null(world.Cues.GetType().GetProperty("Effort"));
    }
}
