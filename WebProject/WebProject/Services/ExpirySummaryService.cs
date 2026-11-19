using System.Globalization;
using WebProject.Helpers;

namespace WebProject.Services
{
    public class ExpirySummary
    {
        public int Total { get; set; }
        public int Days30Plus { get; set; }
        public int Days7To29 { get; set; }
        public int Under7Days { get; set; }
        public int Expired { get; set; }
    }

    public static class ExpirySummaryService
    {
        public static ExpirySummary Calculate(
            IEnumerable<string> expiryDates)
        {
            var summary = new ExpirySummary();
            DateTime today = UkDateHelper.Today();

            foreach (string expiryDate in expiryDates)
            {
                summary.Total++;

                if (!DateTime.TryParseExact(
                    expiryDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime date))
                {
                    continue;
                }

                int daysRemaining = (date.Date - today).Days;

                if (daysRemaining < 0)
                    summary.Expired++;
                else if (daysRemaining < 7)
                    summary.Under7Days++;
                else if (daysRemaining < 30)
                    summary.Days7To29++;
                else
                    summary.Days30Plus++;
            }

            return summary;
        }

        public static ExpirySummary Combine(
            IEnumerable<ExpirySummary> summaries)
        {
            var result = new ExpirySummary();

            foreach (var summary in summaries)
            {
                result.Total += summary.Total;
                result.Days30Plus += summary.Days30Plus;
                result.Days7To29 += summary.Days7To29;
                result.Under7Days += summary.Under7Days;
                result.Expired += summary.Expired;
            }

            return result;
        }
    }
}
