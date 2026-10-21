using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfApplication = System.Windows.Application;

namespace rans0m;

public partial class Overlay : Window
{
    private readonly Border[] _segments;
    private NotifyIcon? _tray;
    private CoinBrowser? _browser;
    private RansomNotification? _notice;
    private bool _finished;
    public bool IsFinishing => _finished;

    private SoundHandle? _spawn;
    private SoundHandle? _attack;
    private SoundHandle? _install;
    private SoundHandle? _cash;
    private SoundHandle? _layer1;
    private SoundHandle? _layer2;
    private SoundHandle? _layer3;
    private int _musicLayer;

    private HwndSource? _source;
    private bool _hotkeyRegistered;
    private const int HotkeyId = 0xA901;
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    public Overlay()
    {
        InitializeComponent();
        Global.overlayWindow = this;
        _segments = new[] { Seg0, Seg1, Seg2, Seg3, Seg4, Seg5, Seg6, Seg7, Seg8, Seg9 };
        Closing += (_, _) => Cleanup();
    }

    private void MakeClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(hwnd, -20);
        SetWindowLong(hwnd, -20, style | 0x00000020 | 0x00080000);
    }

    private void SetupHotkey()
    {
        _source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _source?.AddHook(WndProc);
        // Ctrl+Shift+F12 always exits the simulation while it is running.
        _hotkeyRegistered = RegisterHotKey(new WindowInteropHelper(this).Handle, HotkeyId, 0x0006, 0x7B);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            WpfApplication.Current.Shutdown();
        }
        return IntPtr.Zero;
    }

    private void SetupTray()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Quitter la simulation").Click += (_, _) => Dispatcher.Invoke(() => WpfApplication.Current.Shutdown());
        _tray = new NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application,
            Text = "RANS0M Lab — Simulation",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private static SoundHandle? Sound(string asset)
    {
        try { return SoundHandle.Create(Global.GetResourceSteam("Sounds/" + asset)); }
        catch { return null; }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        MakeClickThrough();
        SetupTray();
        SetupHotkey();
        _spawn = Sound("spawn.wav");
        _attack = Sound("attack.wav");
        _install = Sound("install.wav");
        _cash = Sound("cash.wav");
        _layer1 = Sound("layer1.wav");
        _layer2 = Sound("layer2.wav");
        _layer3 = Sound("layer3.wav");
        ResetVisuals();
        try { await StartGame(); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Erreur de simulation", MessageBoxButton.OK, MessageBoxImage.Error);
            WpfApplication.Current.Shutdown();
        }
    }

    private void ResetVisuals()
    {
        redVignette.Opacity = 0;
        staticBg.Opacity = 0;
        img_idle.Opacity = 0;
        img_attack.Opacity = 0;
        img_stopsign.Opacity = 0;
        vb_download.Opacity = 0;
        Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    private async Task StartGame()
    {
        // Read the bundled image on the UI thread and only pass its bytes to the worker.
        // In RealUserFolders mode, coins are harmless PNG thumbnails, not executable shortcuts.
        byte[]? coinImage = null;
        if (Config.CoinPlacement == CoinPlacementMode.RealUserFolders)
        {
            using Stream source = Global.GetResourceSteam("Gold.png");
            using var buffer = new MemoryStream();
            source.CopyTo(buffer);
            coinImage = buffer.ToArray();
        }
        int generatedGold = await Task.Run(() => GoldCoinManager.GenerateCoins(coinImage));
        if (generatedGold < 1) throw new InvalidOperationException("Impossible de créer les pièces fictives");
        Global.ransomLeft = Math.Min(generatedGold, Config.RansomAmount);
        Global.ransomTimeLeft = Config.InfectionDuration;

        await DownloadJumpscare();
        if (_finished) return;
        WallpaperUpdater.SetDarkRedWallpaper();
        Global.underRansom = true;
        _notice = new RansomNotification();
        _notice.Show();
        // RealUserFolders: let the participant discover PNG coins in Windows Explorer.
        // Do not open the simulated index of every coin behind the ransom window.
        if (Config.CoinPlacement == CoinPlacementMode.Simulation)
            ShowBrowser();
        _layer1?.PlayLooping();
        _musicLayer = 1;
    }

    private async Task ShakeAsync(Thickness center)
    {
        for (int i = 0; i < 24 && !_finished; i++)
        {
            img_attack.Margin = Global.CombineThickness(center,
                new Thickness(Global.rng.Next(-18, 18), Global.rng.Next(-18, 18), 0, 0));
            await Task.Delay(25);
        }
    }

    private async Task DownloadJumpscare()
    {
        _spawn?.Play();
        Global.CenterControl(img_idle);
        img_idle.Opacity = 1;
        await Task.Delay(500);
        img_idle.Opacity = 0;
        _attack?.Play();
        Background = new SolidColorBrush(Color.FromRgb(100, 0, 0));
        staticBg.Opacity = .08;
        Global.CenterControl(img_attack);
        Thickness center = img_attack.Margin;
        img_attack.Opacity = 1;
        await ShakeAsync(center);
        img_attack.Opacity = 0;
        _install?.Play();
        vb_download.Opacity = 1;
        foreach (var segment in _segments)
        {
            if (_finished) return;
            segment.Background = new SolidColorBrush(Color.FromRgb(200, 0, 0));
            await Task.Delay(90);
        }
        await Task.Delay(400);
        ResetVisuals();
    }

    public void ShowBrowser()
    {
        // The integrated coin index is intentionally disabled for real-user folders.
        // Prevent it from being reopened via another UI event in this mode.
        if (_finished || Config.CoinPlacement == CoinPlacementMode.RealUserFolders) return;
        if (_browser?.IsVisible == true) { _browser.Activate(); return; }
        _browser = new CoinBrowser();
        _browser.Show();
    }

    public void UpdateMusicForTime()
    {
        if (_finished) return;
        int elapsed = Config.InfectionDuration - Global.ransomTimeLeft;
        if (_musicLayer == 1 && elapsed >= Config.InfectionDuration / 2)
        {
            _layer1?.Pause(); _layer2?.PlayLooping(); _musicLayer = 2;
        }
        if (_musicLayer == 2 && elapsed >= Config.InfectionDuration * 3 / 4)
        {
            _layer2?.Pause(); _layer3?.PlayLooping(); _musicLayer = 3;
        }
    }

    public void Redeem(string file)
    {
        if (_finished || !Global.underRansom) return;
        if (!GoldCoinManager.TryRedeem(file, out int value)) return;
        Global.ransomLeft = Math.Max(0, Global.ransomLeft - value);
        _cash?.Reset(); _cash?.Play();
        _notice?.UpdateCounters();
        _browser?.RefreshCoins();
        if (Global.ransomLeft == 0) Finish(true);
    }

    public void Finish(bool success)
    {
        if (_finished) return;
        _finished = true;
        LabDiagnostics.Write($"Finishing game (success={success})");
        Global.underRansom = false;
        Cleanup();
        _notice?.Close();
        _browser?.Close();
        _notice = null;
        _browser = null;
        var end = new EndScreen(success);
        // The old overlay stays hidden until the person closes this result window.
        // Explicitly hand over MainWindow so closing game windows cannot dismiss the result.
        WpfApplication.Current.MainWindow = end;
        end.Show();
        LabDiagnostics.Write("Result window shown");
        Hide();
        // Bring the result into the foreground only once; normal mouse movements
        // and focus changes must never hide, close or replace it.
        end.Activate();
    }

    private void Cleanup()
    {
        // Idempotent: called on success, failure, closing, and application exit.
        _layer1?.Stop(); _layer2?.Stop(); _layer3?.Stop();
        _spawn?.Stop(); _attack?.Stop(); _install?.Stop(); _cash?.Stop();
        WallpaperUpdater.RestoreWallpaper();
        GoldCoinManager.DeleteAllCoins();
        if (_source != null)
        {
            if (_hotkeyRegistered) UnregisterHotKey(new WindowInteropHelper(this).Handle, HotkeyId);
            _source.RemoveHook(WndProc); _source = null;
        }
        _tray?.Dispose(); _tray = null;
    }
}
