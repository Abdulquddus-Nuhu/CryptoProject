using CryptoProject.Data;
using CryptoProject.Entities.Enums;
using CryptoProject.Entities.Identity;
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
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    //[Authorize(Roles = nameof(RoleType.Admin))]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AdminController> _logger;
        private readonly string _cryptoWalletKey = Environment.GetEnvironmentVariable("Crptocurrency_API_KEY") ?? string.Empty;
        public AdminController(AppDbContext dbContext, ILogger<AdminController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<PersonaResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _dbContext.Users.Where(x => x.Role == RoleType.User)
                .Include(x => x.Wallet)
                .Include(x => x.LedgerAccount)
                .Include(x => x.USDAccount)
                .OrderBy(x => x.Created)
                .ToListAsync();

            //add pagination searching and filtering to this endpoint
            var response = users.Select(x => new UserResponse()
            {
                Email = x.Email,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PhoneNumber = x.PhoneNumber,
                UserName = x.UserName,
                MiddleName = x.MiddleName,
                Id = x.Id,
                AccountType = x.AccountType.ToString(),
                State = x.State,
                Role = x.Role.ToString(),
                Address = x.Address,
                City = x.City,
                WalletId = x.WalletId,
                WalletBalance = x.Wallet?.Balance,
                LedgerAccountId = x.LedgerAccountId,
                LedgerAccountBalance = x.LedgerAccount.Balance,
                USDAccountId = x.USDAccountId,
                USDAccountBalance = x.USDAccount.Balance,
                CryptoKey = _cryptoWalletKey
            });

            return Ok(response);
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserDetails(Guid userId)
        {
            var user = await _dbContext.Users
                .Include(x => x.Wallet)
                .Include(x => x.LedgerAccount)
                .Include(x => x.USDAccount)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }

            var response = new UserResponse()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                MiddleName = user.MiddleName,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                AccountType = user.AccountType.ToString(),
                Address = user.Address,
                City = user.City,
                State = user.State,
                WalletId = user.WalletId,
                WalletBalance = user.Wallet?.Balance,
                LedgerAccountId = user.LedgerAccountId,
                LedgerAccountBalance = user.LedgerAccount.Balance,
                USDAccountId = user.USDAccountId,
                USDAccountBalance = user.USDAccount.Balance,
                CryptoKey = _cryptoWalletKey
            };

            return Ok(response);
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("activities")]
        public async Task<IActionResult> GetAllActivities()
        {
            var logs = await _dbContext.ActivityLogs.ToListAsync();
            var response = logs.Select(x => new ActivityLogResponse()
            {

                UserEmail = x.UserEmail,
                UserId = x.UserId,
                Timestamp = x.Timestamp,
                Details = x.Details,
                ActivityType = x.ActivityType.ToString(),
            });

            return Ok(response);
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            var transactions = await _dbContext.Transactions.Include(t => t.Sender).Include(t => t.Receiver).ToListAsync();
            var response = transactions.Select(t => new TransactionResponse()
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


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("revert/{transactionId}")]
        public async Task<IActionResult> RevertTransaction(Guid transactionId)
        {
            var transaction = _dbContext.Transactions.FirstOrDefault(t => t.Id == transactionId);
            if (transaction is null)
            {
                return BadRequest("Transaction not found");
            }


            var receiverWallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == transaction.ReceiverId);
            if (receiverWallet is null)
            {
                return BadRequest("Receiver's Wallet not found");
            }

            var senderWallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == transaction.SenderId);
            if (senderWallet is null)
            {
                return BadRequest("Sender's Wallet not found");
            }

            //receiverWallet.Balance -= transaction.Amount;
            //senderWallet.Balance += transaction.Amount;
            receiverWallet.Balance += transaction.Amount;
            senderWallet.Balance -= transaction.Amount;
            transaction.Status = Entities.Enums.TransactionStatus.Reverted;

            _dbContext.Transactions.Update(transaction);
            _dbContext.Wallets.Update(senderWallet);
            _dbContext.Wallets.Update(receiverWallet);

            var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
            Guid.TryParse(userIdString?.Value, out Guid userIdGuid);
            if (userIdGuid == Guid.Empty)
            {
                //todo:
            }

            var logEntry = ActivityLogService.CreateLogEntry(userIdGuid, userEmail: User.Identity.Name, ActivityType.WalletTransferReverted, transaction.Amount);
            _dbContext.ActivityLogs.Add(logEntry);

            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                _logger.LogInformation("Admin reverted a user transaction with id: {0}", transaction.Id);
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("500 - Unable to rervert transaction with id: {0}", transaction.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to rervert transaction! Please try again or contact administrator" });
                //return StatusCode(500, "Unable to rervert transaction! Please try again or contact administrator");
            }
        }


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPost("credit-wallet")]
        public async Task<IActionResult> CreditUser([FromBody] CreditRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }
            var wallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == request.UserId);
            if (wallet is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User wallet not found", Code = 400 });
            }
            wallet.Balance += request.Amount;
            _dbContext.Wallets.Update(wallet);
            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.WalletFundsAdded, request.Amount);
            _dbContext.ActivityLogs.Add(logEntry);
            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("Unable to credit user's wallet! Please try again or contact administrator");
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to credit user's wallet! Please try again or contact administrator" });
            }
        }


        //write endpoint to debit amount from a user account by admin
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPost("debit-wallet")]
        public async Task<IActionResult> DebitUser([FromBody] DebitRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }
            var wallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == request.UserId);
            if (wallet is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User wallet not found", Code = 400 });
            }
            if (wallet.Balance < request.Amount)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "Insufficient funds", Code = 400 });
            }
            wallet.Balance -= request.Amount;
            _dbContext.Wallets.Update(wallet);
            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.WalletFundsDeducted, request.Amount);
            _dbContext.ActivityLogs.Add(logEntry);
            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("Unable to debit user's wallet! Please try again or contact administrator");
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to debit user's wallet! Please try again or contact administrator" });
            }
        }

        //write endpoint to credit user's USD account by admin        //write endpoint to credit user's USD account by admin
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPost("credit-usd")]
        public async Task<IActionResult> CreditUSD([FromBody] CreditRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }
            var usdAccount = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == request.UserId);
            if (usdAccount is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User USD account not found", Code = 400 });
            }
            usdAccount.Balance += request.Amount;
            _dbContext.USDAccounts.Update(usdAccount);

            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.USDFundsAdded, request.Amount);
            _dbContext.ActivityLogs.Add(logEntry);
            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("Unable to credit user's USD account! Please try again or contact administrator");
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to credit user's USD account! Please try again or contact administrator" });
            }
        }
        //write endpoint to debit amount from a user account by admin
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPost("debit-usd")]
        public async Task<IActionResult> DebitUSD([FromBody] DebitRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }
            var usdAccount = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == request.UserId);
            if (usdAccount is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User USD account not found", Code = 400 });
            }

            if (usdAccount.Balance < request.Amount)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "Insufficient funds", Code = 400 });
            }
            usdAccount.Balance -= request.Amount;
            _dbContext.USDAccounts.Update(usdAccount);
            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.USDFundsDeducted, request.Amount);
            _dbContext.ActivityLogs.Add(logEntry);
            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("Unable to debit user's USD account! Please try again or contact administrator");
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to debit user's USD account! Please try again or contact administrator" });
            }
        }

        //write e        //write endpoint to debit amount from a user ledger account by admin
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPost("debit-ledger")]
        public async Task<IActionResult> DebitLedger([FromBody] DebitRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }
            var ledgerAccount = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == request.UserId);
            if (ledgerAccount is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User ledger account not found", Code = 400 });
            }
            if (ledgerAccount.Balance < request.Amount)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "Insufficient funds", Code = 400 });
            }
            ledgerAccount.Balance -= request.Amount;
            _dbContext.LedgerAccounts.Update(ledgerAccount);
            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.AdminFundsAdjusted, request.Amount);
            _dbContext.ActivityLogs.Add(logEntry);
            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("Unable to debit user's ledger account! Please try again or contact administrator");
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to debit user's ledger account! Please try again or contact administrator" });
            }
        }

        //write endpoint to credit user's ledger account by admin        //write endpoint to credit user's ledger account by admin
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPost("credit-ledger")]
        public async Task<IActionResult> CreditLedger([FromBody] CreditRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }
            var ledgerAccount = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == request.UserId);
            if (ledgerAccount is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User ledger account not found", Code = 400 });
            }
            ledgerAccount.Balance += request.Amount;
            _dbContext.LedgerAccounts.Update(ledgerAccount);
            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.AdminFundsAdjusted, request.Amount);
            _dbContext.ActivityLogs.Add(logEntry);
            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                return Ok(new BaseResponse());
            }
            else
            {
                _logger.LogInformation("Unable to credit user's ledger account! Please try again or contact administrator");
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to credit user's ledger account! Please try again or contact administrator" });
            }
        }
    }
}
