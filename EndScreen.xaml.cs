using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WpfApplication = System.Windows.Application;

namespace rans0m;

public partial class EndScreen : Window
{
    private bool _exitRequested;

    public EndScreen(bool success)
    {
        InitializeComponent();
        txt_title.Text = success ? "ORDINATEUR SAUVÉ" : "SYSTEM COMPROMISED";
        txt_title.Foreground = new System.Windows.Media.SolidColorBrush(
            success ? System.Windows.Media.Color.FromRgb(98, 227, 147) : System.Windows.Media.Color.FromRgb(255, 100, 100));
        txt_message.Text = success ? Config.SuccessMessage : Config.EndMessage;
    }

    // Keep this window open independently of the old game windows and timers.
    // The sole voluntary exit is the clearly-labelled button below.
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _exitRequested = true;
        LabDiagnostics.Write("Result closed using button");
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            LabDiagnostics.Write("Unexpected result close request blocked");
            e.Cancel = true;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_Closed(object? sender, EventArgs e) => WpfApplication.Current.Shutdown();
}
