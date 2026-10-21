using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace rans0m;

public partial class CoinBrowser : Window
{
    private readonly BitmapImage _coinImage = new(new Uri("pack://application:,,,/Assets/Gold.png"));
    public CoinBrowser()
    {
        InitializeComponent();
        txt_help.Text = Config.CoinPlacement == CoinPlacementMode.RealUserFolders
            ? "Pièces PNG dans les vrais dossiers Windows : double-clic ici, ou glisse une pièce depuis l'Explorateur Windows vers la fenêtre RANS0M. Ne double-clique pas sur le PNG dans Windows (il s'ouvrirait en image)."
            : "Développe les dossiers puis DOUBLE-CLIQUE sur une pièce d'or pour la récupérer. Tes vrais dossiers ne sont pas utilisés.";
        RefreshCoins();
    }

    public void RefreshCoins()
    {
        folders.Items.Clear();
        txt_remaining.Text = $"Pièces restantes à payer : {Global.ransomLeft}";
        string? root = GoldCoinManager.WorkspaceRoot;
        if (root == null || !Directory.Exists(root)) return;
        if (Config.CoinPlacement == CoinPlacementMode.RealUserFolders)
        {
            // Never enumerate unrelated personal files: use the coin manifest snapshot only.
            foreach (var group in GoldCoinManager.ListActiveCoins()
                         .GroupBy(path => Path.GetDirectoryName(path) ?? "")
                         .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var folder = new TreeViewItem
                {
                    Header = "📁 " + Path.GetFileName(group.Key),
                    ToolTip = group.Key,
                    IsExpanded = true
                };
                foreach (string path in group)
                    folder.Items.Add(BuildCoinItem(path));
                folders.Items.Add(folder);
            }
            return;
        }
        foreach (string directory in Directory.EnumerateDirectories(root))
            folders.Items.Add(BuildFolder(directory));
    }

    private TreeViewItem BuildFolder(string path)
    {
        var item = new TreeViewItem { Header = "📁 " + Path.GetFileName(path), IsExpanded = true };
        foreach (string dir in Directory.EnumerateDirectories(path))
            item.Items.Add(BuildFolder(dir));
        foreach (string file in Directory.EnumerateFiles(path, "*.gold?"))
            item.Items.Add(BuildCoinItem(file));
        return item;
    }

    private TreeViewItem BuildCoinItem(string file)
    {
        var content = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        content.Children.Add(new Image { Source = _coinImage, Width = 25, Height = 25 });
        content.Children.Add(new TextBlock { Text = "  🪙 " + Path.GetFileName(file), VerticalAlignment = VerticalAlignment.Center });
        return new TreeViewItem { Header = content, Tag = file };
    }

    private void Folders_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Don't redeem a parent folder when a child UI element was double-clicked.
        if (folders.SelectedItem is TreeViewItem node && node.Tag is string path)
        {
            Global.overlayWindow?.Redeem(path);
            e.Handled = true;
        }
    }
}
