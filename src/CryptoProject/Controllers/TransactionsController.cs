using CryptoProject.Data;
using CryptoProject.Entities;
using CryptoProject.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace CryptoProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        public TransactionsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        //[Produces(MediaTypeNames.Application.Json)]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[HttpPost("create")]
        //public IActionResult CreateTransaction([FromBody] string transactionDto)
        //{
        //    // Simplify: Assume transaction is created successfully
        //    return CreatedAtAction(nameof(CreateTransaction), new { TransactionId = 1 /* Dummy transaction ID */ });
        //}


        //[Produces(MediaTypeNames.Application.Json)]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[HttpPost("{transactionId}/revert")]
        //public IActionResult RevertTransaction(Guid transactionId)
        //{
        //    // Simplify: Assume transaction is reverted successfully
        //    return NoContent();
        //}


        //[Produces(MediaTypeNames.Application.Json)]
        //[ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[HttpGet()]
        //public async Task<IActionResult> GetAllTransactions()
        //{
        //    List<Transaction> transactions = new List<Transaction>();
        //    IEnumerable<TransactionResponse> response = new List<TransactionResponse>();


        //    var user = User.Identity!.Name ?? string.Empty;
        //    var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
        //    Guid.TryParse(userIdString?.Value, out Guid userIdGuid);


        //    if (userIdGuid == Guid.Empty)
        //    {
        //        return Ok(response);
        //    }


        //    transactions = await _dbContext.Transactions
        //        .Include(t => t.Sender)
        //        .Include(t => t.Receiver)
        //        .Where(t => t.SenderId == userIdGuid)
        //        .ToListAsync();

        //    response = transactions.Select(t => new TransactionResponse()
        //    {
        //        Amount = t.Amount,
        //        Sender = t.Sender.FullName,
        //        SenderId = t.SenderId,
        //        SenderEmail = t.Sender.Email,
        //        Receiver = t.Receiver.FullName,
        //        ReceiverId = t.ReceiverId,
        //        ReceiverEmail = t.Receiver.Email,
        //        Status = t.Status.ToString(),
        //        Type = t.Type.ToString(),
        //        Timestamp = t.Timestamp,
        //    });

        //    return Ok(response);
        //}

    }
}
