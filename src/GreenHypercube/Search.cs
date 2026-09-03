namespace GreenHypercube;

/// <summary>
/// Pays for assay draws. Exposes <see cref="Cues"/> only — no reward vector,
/// no effort index, no vault.
/// </summary>
public sealed class SearchEnvironment
{
    private readonly AssayVault _assay;
    private readonly bool[] _tested;

    internal SearchEnvironment(ICueView cues, AssayVault assay)
    {
        Cues = cues;
        _assay = assay;
        _tested = new bool[cues.N];
    }

    public ICueView Cues { get; }
    public int TestedCount { get; private set; }

    public bool IsTested(int index) => _tested[index];

    public int CopyUntestedIndices(Span<int> destination)
    {
        var n = 0;
        for (var i = 0; i < _tested.Length; i++)
        {
            if (_tested[i])
            {
                continue;
            }

            if (n >= destination.Length)
            {
                throw new ArgumentException("Destination is too small for the untested set.", nameof(destination));
            }

            destination[n++] = i;
        }

        return n;
    }

    public double Experiment(int index)
    {
        if (!_tested[index])
        {
            _tested[index] = true;
            TestedCount++;
        }

        return _assay.Reveal(index);
    }
}

public sealed class EpisodeResult
{
    public required string Strategy { get; init; }
    public required bool[] UsefulInOrder { get; init; }
    public required int TotalUseful { get; init; }

    public double Audc()
    {
        if (TotalUseful == 0 || UsefulInOrder.Length == 0)
        {
            return 0.0;
        }

        var cumulative = 0.0;
        var sum = 0.0;
        for (var i = 0; i < UsefulInOrder.Length; i++)
        {
            if (UsefulInOrder[i])
            {
                cumulative += 1.0;
            }

            sum += cumulative;
        }

        return (sum / UsefulInOrder.Length) / TotalUseful;
    }
}

public interface IStrategy
{
    string Name { get; }
    void Reset();
    int Propose(SearchEnvironment env, ref Pcg32 rng);
    void Observe(int index, double reward);
}

public sealed class RandomSearch : IStrategy
{
    public string Name => "random";

    public void Reset()
    {
    }

    public int Propose(SearchEnvironment env, ref Pcg32 rng)
    {
        var n = env.Cues.N;
        Span<int> untested = n <= 512 ? stackalloc int[n] : new int[n];
        var count = env.CopyUntestedIndices(untested);
        return untested[rng.Next(count)];
    }

    public void Observe(int index, double reward)
    {
    }
}

public sealed class SensorySearch : IStrategy
{
    private readonly double[] _score;

    public SensorySearch(ICueView cues)
    {
        var salience = cues.SensorySalience;
        var max = 0.0;
        for (var i = 0; i < salience.Length; i++)
        {
            if (salience[i] > max)
            {
                max = salience[i];
            }
        }

        if (max <= 0)
        {
            max = 1.0;
        }

        _score = new double[cues.N];
        for (var i = 0; i < _score.Length; i++)
        {
            _score[i] = salience[i] / max;
        }
    }

    public string Name => "sensory";

    public void Reset()
    {
    }

    public int Propose(SearchEnvironment env, ref Pcg32 rng)
    {
        var n = env.Cues.N;
        Span<int> untested = n <= 512 ? stackalloc int[n] : new int[n];
        var count = env.CopyUntestedIndices(untested);
        if (rng.NextDouble() < 0.05)
        {
            return untested[rng.Next(count)];
        }

        var best = untested[0];
        var bestScore = double.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            var idx = untested[i];
            var score = _score[idx] + (rng.NextDouble() - 0.5) * 1e-9;
            if (score > bestScore)
            {
                bestScore = score;
                best = idx;
            }
        }

        return best;
    }

    public void Observe(int index, double reward)
    {
    }
}

public static class Engine
{
    public static EpisodeResult RunEpisode(World world, IStrategy strategy, int budget, ref Pcg32 rng)
    {
        strategy.Reset();
        var env = new SearchEnvironment(world.Cues, world.Assay);
        budget = Math.Min(budget, world.Cues.N);
        var useful = new bool[budget];
        var t = 0;
        while (env.TestedCount < budget)
        {
            var idx = strategy.Propose(env, ref rng);
            if (env.IsTested(idx))
            {
                break;
            }

            var reward = env.Experiment(idx);
            strategy.Observe(idx, reward);
            useful[t] = reward >= world.Assay.DiscoveryThreshold;
            t++;
        }

        if (t < useful.Length)
        {
            Array.Resize(ref useful, t);
        }

        return new EpisodeResult
        {
            Strategy = strategy.Name,
            UsefulInOrder = useful,
            TotalUseful = world.Assay.UsefulCount,
        };
    }
}
