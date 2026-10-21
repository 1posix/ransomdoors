using System.IO;
using System.Windows;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace rans0m;

internal static class Global
{
    public static Overlay? overlayWindow;
    public static int ransomLeft;
    public static int ransomTimeLeft;
    public static bool underRansom;
    public static readonly Random rng = new();

    public static Stream GetResourceSteam(string path)
    {
        return WpfApplication.GetResourceStream(new Uri($"pack://application:,,,/Assets/{path}"))?.Stream
            ?? throw new FileNotFoundException("Embedded asset was not found", path);
    }

    public static Thickness CombineThickness(Thickness a, Thickness b) =>
        new(a.Left + b.Left, a.Top + b.Top, a.Right + b.Right, a.Bottom + b.Bottom);

    public static void CenterControl(FrameworkElement control)
    {
        var overlay = overlayWindow;
        double width = overlay?.ActualWidth > 0 ? overlay.ActualWidth : SystemParameters.PrimaryScreenWidth;
        double height = overlay?.ActualHeight > 0 ? overlay.ActualHeight : SystemParameters.PrimaryScreenHeight;
        double ew = control.ActualWidth > 0 ? control.ActualWidth : control.Width;
        double eh = control.ActualHeight > 0 ? control.ActualHeight : control.Height;
        control.Margin = new Thickness(Math.Max(0, (width - ew) / 2), Math.Max(0, (height - eh) / 2), 0, 0);
    }
}
