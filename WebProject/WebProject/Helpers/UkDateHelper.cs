namespace WebProject.Helpers
{
    public static class UkDateHelper
    {
        public static DateTime Today()
        {
            var ukTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Europe/London");

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                ukTimeZone).Date;
        }
    }
}
