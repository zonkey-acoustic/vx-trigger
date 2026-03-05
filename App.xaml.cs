using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using ShotTrigger.Services;
using ShotTrigger.Views;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace ShotTrigger;

public partial class App : Application
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private Forms.NotifyIcon? _trayIcon;
    private TriggerOrchestrator? _orchestrator;
    private ConfigWindow? _configWindow;
    private Forms.ToolStripMenuItem? _statusItem;
    private Forms.ToolStripMenuItem? _toggleItem;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _orchestrator = new TriggerOrchestrator();
        _orchestrator.StatusChanged += OnStatusChanged;
        _orchestrator.ShotFired += OnShotFired;

        SetupTrayIcon();

        // Auto-start monitoring if a trigger is enabled
        if (_orchestrator.Settings.AudioTriggerEnabled || _orchestrator.Settings.NetworkTriggerEnabled)
        {
            _orchestrator.Start();
            UpdateTrayState();
        }
        else
        {
            UpdateTrayState();
            // Show config on first run if nothing is configured
            ShowConfigWindow();
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = CreateTrayIcon(Color.Gray),
            Visible = true,
            Text = "Shot Trigger"
        };

        var menu = new Forms.ContextMenuStrip();

        _statusItem = new Forms.ToolStripMenuItem("Not monitoring")
        {
            Enabled = false
        };
        menu.Items.Add(_statusItem);
        menu.Items.Add(new Forms.ToolStripSeparator());

        var configItem = new Forms.ToolStripMenuItem("Configure...");
        configItem.Click += (_, _) => ShowConfigWindow();
        menu.Items.Add(configItem);

        _toggleItem = new Forms.ToolStripMenuItem("Start Monitoring");
        _toggleItem.Click += (_, _) => ToggleMonitoring();
        menu.Items.Add(_toggleItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowConfigWindow();
    }

    private void ToggleMonitoring()
    {
        if (_orchestrator == null) return;

        if (_orchestrator.IsRunning)
        {
            _orchestrator.Stop();
        }
        else
        {
            _orchestrator.Start();
        }

        UpdateTrayState();
    }

    private void UpdateTrayState()
    {
        if (_trayIcon == null || _orchestrator == null || _statusItem == null || _toggleItem == null)
            return;

        if (_orchestrator.IsRunning)
        {
            SetTrayIcon(Color.LimeGreen);
            _toggleItem.Text = "Stop Monitoring";

            var tooltip = $"Shot Trigger - Monitoring";
            if (_orchestrator.ShotCount > 0)
                tooltip += $"\nShots: {_orchestrator.ShotCount}";
            if (_orchestrator.LastShotTime.HasValue)
                tooltip += $"\nLast: {_orchestrator.LastShotTime:HH:mm:ss}";
            _trayIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
        }
        else
        {
            var hasConfig = _orchestrator.Settings.AudioTriggerEnabled || _orchestrator.Settings.NetworkTriggerEnabled;
            SetTrayIcon(hasConfig ? Color.Yellow : Color.Gray);
            _toggleItem.Text = "Start Monitoring";
            _trayIcon.Text = "Shot Trigger - Stopped";
        }
    }

    private void OnStatusChanged(object? sender, string status)
    {
        Current.Dispatcher.Invoke(() =>
        {
            if (_statusItem != null)
                _statusItem.Text = status;
            UpdateTrayState();
        });
    }

    private void OnShotFired(object? sender, EventArgs e)
    {
        Current.Dispatcher.Invoke(() =>
        {
            UpdateTrayState();
        });
    }

    private void ShowConfigWindow()
    {
        if (_configWindow != null && _configWindow.IsVisible)
        {
            _configWindow.Activate();
            return;
        }

        _configWindow = new ConfigWindow(_orchestrator!);
        _configWindow.Closed += (_, _) =>
        {
            _configWindow = null;
            UpdateTrayState();
        };
        _configWindow.Show();
    }

    private void ExitApp()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        Shutdown(); // Triggers OnExit for orchestrator cleanup
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _orchestrator?.Dispose();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        base.OnExit(e);
    }

    private void SetTrayIcon(Color color)
    {
        if (_trayIcon == null) return;

        var oldIcon = _trayIcon.Icon;
        _trayIcon.Icon = CreateTrayIcon(color);
        oldIcon?.Dispose();
    }

    private static Icon CreateTrayIcon(Color color)
    {
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        using var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1);
        g.DrawEllipse(pen, 1, 1, 14, 14);

        var hIcon = bitmap.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }
}
