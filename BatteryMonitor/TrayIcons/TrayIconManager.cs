using BatteryMonitor.Fetchers;

namespace BatteryMonitor.TrayIcons;

public class TrayIconManager : IDisposable
{
    private readonly Dictionary<string, BatteryLevelFetcher> devices;
    private readonly Dictionary<string, NotifyIcon> icons = new();
    private readonly System.Windows.Forms.Timer updateTimer;

    public TrayIconManager(Dictionary<string, BatteryLevelFetcher> devices)
    {
        this.devices = devices;

        // Create one tray icon per device
        foreach (var kvp in devices)
        {
            string name = kvp.Key;
            BatteryLevelFetcher fetcher = kvp.Value;

            NotifyIcon icon = new NotifyIcon
            {
                Icon = CreateTextIcon(name, fetcher.BatteryPercentage),
                Text = $"{name} - {fetcher.BatteryPercentage}%",
                Visible = true
            };

            // Tooltip shows name on hover
            icon.MouseMove += (s, e) =>
            {
                icon.Text = $"{name} - {fetcher.BatteryPercentage}%";
            };

            // Optional right-click context menu
            ContextMenuStrip menu = new();
            menu.Items.Add("Exit", null, (s, e) => Application.Exit());
            icon.ContextMenuStrip = menu;

            icons[name] = icon;
        }

        // Update icons every 30 seconds
        updateTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        updateTimer.Tick += (s, e) => UpdateIcons();
        updateTimer.Start();
    }

    private void UpdateIcons()
    {
        foreach (var kvp in devices)
        {
            string name = kvp.Key;
            BatteryLevelFetcher fetcher = kvp.Value;
            if (!icons.TryGetValue(name, out var icon)) continue;

            icon.Icon = CreateTextIcon(name, fetcher.BatteryPercentage);
            icon.Text = $"{name} - {fetcher.BatteryPercentage}%";
        }
    }

    private Icon CreateTextIcon(string name, double level)
    {
        // Shorten name to 2–3 letters
        string shortName = name.Length > 1 ? name[..1].ToUpper() : name.ToUpper();
        string text = shortName;

        // Background color by battery
        Color textColor = level switch
        {
            > 50 => Color.FromArgb(0, 180, 0),      // green
            > 25 => Color.FromArgb(230, 140, 0),    // orange
            _ => Color.FromArgb(200, 0, 0)          // red
        };

        using Bitmap bmp = new(16, 16);
        using Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        using var font = new Font("Segoe UI", 12, FontStyle.Bold);
        using var brush = new SolidBrush(textColor);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Split text lines for better centering
        string[] lines = text.Split('\n');
        float lineHeight = 16;
        float startY = (bmp.Height - lineHeight * lines.Length) / 2;

        for (int i = 0; i < lines.Length; i++)
        {
            g.DrawString(lines[i], font, brush,
                new RectangleF(0, startY + i * lineHeight, bmp.Width, lineHeight),
                sf);
        }

        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        updateTimer?.Stop();
        foreach (var icon in icons.Values)
        {
            icon.Visible = false;
            icon.Dispose();
        }
        updateTimer?.Dispose();
    }
}
