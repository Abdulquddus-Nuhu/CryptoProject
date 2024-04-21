namespace CryptoProject.Models.Requests
{
    public record EditAccessCodeRequest
    {
        public string NewAccessCode { get; init; }
    }
}
