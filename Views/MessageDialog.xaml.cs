using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ShotTrigger.Views;

public enum MessageDialogType
{
    Information,
    Warning,
    Error
}

public partial class MessageDialog : Window
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MessageDialog(string title, string message, MessageDialogType type = MessageDialogType.Information)
    {
        InitializeComponent();

        SourceInitialized += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int value = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        };

        Title = title;
        HeaderText.Text = title;
        MessageText.Text = message;

        switch (type)
        {
            case MessageDialogType.Warning:
                HeaderIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.AlertCircle;
                HeaderIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                break;
            case MessageDialogType.Error:
                HeaderIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircle;
                HeaderIcon.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                break;
            case MessageDialogType.Information:
            default:
                HeaderIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.InformationCircle;
                HeaderIcon.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                break;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public static void Show(Window owner, string title, string message, MessageDialogType type = MessageDialogType.Information)
    {
        var dialog = new MessageDialog(title, message, type)
        {
            Owner = owner
        };
        dialog.ShowDialog();
    }
}
