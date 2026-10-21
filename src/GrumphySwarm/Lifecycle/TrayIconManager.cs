using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using GrumphySwarm.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrumphySwarm.Lifecycle;

/// <summary>
/// Manages the Windows system tray icon and context menu using native Shell_NotifyIcon.
/// Avoids external dependencies while providing high-DPI crisp visuals and full WPF context menu support.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private const int TrayIconId = 1001;

    private readonly ILogger<TrayIconManager> _logger;
    private readonly Window _ownerWindow;
    private IntPtr _hwnd;
    private HwndSource? _hwndSource;
    private IntPtr _hIcon = IntPtr.Zero;
    private bool _isIconAdded;
    private bool _disposed;

    public event EventHandler? ToggleRequested;
    public event EventHandler? PinToggled;
    public event EventHandler? ExitRequested;
    public event EventHandler? SettingsRequested;

    public bool IsPinned { get; set; }
    public bool IsExpanded { get; set; }

    public TrayIconManager(Window ownerWindow, ILogger<TrayIconManager>? logger = null)
    {
        _ownerWindow = ownerWindow ?? throw new ArgumentNullException(nameof(ownerWindow));
        _logger = logger ?? NullLogger<TrayIconManager>.Instance;
    }

    public void Initialize()
    {
        var helper = new WindowInteropHelper(_ownerWindow);
        _hwnd = helper.Handle;

        if (_hwnd == IntPtr.Zero)
        {
            _logger.LogError("Window handle is null during TrayIconManager initialization.");
            return;
        }

        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);

        CreateAndAddTrayIcon();
    }

    private void CreateAndAddTrayIcon()
    {
        try
        {
            _hIcon = CreateNotchIconHandle();

            var data = new NativeMethods.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = TrayIconId,
                uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP,
                uCallbackMessage = NativeMethods.WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "Grumphy Swarm Notch",
                szInfo = string.Empty,
                szInfoTitle = string.Empty
            };

            // Remove any stale icon registration for this HWND/ID
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);

            // Attempt addition with standard V4 size (976 on 64-bit)
            _isIconAdded = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data);

            // If V4 size was rejected by shell, fallback to V3 size (952)
            if (!_isIconAdded)
            {
                data.cbSize = 952;
                _isIconAdded = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data);
            }

            // If still not added, try modifying in case it was recognized as existing
            if (!_isIconAdded)
            {
                _isIconAdded = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref data);
            }

            if (!_isIconAdded)
            {
                int err = Marshal.GetLastWin32Error();
                _logger.LogWarning("Failed to add system tray icon. Win32 Error: {Error}, HIcon: 0x{HIcon:X}, cbSize: {Size}",
                    err, _hIcon.ToInt64(), data.cbSize);
            }
            else
            {
                _logger.LogInformation("System tray icon registered successfully (cbSize: {Size}).", data.cbSize);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while registering system tray icon.");
        }
    }

    private static IntPtr CreateNotchIconHandle()
    {
        const int size = 32;
        using var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Draw miniature notch pill at the top of the icon
            using var pillBrush = new SolidBrush(Color.FromArgb(255, 18, 18, 22));
            using var borderPen = new Pen(Color.FromArgb(255, 60, 60, 70), 1.5f);

            // Rounded notch rectangle: flush with top
            var path = new GraphicsPath();
            float x = 3f, y = 2f, w = 26f, h = 18f, r = 7f;
            path.AddArc(x, y + h - r, r, r, 90, 90);
            path.AddLine(x, y, x + w, y);
            path.AddArc(x + w - r, y + h - r, r, r, 0, 90);
            path.CloseFigure();

            g.FillPath(pillBrush, path);
            g.DrawPath(borderPen, path);

            // Glowing indicator dot (Emerald green)
            using var dotBrush = new SolidBrush(Color.FromArgb(255, 16, 185, 129));
            g.FillEllipse(dotBrush, 13f, 7f, 6f, 6f);
        }

        return bitmap.GetHicon();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_TRAYICON)
        {
            int eventId = lParam.ToInt32();
            switch (eventId)
            {
                case NativeMethods.WM_LBUTTONUP:
                case NativeMethods.WM_LBUTTONDBLCLK:
                    ToggleRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;

                case NativeMethods.WM_RBUTTONUP:
                case NativeMethods.WM_CONTEXTMENU:
                    ShowContextMenu();
                    handled = true;
                    break;
            }
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        var menu = new ContextMenu
        {
            Background = System.Windows.Media.Brushes.Black,
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 48)),
            BorderThickness = new Thickness(1)
        };

        var titleItem = new MenuItem
        {
            Header = "Grumphy Swarm (Milestone 1)",
            IsEnabled = false,
            FontWeight = FontWeights.Bold
        };
        menu.Items.Add(titleItem);
        menu.Items.Add(new Separator());

        var toggleItem = new MenuItem
        {
            Header = IsExpanded ? "Collapse Notch" : "Expand Notch"
        };
        toggleItem.Click += (_, _) => ToggleRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(toggleItem);

        var pinItem = new MenuItem
        {
            Header = "Pin Notch",
            IsCheckable = true,
            IsChecked = IsPinned
        };
        pinItem.Click += (_, _) => PinToggled?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(pinItem);

        var settingsItem = new MenuItem
        {
            Header = "Settings..."
        };
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem
        {
            Header = "Exit Grumphy Swarm"
        };
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        // Required Win32 sequence to properly dismiss context menu when clicking away
        NativeMethods.SetForegroundWindow(_hwnd);
        menu.IsOpen = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_isIconAdded && _hwnd != IntPtr.Zero)
        {
            var data = new NativeMethods.NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = TrayIconId
            };
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);
            _isIconAdded = false;
        }

        if (_hIcon != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

        _hwndSource?.RemoveHook(WndProc);
    }
}
