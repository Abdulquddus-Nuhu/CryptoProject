namespace CryptoProject.Models.Responses
{
    public record BaseResponse
    {
        public int Code { get; set; } = 0;
        public bool Status { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}
