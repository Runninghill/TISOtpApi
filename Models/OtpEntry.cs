namespace TISOtpApi.Models
{
    public class OtpEntry
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int FailedAttempts { get; set; } = 0;
        public bool IsUsed { get; set; } = false;
    }
}
