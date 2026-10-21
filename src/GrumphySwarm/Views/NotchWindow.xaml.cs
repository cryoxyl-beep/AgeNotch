using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using GrumphySwarm.Animations;
using GrumphySwarm.Interop;
using GrumphySwarm.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Views;

/// <summary>
/// Native host window for the Grumphy Swarm notch.
/// Implements transparent, non-activating, top-most window positioning with DPI awareness,
/// unified transition orchestration, and mathematical flared notch silhouette rendering.
/// </summary>
public partial class NotchWindow : Window
{
    public static readonly DependencyProperty TransitionProgressProperty =
        DependencyProperty.Register(
            nameof(TransitionProgress),
            typeof(double),
            typeof(NotchWindow),
            new FrameworkPropertyMetadata(0.0, OnTransitionProgressPropertyChanged));

    private readonly NotchViewModel _viewModel;
    private readonly DisplayManager _displayManager;
    private readonly ILogger<NotchWindow> _logger;

    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private bool _isAnimating;

    public bool IsAnimating => _isAnimating;

    public double TransitionProgress
    {
        get => (double)GetValue(TransitionProgressProperty);
        set => SetValue(TransitionProgressProperty, value);
    }

    public NotchWindow(
        NotchViewModel viewModel,
        DisplayManager displayManager,
        ILogger<NotchWindow>? logger = null)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _displayManager = displayManager ?? throw new ArgumentNullException(nameof(displayManager));
        _logger = logger ?? NullLogger<NotchWindow>.Instance;

        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _displayManager.DisplayConfigurationChanged += OnDisplayConfigurationChanged;

