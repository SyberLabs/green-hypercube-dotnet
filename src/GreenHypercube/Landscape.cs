namespace GreenHypercube;

public static class Landscape
{
    public static World Generate(
        int n,
        double signalStrength,
        double rewardDensity,
        ulong seed,
        ulong landscape = 0,
        double effortStrength = 0.5,
        double cueFromEffort = 0.0,
        double discoveryThreshold = 0.05)
    {
        if (n < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(n));
        }

        var rng = Pcg32.Stream(seed, landscape, RngPurpose.Generate);
        var s = Math.Clamp(signalStrength, 0.0, 1.0);
        var e = Math.Clamp(effortStrength, 0.0, 1.0);
        var c = Math.Clamp(cueFromEffort, 0.0, 1.0);
        if (s + c > 1.0)
        {
            var scale = 1.0 / (s + c);
            s *= scale;
            c *= scale;
        }

        var z = Standardize(GaussianVector(ref rng, n));
        var u = GaussianVector(ref rng, n);
        var effortMix = new double[n];
        var sqrtE = Math.Sqrt(e);
        var sqrtOneE = Math.Sqrt(1.0 - e);
        for (var i = 0; i < n; i++)
        {
            effortMix[i] = sqrtE * z[i] + sqrtOneE * u[i];
        }

        var effort = Standardize(effortMix);

        var noise = GaussianVector(ref rng, n);
        var sensoryMix = new double[n];
        var sqrtS = Math.Sqrt(s);
        var sqrtC = Math.Sqrt(c);
        var sqrtRest = Math.Sqrt(Math.Max(0.0, 1.0 - s - c));
        for (var i = 0; i < n; i++)
        {
            sensoryMix[i] = sqrtS * z[i] + sqrtC * effort[i] + sqrtRest * noise[i];
        }

        var sensory = Sigmoid(Standardize(sensoryMix));

        var assayNoise = GaussianVector(ref rng, n);
        var assay = new double[n];
        for (var i = 0; i < n; i++)
        {
            assay[i] = z[i] + 0.25 * assayNoise[i];
        }

        var target = Math.Max(1, (int)Math.Round(rewardDensity * n));
        var order = new int[n];
        for (var i = 0; i < n; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (a, b) => assay[b].CompareTo(assay[a]));
        var reward = new double[n];
        var lo = assay[order[target - 1]];
        var hi = assay[order[0]];
        var span = hi - lo + 1e-9;
        for (var k = 0; k < target; k++)
        {
            var idx = order[k];
            var unit = (assay[idx] - lo) / span;
            reward[idx] = 0.4 + 0.6 * unit;
        }

        return new World(
            new CueView(sensory),
            effort,
            new AssayVault(reward, discoveryThreshold));
    }

    internal static double[] GaussianVector(ref Pcg32 rng, int n)
    {
        var v = new double[n];
        for (var i = 0; i < n; i++)
        {
            v[i] = rng.NextGaussian();
        }

        return v;
    }

    internal static double[] Standardize(double[] v)
    {
        var mean = 0.0;
        for (var i = 0; i < v.Length; i++)
        {
            mean += v[i];
        }

        mean /= v.Length;
        var varSum = 0.0;
        for (var i = 0; i < v.Length; i++)
        {
            var d = v[i] - mean;
            varSum += d * d;
        }

        var sd = Math.Sqrt(varSum / v.Length);
        var outV = new double[v.Length];
        if (sd <= 0)
        {
            return outV;
        }

        for (var i = 0; i < v.Length; i++)
        {
            outV[i] = (v[i] - mean) / sd;
        }

        return outV;
    }

    internal static double[] Sigmoid(double[] v)
    {
        var outV = new double[v.Length];
        for (var i = 0; i < v.Length; i++)
        {
            outV[i] = 1.0 / (1.0 + Math.Exp(-1.8 * v[i]));
        }

        return outV;
    }
}
