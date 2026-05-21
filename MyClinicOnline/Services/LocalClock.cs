namespace MyClinicOnline.Services
{
    internal static class LocalClock
    {
        private static readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "FLE Standard Time" : "Europe/Sofia");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zone);
        public static DateTime Today => Now.Date;
    }
}
