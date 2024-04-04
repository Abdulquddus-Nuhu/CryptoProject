using CryptoProject.Data;
using CryptoProject.Entities;
using CryptoProject.Entities.Enums;
using CryptoProject.Models.Requests;
using CryptoProject.Models.Responses;
using CryptoProject.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace CryptoProject.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        public WalletController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

       //[Authorize(Roles = nameof(RoleType.Admin) + ", " + nameof(RoleType.User))]
        [HttpGet("get-balance")]
        public async Task<ActionResult> GetWalletBalance(Guid userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                return BadRequest("User not found");
            }

            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == userId);
            if (wallet is null)
            {
                return BadRequest("Wallet not found");
            }

            return Ok(new { Balance = wallet.Balance });
        }
        
        //[Authorize(Roles = nameof(RoleType.Admin) + ", " + nameof(RoleType.User))]
        [HttpPost("set-pin")]
        public async Task<IActionResult> SetOrUpdatePin([FromBody] SetPinRequest request)
        {
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest("User not found");
            }

            if (string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest("Ivalid or empty pin");
            }

            user.Pin = request.Pin;


            var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
            Guid.TryParse(userIdString?.Value, out Guid userIdGuid);
            if (userIdGuid == Guid.Empty)
            {
                //todo:
            }

            var logEntry = ActivityLogService.CreateLogEntry(userIdGuid, userEmail: User.Identity.Name, ActivityType.UserSetPin, $"User with email {user.Email} sets a new pin");
            _dbContext.ActivityLogs.Add(logEntry);

            if (await _dbContext.TrySaveChangesAsync())
            {
                return Ok();
            }
            else
            {
                return StatusCode(500, "Unable to update record please try again or contact administrator");
            }
        }

        //[Authorize(Roles = nameof(RoleType.User))]
        [HttpPost("transfer")]
        public async Task<ActionResult> CreateTransaction(TransactionRequest request)
        {

            var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
            Guid.TryParse(userIdString?.Value, out Guid userIdGuid);


            if (userIdGuid == Guid.Empty)
            {
                return BadRequest();
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (user is null)
            {
                return BadRequest("Sender account not found");
            }
            
            var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userIdGuid);
            if (receiver is null)
            {
                return BadRequest("Receiver account not found");
            }

            var transaction = new Transaction()
            {
                Amount = request.Amount,
                SenderId = userIdGuid,
                ReceiverId = request.ReceiverId,
                //Status = TransactionStatus.Pending,
                Status = TransactionStatus.Successful,
                Type = TransactionType.Transfer,
                Timestamp = DateTime.UtcNow
            };
            await _dbContext.Transactions.AddAsync(transaction);


            var senderWallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == userIdGuid);
            if (senderWallet is null)
            {
                return BadRequest("Sender's Wallet not found");
            }
            
            var receiverWallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == request.ReceiverId);
            if (receiverWallet is null)
            {
                return BadRequest("Receiver's Wallet not found");
            }

            if (senderWallet.Balance < request.Amount)
            {
                return BadRequest("Insufficient balance!");
            }

            senderWallet.Balance -= request.Amount;
            receiverWallet.Balance += request.Amount;

            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok();
            }
            else
            {
                return StatusCode(500, "Unable to process transfer please try again later or contact administrator");
            }


        }


        //[Authorize(Roles = nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            List<Transaction> transactions = new List<Transaction>();
            IEnumerable<TransactionResponse> response = new List<TransactionResponse>();


            var user = User.Identity!.Name ?? string.Empty;
            var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
            Guid.TryParse(userIdString?.Value, out Guid userIdGuid);


            if (userIdGuid == Guid.Empty)
            {
                return Ok(response);
            }


            transactions = await _dbContext.Transactions
                .Include(t => t.Sender)
                .Include(t => t.Receiver)
                .Where(t => t.SenderId == userIdGuid)
                .ToListAsync();

            response = transactions.Select(t => new TransactionResponse()
            {
                Amount = t.Amount,
                Sender = t.Sender.FullName,
                SenderId = t.SenderId,
                SenderEmail = t.Sender.Email,
                Receiver = t.Receiver.FullName,
                ReceiverId = t.ReceiverId,
                ReceiverEmail = t.Receiver.Email,
                Status = t.Status.ToString(),
                Type = t.Type.ToString(),
                Timestamp = t.Timestamp,
            });

            return Ok(response);
        }

    }
}
