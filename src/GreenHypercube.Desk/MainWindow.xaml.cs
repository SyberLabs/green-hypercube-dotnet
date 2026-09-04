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
            var results = await Task.Run(() => StudyScenarios.RunDemonstration(progress));
            foreach (var row in results)
            {
                _rows.Add(new RowVm(
                    row.Label,
                    row.Result.Mean.ToString("F3"),
                    $"[{row.Result.Ci95Low:F3}, {row.Result.Ci95High:F3}]"));
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

    public sealed record RowVm(string Condition, string Mean, string Interval);
}
