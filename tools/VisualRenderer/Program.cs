using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using GrumphySwarm.Interop;
using GrumphySwarm.ViewModels;
using GrumphySwarm.Views;

namespace VisualRenderer;

public static class Program
{
    private class DummySettingsService : ISettingsService
    {
        public NotchSettings CurrentSettings { get; set; } = new();
        public NotchSettings LoadSettings() => CurrentSettings;
        public Task<NotchSettings> LoadSettingsAsync(CancellationToken cancellationToken = default) => Task.FromResult(CurrentSettings);
        public Task SaveSettingsAsync(NotchSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public event EventHandler<NotchSettings>? SettingsChanged;
    }

    private class MockClock : ISystemClock
    {
        public DateTimeOffset Now => new(2024, 8, 5, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => new(2024, 8, 5, 12, 0, 0, TimeSpan.Zero);
    }

    [STAThread]
    public static void Main(string[] args)
    {
        if (Application.Current == null)
        {
            new Application();
        }

        if (!Application.Current.Resources.MergedDictionaries.Any(d => d.Source?.OriginalString.Contains("Colors.xaml") == true))
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/GrumphySwarm;component/Resources/Colors.xaml", UriKind.Absolute)
            });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/GrumphySwarm;component/Resources/Icons.xaml", UriKind.Absolute)
            });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/GrumphySwarm;component/Resources/Styles.xaml", UriKind.Absolute)
            });
        }

        var settingsService = new DummySettingsService();
        var stateMachine = new NotchStateMachine();
        var displayManager = new DisplayManager();
        using var viewModel = new NotchViewModel(stateMachine, settingsService, clock: new MockClock());

        var window = new NotchWindow(viewModel, displayManager);
        window.ApplyTransitionProgress(1.0);

        var container = window.NotchContainerElement;
        container.Measure(new Size(700, 160));
        container.Arrange(new Rect(0, 0, 660, 140));
        container.UpdateLayout();


        foreach (var grid in container.FindVisualChildren<System.Windows.Controls.Grid>())
        {
            if (grid.ColumnDefinitions.Count == 5)
            {
                Console.WriteLine("Nook 5-column grid children:");
                foreach (UIElement child in grid.Children)
                {
                    var feChild = child as FrameworkElement;
                    var transform = child.TransformToAncestor(container);
                    var pt = transform.Transform(new Point(0, 0));
                    var r = new Rect(pt.X, pt.Y, feChild?.ActualWidth ?? 0, feChild?.ActualHeight ?? 0);
                    Console.WriteLine($"  Col {System.Windows.Controls.Grid.GetColumn(child)} ({child.GetType().Name}): Rect={r}");
                }
            }
        }

        var rtb = new RenderTargetBitmap(660, 140, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(container);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        string baseDir = Directory.GetCurrentDirectory();
        string outRendered = Path.Combine(baseDir, "notch_visual_rendered.png");
        string outScreenshot = Path.Combine(baseDir, "current_screenshot.png");

        using (var fs = File.Create(outRendered))
        {
            encoder.Save(fs);
        }

        File.Copy(outRendered, outScreenshot, overwrite: true);
        Console.WriteLine($"Rendered successfully to {outRendered} and {outScreenshot}");

        // Render Collapsed State
        window.ApplyTransitionProgress(0.0);
        container.Measure(new Size(300, 60));
        container.Arrange(new Rect(0, 0, 240, 34));
        container.UpdateLayout();

        var rtbCollapsed = new RenderTargetBitmap(240, 34, 96, 96, PixelFormats.Pbgra32);
        rtbCollapsed.Render(container);
        var encCollapsed = new PngBitmapEncoder();
        encCollapsed.Frames.Add(BitmapFrame.Create(rtbCollapsed));
        string outCollapsed = Path.Combine(baseDir, "notch_collapsed.png");
        using (var fs = File.Create(outCollapsed))
        {
            encCollapsed.Save(fs);
        }
        Console.WriteLine($"Rendered collapsed notch to {outCollapsed}");

        // Render Agents Tab
        viewModel.SelectTabCommand.Execute("Agents");
        window.ApplyTransitionProgress(1.0);
        container.Measure(new Size(700, 160));
        container.Arrange(new Rect(0, 0, 660, 140));
        container.UpdateLayout();

        var rtbAgents = new RenderTargetBitmap(660, 140, 96, 96, PixelFormats.Pbgra32);
        rtbAgents.Render(container);
        var encAgents = new PngBitmapEncoder();
        encAgents.Frames.Add(BitmapFrame.Create(rtbAgents));
        string outAgents = Path.Combine(baseDir, "notch_agents.png");
        using (var fs = File.Create(outAgents))
        {
            encAgents.Save(fs);
        }
        Console.WriteLine($"Rendered agents tab to {outAgents}");
    }

    public static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T t) yield return t;
            foreach (var childOfChild in FindVisualChildren<T>(child))
            {
                yield return childOfChild;
            }
        }
    }
}