        Loaded += OnWindowLoaded;
    }

    private static void OnTransitionProgressPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NotchWindow window && e.NewValue is double progress)
        {
            window.ApplyTransitionProgress(progress);
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        _hwnd = helper.Handle;

        if (_hwnd != IntPtr.Zero)
        {
            ApplyNativeWindowStyles(_hwnd);

            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            _logger.LogInformation("Native window initialized with handle 0x{Hwnd:X}.", _hwnd.ToInt64());
        }

        UpdateWindowBoundsAndPosition(isExpanded: _viewModel.IsExpanded, animated: false);
    }

    private void ApplyNativeWindowStyles(IntPtr hwnd)
    {
        try
        {
            // Add WS_EX_TOOLWINDOW (hide from Alt+Tab) and WS_EX_NOACTIVATE (prevent stealing keyboard focus)
            long currentExStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            long newExStyle = currentExStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newExStyle));

            // Re-assert topmost without stealing focus and apply frame changes
            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_FRAMECHANGED);

            _logger.LogInformation("Native window styles applied successfully. Handle: 0x{Hwnd:X}, Before: 0x{Before:X8}, After: 0x{After:X8}",
                hwnd.ToInt64(), currentExStyle, newExStyle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply native window styles to hwnd 0x{Hwnd:X}", hwnd.ToInt64());
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case NativeMethods.WM_MOUSEACTIVATE:
                // Return MA_NOACTIVATE: prevents Windows from stealing foreground focus from the user's active application
                handled = true;
                return new IntPtr(NativeMethods.MA_NOACTIVATE);

            case NativeMethods.WM_DPICHANGED:
            case NativeMethods.WM_DISPLAYCHANGE:
            case NativeMethods.WM_SETTINGCHANGE:
                _logger.LogInformation("Received display/DPI change message (0x{Msg:X}). Recalculating notch placement.", msg);
                Dispatcher.InvokeAsync(() => UpdateWindowBoundsAndPosition(_viewModel.IsExpanded, animated: false));
                break;
        }

        return IntPtr.Zero;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        UpdateWindowBoundsAndPosition(isExpanded: _viewModel.IsExpanded, animated: false);
    }

    private void OnDisplayConfigurationChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() => UpdateWindowBoundsAndPosition(_viewModel.IsExpanded, animated: false));
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotchViewModel.IsExpanded))
        {
            UpdateWindowBoundsAndPosition(isExpanded: _viewModel.IsExpanded, animated: true);
        }
    }

    /// <summary>
    /// Synchronously applies the exact interpolated shape geometry and content opacities
    /// for a given transition progress value in [0.0, 1.0].
    /// </summary>
    public void ApplyTransitionProgress(double progress)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);

        double collapsedW = _viewModel.CollapsedWidth;
        double expandedW = _viewModel.ExpandedWidth;
        double collapsedH = _viewModel.CollapsedHeight;
        double expandedH = _viewModel.ExpandedHeight;
        double flareCollapsed = _viewModel.FlareRadiusCollapsed;
        double flareExpanded = _viewModel.FlareRadiusExpanded;
        double cornerCollapsed = _viewModel.CornerRadiusCollapsed;
        double cornerExpanded = _viewModel.CornerRadiusExpanded;

        // Smooth continuous linear interpolation of silhouette parameters
        double currentW = collapsedW + (expandedW - collapsedW) * progress;
        double currentH = collapsedH + (expandedH - collapsedH) * progress;
        double currentFlare = flareCollapsed + (flareExpanded - flareCollapsed) * progress;
        double currentCorner = cornerCollapsed + (cornerExpanded - cornerCollapsed) * progress;

        NotchContainer.Width = currentW;
        NotchContainer.Height = currentH;

        var geometry = NotchGeometryBuilder.BuildGeometry(currentW, currentH, currentFlare, currentCorner);
        NotchBackgroundPath.Data = geometry;
        NotchClippedContent.Clip = geometry;

        // Content visibility and non-overlapping opacity cross-fade:
        // Progress <= 0.20: Collapsed content fades 1.0 -> 0.0
        // Progress >= 0.20: Expanded content fades 0.0 -> 1.0
        if (progress <= 0.001)
        {
            CollapsedContentPanel.Opacity = 1.0;
            CollapsedContentPanel.Visibility = Visibility.Visible;
            CollapsedContentPanel.IsHitTestVisible = true;

            ExpandedContentPanel.Opacity = 0.0;
            ExpandedContentPanel.Visibility = Visibility.Collapsed;
            ExpandedContentPanel.IsHitTestVisible = false;
        }
        else if (progress >= 0.999)
        {
            CollapsedContentPanel.Opacity = 0.0;
            CollapsedContentPanel.Visibility = Visibility.Collapsed;
            CollapsedContentPanel.IsHitTestVisible = false;

            ExpandedContentPanel.Opacity = 1.0;
            ExpandedContentPanel.Visibility = Visibility.Visible;
            ExpandedContentPanel.IsHitTestVisible = true;
        }
        else if (progress < 0.20)
        {
            double fade = 1.0 - (progress / 0.20);
            CollapsedContentPanel.Opacity = Math.Clamp(fade, 0.0, 1.0);
            CollapsedContentPanel.Visibility = Visibility.Visible;
            CollapsedContentPanel.IsHitTestVisible = false;

            ExpandedContentPanel.Opacity = 0.0;
            ExpandedContentPanel.Visibility = Visibility.Collapsed;
            ExpandedContentPanel.IsHitTestVisible = false;
        }
        else
        {
            double fade = (progress - 0.20) / 0.80;
            CollapsedContentPanel.Opacity = 0.0;
            CollapsedContentPanel.Visibility = Visibility.Collapsed;
            CollapsedContentPanel.IsHitTestVisible = false;

            ExpandedContentPanel.Opacity = Math.Clamp(fade, 0.0, 1.0);
            ExpandedContentPanel.Visibility = Visibility.Visible;
            ExpandedContentPanel.IsHitTestVisible = progress > 0.85;
        }
    }

    /// <summary>
    /// Recalculates and adjusts window host bounds and animates the notch card smoothly.
    /// </summary>
    public void UpdateWindowBoundsAndPosition(bool isExpanded, bool animated)
    {
        var screenWidth = _displayManager.PrimaryScreenWidthDips;

        double targetWindowWidth = _viewModel.ExpandedWidth + 40;
        double targetWindowHeight = _viewModel.ExpandedHeight + 40;
        double compactWindowWidth = _viewModel.CollapsedWidth + 40;
        double compactWindowHeight = _viewModel.CollapsedHeight + 20;

        if (!animated)
        {
            // Clear any active animation and snap immediately
            BeginAnimation(TransitionProgressProperty, null);

            double targetProgress = isExpanded ? 1.0 : 0.0;
            TransitionProgress = targetProgress;
            ApplyTransitionProgress(targetProgress);

            double winWidth = isExpanded ? targetWindowWidth : compactWindowWidth;
            double winHeight = isExpanded ? targetWindowHeight : compactWindowHeight;
            SetWindowHostBounds(winWidth, winHeight, screenWidth);

            _logger.LogInformation("UpdateWindowBoundsAndPosition (Snap): isExpanded={IsExpanded}, Window=({Left},{Top},{Width},{Height})",
                isExpanded, Left, Top, Width, Height);
            return;
        }

        _isAnimating = true;

        if (isExpanded)
        {
            // Immediately expand host window canvas so opening animation never clips
            SetWindowHostBounds(targetWindowWidth, targetWindowHeight, screenWidth);

            var expandAnim = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            expandAnim.Completed += (_, _) =>
            {
                _isAnimating = false;
            };

            BeginAnimation(TransitionProgressProperty, expandAnim, HandoffBehavior.SnapshotAndReplace);
        }
        else
        {
            var collapseAnim = new DoubleAnimation
            {
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            collapseAnim.Completed += (_, _) =>
            {
                _isAnimating = false;

                // When fully collapsed and no expand request superseded this animation,
                // safely shrink host window bounds back to compact size
                if (!_viewModel.IsExpanded && TransitionProgress <= 0.05)
                {
                    SetWindowHostBounds(compactWindowWidth, compactWindowHeight, screenWidth);
                }
            };

            BeginAnimation(TransitionProgressProperty, collapseAnim, HandoffBehavior.SnapshotAndReplace);
        }
    }

    private void SetWindowHostBounds(double width, double height, double screenWidth)
    {
        Width = width;
        Height = height;
        Left = Math.Max(0.0, (screenWidth - width) / 2.0);
        Top = 0.0;
    }

    private void OnNotchMouseEnter(object sender, MouseEventArgs e)
    {
        _viewModel.HandleMouseEnterCommand.Execute(null);
    }

    private void OnNotchMouseLeave(object sender, MouseEventArgs e)
    {
        _viewModel.HandleMouseLeaveCommand.Execute(null);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _displayManager.DisplayConfigurationChanged -= OnDisplayConfigurationChanged;
        _hwndSource?.RemoveHook(WndProc);
    }
}
