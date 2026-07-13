namespace KiaiControl.Core.Common;

public static class CacheTime
{
    public static TimeSpan OneMinute => TimeSpan.FromMinutes(1); 
    public static TimeSpan FiveMinutes => TimeSpan.FromMinutes(5); 
    public static TimeSpan FifteenMinutes => TimeSpan.FromMinutes(15);
    public static TimeSpan ThirtyMinutes => TimeSpan.FromMinutes(30); 
    public static TimeSpan TwentyFiveMinutes => TimeSpan.FromMinutes(25); 
    public static TimeSpan OneHours => TimeSpan.FromHours(1); 
    public static TimeSpan OneDay => TimeSpan.FromDays(1);
}
