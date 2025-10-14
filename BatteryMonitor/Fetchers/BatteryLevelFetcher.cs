namespace BatteryMonitor.Fetchers;

public abstract class BatteryLevelFetcher
{
    public abstract bool IsActive { get; }
    public abstract int BatteryPercentage { get; }
    public virtual void StartedCharging() { }
}
