using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Models.Requests
{
    public record SetPinRequest
    {
        //[StringLength(4, MinimumLength = 4, ErrorMessage = "The pin must be four characters")]
        [Length(4,4)]
        public string Pin { get; set; }

        public Guid UserId { get; set; }
    }
}
