namespace CryptoProject.Models.Responses
{
    public record ActivityLogResponse
    {
        public Guid? UserId { get; set; }
        public string? FullName { get; set; }

        public string? UserEmail { get; set; }
        public string ActivityType { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; } = string.Empty;
        public string? Data { get; set; }
    }
}
