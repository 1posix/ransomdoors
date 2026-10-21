using System.IO;
using System.Windows;
using System.Windows.Threading;
using WpfApplication = System.Windows.Application;

namespace rans0m;

public partial class RansomNotification : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    public RansomNotification()
    {
        InitializeComponent();
        if (Config.CoinPlacement == CoinPlacementMode.RealUserFolders)
        {
            // Let the participant search the real Desktop/Documents/etc. themselves.
            // No in-app coin index is displayed or made available in this mode.
            btn_browse.Visibility = Visibility.Collapsed;
            txt_howto.Text = "Cherche les pièces d'or dans ton Bureau, Documents, Téléchargements, Images, Musique et Vidéos, puis glisse-les ici pour payer avant la fin du temps !";
            txt_footer.Text = "EXERCICE DE SENSIBILISATION — Aucun fichier personnel n'est chiffré. Explore les dossiers Windows et glisse les pièces PNG sur cette fenêtre. Ctrl+Maj+F12 pour arrêter.";
        }
        _timer.Tick += (_, _) =>
        {
            if (!Global.underRansom) { _timer.Stop(); return; }
            Global.ransomTimeLeft = Math.Max(0, Global.ransomTimeLeft - 1);
            UpdateCounters();
            Global.overlayWindow?.UpdateMusicForTime();
            if (Global.ransomTimeLeft <= 0) Global.overlayWindow?.Finish(false);
        };
        Closed += (_, _) => _timer.Stop();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateCounters();
        _timer.Start();
    }

    public void UpdateCounters()
    {
        txt_ransomLeft.Text = Global.ransomLeft.ToString();
        txt_timer.Text = $"TIME: {Global.ransomTimeLeft / 60:D2}:{Global.ransomTimeLeft % 60:D2}";
    }

    private void Browse_Click(object sender, RoutedEventArgs e) => Global.overlayWindow?.ShowBrowser();
    private void Quit_Click(object sender, RoutedEventArgs e) => WpfApplication.Current.Shutdown();
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (Global.underRansom && Global.overlayWindow?.IsFinishing != true)
            WpfApplication.Current.Shutdown();
    }
    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }
    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        foreach (string file in files) Global.overlayWindow?.Redeem(file);
    }
}
