using BatteryMonitor.Fetchers;
using System.Timers;

namespace BatteryMonitor.Warnings;

public sealed class OnDisabledWarning
{
    private bool isPopupShown = false;

    public OnDisabledWarning(BatteryLevelFetcher fetcher, int Threshold)
    {
        var timer = new System.Timers.Timer(10_000); // tick every second
        timer.Elapsed += (object? sender, ElapsedEventArgs e) =>
        {
            if (fetcher.BatteryPercentage < Threshold && !fetcher.IsActive && !isPopupShown)
            {
                ShowChargingPopup(fetcher.BatteryPercentage, () => fetcher.StartedCharging());
            }
        };
        timer.Start();
    }

    private void ShowChargingPopup(int percentage, Action onChargeButtonPressed)
    {
        isPopupShown = true;

        Form popup = new()
        {
            Width = 400,
            Height = 150,
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            TopMost = true
        };

        Label label = new()
        {
            Text = $"Microphone battery is low ({percentage}%). Click 'Charging' if it's charging.",
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Button chargingButton = new()
        {
            Text = "Charging",
            Dock = DockStyle.Bottom,
            Height = 40
        };

        chargingButton.Click += (s, e) =>
        {
            onChargeButtonPressed();
            popup.Close();
            isPopupShown = false;
        };

        popup.Controls.Add(label);
        popup.Controls.Add(chargingButton);
        popup.ShowDialog();
    }
}
