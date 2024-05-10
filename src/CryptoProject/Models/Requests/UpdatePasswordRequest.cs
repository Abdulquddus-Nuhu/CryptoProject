namespace CryptoProject.Models.Requests
{
    public record UpdatePasswordRequest
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
    }
}
