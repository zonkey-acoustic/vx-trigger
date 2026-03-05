using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ShotTrigger.Services;

namespace ShotTrigger.Views;

public partial class ConfigWindow : Window
{
    private readonly TriggerOrchestrator _orchestrator;
    private bool _isInitialized;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public ConfigWindow(TriggerOrchestrator orchestrator)
    {
        InitializeComponent();
        _orchestrator = orchestrator;

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int value = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        };

        LoadSettingsToUI();
        UpdateStatus();

        _orchestrator.StatusChanged += OnOrchestratorStatusChanged;
        _orchestrator.ShotFired += OnOrchestratorShotFired;
        Closed += OnWindowClosed;

        _isInitialized = true;
        UpdateSliderDisplayValues();
        UpdateVisibility();
    }

    private void OnOrchestratorStatusChanged(object? sender, string status) => Dispatcher.Invoke(UpdateStatus);
    private void OnOrchestratorShotFired(object? sender, EventArgs e) => Dispatcher.Invoke(UpdateStatus);

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _orchestrator.StatusChanged -= OnOrchestratorStatusChanged;
        _orchestrator.ShotFired -= OnOrchestratorShotFired;
    }

    private void LoadSettingsToUI()
    {
        var settings = _orchestrator.Settings;

        // Shots directory
        ShotsDirectoryTextBox.Text = settings.ShotsDirectoryPath;

        // Audio devices
        var devices = _orchestrator.AudioTrigger.GetAudioOutputDevices();
        AudioDeviceCombo.ItemsSource = devices;
        if (settings.SelectedDeviceIndex >= 0 && settings.SelectedDeviceIndex < devices.Count)
            AudioDeviceCombo.SelectedIndex = settings.SelectedDeviceIndex;

        // Trigger type
        if (settings.NetworkTriggerEnabled)
            NetworkRadio.IsChecked = true;
        else
            AudioRadio.IsChecked = true;

        // Network settings
        NetworkHostTextBox.Text = settings.NetworkTriggerHost;
        if (settings.NetworkTriggerPort > 0)
            NetworkPortTextBox.Text = settings.NetworkTriggerPort.ToString();

        // Tone sliders
        FrequencySlider.Value = settings.ToneFrequencyHz;
        NoiseDecaySlider.Value = settings.ToneNoiseDecay;
        ToneDecaySlider.Value = settings.ToneToneDecay;
        ToneMixSlider.Value = settings.ToneMix;
        DurationSlider.Value = settings.ToneDurationMs;
    }

    private void UpdateStatus()
    {
        if (_orchestrator.IsRunning)
            StatusText.Text = $"Monitoring: {_orchestrator.Settings.ShotsDirectoryPath}";
        else
            StatusText.Text = "Not monitoring";

        if (_orchestrator.ShotCount > 0)
            ShotCountText.Text = $"Shots detected: {_orchestrator.ShotCount}" +
                (_orchestrator.LastShotTime.HasValue ? $" | Last: {_orchestrator.LastShotTime:HH:mm:ss}" : "");
        else
            ShotCountText.Text = "";
    }

    private void ToneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSliderDisplayValues();
    }

    private void UpdateSliderDisplayValues()
    {
        if (!_isInitialized) return;

        FrequencyValue.Text = $"{FrequencySlider.Value:0} Hz";
        NoiseDecayValue.Text = $"{NoiseDecaySlider.Value:0}";
        ToneDecayValue.Text = $"{ToneDecaySlider.Value:0}";
        ToneMixValue.Text = $"{ToneMixSlider.Value:0.00}";
        DurationValue.Text = $"{DurationSlider.Value:0} ms";
    }

    private void TriggerType_Changed(object sender, RoutedEventArgs e)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (AudioConfigGroup == null || NetworkConfigGroup == null)
            return;

        if (AudioRadio.IsChecked == true)
        {
            AudioConfigGroup.Visibility = Visibility.Visible;
            NetworkConfigGroup.Visibility = Visibility.Collapsed;
        }
        else
        {
            AudioConfigGroup.Visibility = Visibility.Collapsed;
            NetworkConfigGroup.Visibility = Visibility.Visible;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the ProTee Shots directory",
            UseDescriptionForTitle = true,
            SelectedPath = ShotsDirectoryTextBox.Text
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ShotsDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private void TestAudioButton_Click(object sender, RoutedEventArgs e)
    {
        if (AudioDeviceCombo.SelectedItem is AudioDeviceInfo device)
        {
            _orchestrator.AudioTrigger.TestTone(device.Index,
                FrequencySlider.Value,
                NoiseDecaySlider.Value,
                ToneDecaySlider.Value,
                ToneMixSlider.Value,
                DurationSlider.Value);
        }
        else
        {
            MessageDialog.Show(this, "Test", "Please select an audio device first.", MessageDialogType.Warning);
        }
    }

    private void TestNetworkButton_Click(object sender, RoutedEventArgs e)
    {
        var host = NetworkHostTextBox.Text.Trim();
        if (string.IsNullOrEmpty(host)) host = "127.0.0.1";

        if (!int.TryParse(NetworkPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
        {
            MessageDialog.Show(this, "Test", "Please enter a valid port number (1-65535).", MessageDialogType.Warning);
            return;
        }

        _orchestrator.NetworkTrigger.TestPacket(port, host);
        MessageDialog.Show(this, "Test", $"Test packet sent to {host}:{port}", MessageDialogType.Information);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = _orchestrator.Settings;

        // Validate shots directory
        var shotsDir = ShotsDirectoryTextBox.Text.Trim();
        if (string.IsNullOrEmpty(shotsDir))
        {
            MessageDialog.Show(this, "Validation Error", "Please enter a shots directory path.", MessageDialogType.Warning);
            return;
        }
        settings.ShotsDirectoryPath = shotsDir;

        if (AudioRadio.IsChecked == true)
        {
            if (AudioDeviceCombo.SelectedItem is not AudioDeviceInfo device)
            {
                MessageDialog.Show(this, "Validation Error", "Please select an audio output device.", MessageDialogType.Warning);
                return;
            }

            settings.AudioTriggerEnabled = true;
            settings.NetworkTriggerEnabled = false;
            settings.SelectedDeviceIndex = device.Index;
            settings.SelectedDeviceName = device.Name;
            settings.ToneFrequencyHz = FrequencySlider.Value;
            settings.ToneNoiseDecay = NoiseDecaySlider.Value;
            settings.ToneToneDecay = ToneDecaySlider.Value;
            settings.ToneMix = ToneMixSlider.Value;
            settings.ToneDurationMs = DurationSlider.Value;
        }
        else
        {
            var host = NetworkHostTextBox.Text.Trim();
            if (string.IsNullOrEmpty(host)) host = "127.0.0.1";

            if (!int.TryParse(NetworkPortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageDialog.Show(this, "Validation Error", "Please enter a valid network port (1-65535).", MessageDialogType.Warning);
                return;
            }

            settings.NetworkTriggerEnabled = true;
            settings.AudioTriggerEnabled = false;
            settings.NetworkTriggerPort = port;
            settings.NetworkTriggerHost = host;
        }

        _orchestrator.SaveSettings();

        // Restart monitoring with new settings
        _orchestrator.Stop();
        _orchestrator.Start();

        UpdateStatus();
        MessageDialog.Show(this, "Saved", "Settings saved. Monitoring restarted with new configuration.", MessageDialogType.Information);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
