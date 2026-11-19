using WebProject.Services;

namespace WebProject.Models
{
    public class ClientSummary
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;

        public ExpirySummary Warranties { get; set; } = new();
        public ExpirySummary Licenses { get; set; } = new();
    }
}
