using System.Windows;
using GrumphySwarm.Core.Abstractions;
using GrumphySwarm.Core.Settings;
using GrumphySwarm.Core.State;
using GrumphySwarm.Diagnostics;
using GrumphySwarm.Interop;
using GrumphySwarm.Lifecycle;
using GrumphySwarm.Services;
using GrumphySwarm.ViewModels;
using GrumphySwarm.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GrumphySwarm;

/// <summary>
/// Interaction logic for App.xaml.
/// Configures dependency injection, single instance mutex, tray icon, and global exception logging.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private SingleInstanceMutex? _singleInstanceMutex;
    private TrayIconManager? _trayIconManager;
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 1. Configure Dependency Injection & Logging first so early logs and errors are captured
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("=================================================");
            _logger.LogInformation("Grumphy Swarm application starting up (Milestone 1).");
            _logger.LogInformation("Process ID: {Pid}, Machine: {Machine}, OS: {OS}",
                Environment.ProcessId, Environment.MachineName, Environment.OSVersion);

            // 2. Global unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                _logger?.LogCritical(ex, "AppDomain unhandled exception.");
            };

            DispatcherUnhandledException += (s, args) =>
            {
                _logger?.LogError(args.Exception, "Dispatcher unhandled exception caught: {Message}", args.Exception.Message);
                args.Handled = true; // Prevent crash, but log thoroughly
            };

            // 3. Enforce single-instance application execution
            _singleInstanceMutex = new SingleInstanceMutex();
            if (!_singleInstanceMutex.IsOnlyInstance)
            {
                _logger.LogWarning("Another instance of Grumphy Swarm is already running. Showing alert and exiting.");
                MessageBox.Show(
                    "An instance of Grumphy Swarm is already running in your desktop tray.",
                    "Grumphy Swarm",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Shutdown();
                return;
            }

            // 4. Load persisted settings synchronously
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            var settings = settingsService.LoadSettings();
            _logger.LogInformation("Settings loaded: Collapsed=({CW}x{CH}), Expanded=({EW}x{EH})",
                settings.CollapsedWidth, settings.CollapsedHeight, settings.ExpandedWidth, settings.ExpandedHeight);

            // 5. Create and show Notch window
            var notchWindow = _serviceProvider.GetRequiredService<NotchWindow>();
            MainWindow = notchWindow;

            _logger.LogInformation("Showing NotchWindow...");
            notchWindow.Show();
            _logger.LogInformation("NotchWindow.Show() completed. Visibility: {Visibility}, Topmost: {Topmost}",
                notchWindow.Visibility, notchWindow.Topmost);

            // 6. Initialize System Tray
            var viewModel = _serviceProvider.GetRequiredService<NotchViewModel>();
            _trayIconManager = new TrayIconManager(notchWindow, _serviceProvider.GetService<ILogger<TrayIconManager>>());
            _trayIconManager.Initialize();

            _trayIconManager.ToggleRequested += (_, _) =>
            {
                _logger.LogInformation("Tray icon ToggleRequested received.");
                viewModel.ToggleExpandCommand.Execute(null);
            };

            _trayIconManager.PinToggled += (_, _) =>
            {
                _logger.LogInformation("Tray icon PinToggled received.");
                viewModel.TogglePinCommand.Execute(null);
            };

            _trayIconManager.ExitRequested += (_, _) =>
            {
                _logger.LogInformation("Tray icon ExitRequested received.");
                Shutdown();
            };

            _trayIconManager.SettingsRequested += (_, _) =>
            {
                MessageBox.Show(
                    "Settings panel and Agent configuration will be introduced in Milestone 2.\n" +
                    "Configuration is persisted at %LocalAppData%\\GrumphySwarm\\settings.json.",
                    "Grumphy Swarm Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };

            // Synchronize tray state with ViewModel
            viewModel.PropertyChanged += (_, prop) =>
            {
                if (prop.PropertyName == nameof(NotchViewModel.IsPinned))
                {
                    _trayIconManager.IsPinned = viewModel.IsPinned;
                }
                else if (prop.PropertyName == nameof(NotchViewModel.IsExpanded))
                {
                    _trayIconManager.IsExpanded = viewModel.IsExpanded;
                }
            };

            _logger.LogInformation("Grumphy Swarm initialized and running successfully.");
        }
        catch (Exception ex)
        {
            _logger?.LogCritical(ex, "Fatal exception during startup: {Message}", ex.Message);
            MessageBox.Show(
                $"Grumphy Swarm failed to initialize:\n{ex.Message}\n\nCheck logs at %LocalAppData%\\GrumphySwarm\\app.log",
                "Grumphy Swarm Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider());
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Core abstractions & singletons
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<INotchStateMachine, NotchStateMachine>();
        services.AddSingleton<DisplayManager>();

        // Native Windows 11 System Integrations
        services.AddSingleton<IAudioVolumeService, WindowsAudioVolumeService>();
        services.AddSingleton<IBrightnessService, WmiBrightnessService>();
        services.AddSingleton<IMediaSessionService, WindowsMediaSessionService>();
        services.AddSingleton<IAgentProfileService, JsonAgentProfileService>();

        // MVVM
        services.AddSingleton<NotchViewModel>();
        services.AddSingleton<NotchWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("Grumphy Swarm shutting down cleanly.");

        _trayIconManager?.Dispose();
        _serviceProvider?.Dispose();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}
