using System.Collections.ObjectModel;
using System.Windows;
using GreenHypercube;

namespace GreenHypercube.Desk;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Rows.ItemsSource = _rows;
    }

    private readonly ObservableCollection<RowVm> _rows = [];

    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        RunButton.IsEnabled = false;
        _rows.Clear();
        Status.Text = "Running…";
        Progress.Value = 0;
        var progress = new Progress<StudyProgress>(p =>
        {
            Progress.Maximum = p.Total;
            Progress.Value = p.Completed;
            Status.Text = $"Landscapes {p.Completed} / {p.Total}";
        });

        try
        {
            var results = await Task.Run(() => RunAll(progress));
            foreach (var row in results)
            {
                _rows.Add(row);
            }

            Status.Text = "Done. Global shuffle should cover zero. Within-effort keeps an effort proxy.";
        }
        catch (Exception ex)
        {
            Status.Text = ex.Message;
        }
        finally
        {
            RunButton.IsEnabled = true;
        }
    }

    private static List<RowVm> RunAll(IProgress<StudyProgress> progress)
    {
        const int landscapes = 24;
        const int conditions = 7;
        var offset = 0;
        var rows = new List<RowVm>();

        void Add(string name, StudySpec spec, NullKind nullKind)
        {
            var slice = new OffsetProgress(progress, offset, landscapes * conditions);
            var r = Study.SensoryAdvantage(spec, nullKind, slice);
            offset += landscapes;
            rows.Add(new RowVm(
                name,
                r.Mean.ToString("F3"),
                $"[{r.Ci95Low:F3}, {r.Ci95High:F3}]"));
        }

        StudySpec Spec(double signal, double effort = 0.5, double cueFromEffort = 0) => new()
        {
            Landscapes = landscapes,
            N = 120,
            Budget = 70,
            Seed = 100,
            SignalStrength = signal,
            EffortStrength = effort,
            CueFromEffort = cueFromEffort,
            RewardDensity = 0.15,
            RandomReplicates = 8,
        };

        Add("signal=0, real assay", Spec(0.0), NullKind.None);
        Add("signal=0.85, real assay", Spec(0.85), NullKind.None);
        Add("signal=0.85, global shuffle", Spec(0.85), NullKind.PermuteReward);
        Add("signal=0.85, within-effort (effort independent)", Spec(0.85, 0.0), NullKind.PermuteRewardWithinEffort);
        Add("mirage (cue=effort), real assay", Spec(0.0, 0.95, 0.9), NullKind.None);
        Add("mirage, within-effort shuffle", Spec(0.0, 0.95, 0.9), NullKind.PermuteRewardWithinEffort);
        Add("mirage, global shuffle", Spec(0.0, 0.95, 0.9), NullKind.PermuteReward);
        return rows;
    }

    private sealed class OffsetProgress : IProgress<StudyProgress>
    {
        private readonly IProgress<StudyProgress> _inner;
        private readonly int _offset;
        private readonly int _total;

        public OffsetProgress(IProgress<StudyProgress> inner, int offset, int total)
        {
            _inner = inner;
            _offset = offset;
            _total = total;
        }

        public void Report(StudyProgress value)
        {
            _inner.Report(new StudyProgress(_offset + value.Completed, _total));
        }
    }

    public sealed record RowVm(string Condition, string Mean, string Interval);
}
