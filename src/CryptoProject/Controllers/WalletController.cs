using CryptoProject.Data;
using CryptoProject.Entities;
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
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Mime;

namespace CryptoProject.Controllers
{
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly OtpGenerator _otpGenerator;
        private readonly EmailService _emailService;
        private readonly ILogger<WalletController> _logger;
        public WalletController(AppDbContext dbContext, OtpGenerator otpGenerator, EmailService emailService,
            ILogger<WalletController> logger)
        {
            _dbContext = dbContext;
            _otpGenerator = otpGenerator;
            _emailService = emailService;
            _logger = logger;
        }

        //[Authorize(Roles = nameof(RoleType.Admin) + ", " + nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Get a user balance",
         Description = "WalletType:-   UsdAccount == 0, LedgerAccount == 1, WalleAccount == 2,")
        //OperationId = "auth.login",
        //Tags = new[] { "AuthEndpoints" })
        ]
        [HttpGet("get-balance")]
        public async Task<ActionResult> GetWalletBalance(GetBalanceRequest request)
        {
            var response = new BalanceResponse();

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest("User not found");
            }

            if (request.WalletType is WalletType.WalletAccount)
            {
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (wallet is null)
                {
                    return BadRequest("Wallet not found");
                }
                response.Balance = wallet.Balance;
            }
            else if (request.WalletType is WalletType.LedgerAccount)
            {
                var ledgerAccount = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (ledgerAccount is null)
                {
                    return BadRequest("Ledger-Account not found");
                }
                response.Balance = ledgerAccount.Balance;
            }
            else if (request.WalletType is WalletType.UsdAccount)
            {
                var uSDAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (uSDAccount is null)
                {
                    return BadRequest("USD-Account not found");
                }
                response.Balance = uSDAccount.Balance;

            }


            return Ok(response);
        }

        //[Authorize(Roles = nameof(RoleType.Admin) + ", " + nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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


            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserSetPin, $"User with email {user.Email} sets a new pin");
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
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("initiate-transfer")]
        public async Task<IActionResult> InitiateTransfer([FromBody] InitiateTransferRequest request)
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

            string otp = _otpGenerator.Generate(user.Email, 2, 6);
            _logger.LogInformation("User with id: {0} requested otp: {1}",request.UserId,otp);

            string subject = "Transfer OTP";
            string message = $"Your OTP is {otp} valid for 2 minutes";
            List<string> receivers = [user.Email];

            _emailService.SendEmail(receivers, subject, message, "abdulquddusnuhu@gmail.com");

            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserInitiateTransfer, $"User with email {user.Email} initiated a transfer");
            _dbContext.ActivityLogs.Add(logEntry);

            return Ok(otp);
        }
        //write endpoint to verify otp and complete transfer
        //[Authorize(Roles = nameof(RoleType.User))]
        //[Produces(MediaTypeNames.Application.Json)]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //[HttpPost("complete-transfer")]
        //public async Task<IActionResult> CompleteTransfer([FromBody] CompleteTransferRequest request)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return UnprocessableEntity(ModelState);
        //    }
        //    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        //    if (user is null)
        //    {
        //        return BadRequest("User not found");
        //    }
        //    if (!_otpGenerator.Verify(user.Email, request.Otp, 5, 6))
        //    {
        //        return BadRequest("Invalid OTP");
        //    }
        //    var transaction = new Transaction()
        //    {
        //        Amount = request.Amount,
        //        SenderId = request.UserId,
        //        ReceiverWalletAddress = request.ReceiverWalletAddress,
        //        Details = request.Details,
        //        Status = TransactionStatus.Successful,
        //        Type = TransactionType.Transfer,
        //        Timestamp = DateTime.UtcNow,
        //        WalletType = request.WalletType,
        //    };
        //    await _dbContext.Transactions.AddAsync(transaction);
        //    if (requestpoint to initiate transfer by geneating otp and sending it to a user email using elastic email service

        //[Authorize(Roles = nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(TransactionResponse),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("transfer")]
        public async Task<ActionResult> CreateTransaction([FromBody]TransactionRequest request)
        {

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest("Sender's account not found");
            }

            //var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            //if (receiver is null)
            //{
            //    return BadRequest("Receiver account not found");
            //}

            if (!_otpGenerator.Verify(user.Email, request.Otp, 2, 6))
            {
                _logger.LogInformation("Invalid OTP: {}",request.Otp);
                return BadRequest("Invalid OTP");
            }

            if (user.Pin != request.Pin)
            {
                _logger.LogInformation("Incorrect pin: {0}", request.Pin);
                return BadRequest("Incorrect pin");
            }


            if (request.WalletType is WalletType.UsdAccount)
            {
                var usdAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest("USD-Account not found");
                }

                if (usdAccount.Balance < request.Amount)
                {
                    return BadRequest("Insufficient balance!");
                }

                usdAccount.Balance -= request.Amount;

                var logEntry1 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress} from his USD-Account");
                _dbContext.ActivityLogs.Add(logEntry1);

            }
            else if (request.WalletType is WalletType.LedgerAccount)
            {

                var ledgerAccount = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (ledgerAccount is null)
                {
                    return BadRequest("Ledger-Account not found");
                }

                if (ledgerAccount.Balance < request.Amount)
                {
                    return BadRequest("Insufficient balance!");
                }

                ledgerAccount.Balance -= request.Amount;

                var logEntry2 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress} from his Ledger-Account");
                _dbContext.ActivityLogs.Add(logEntry2);
            }
            else if (request.WalletType is WalletType.WalletAccount)
            {
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (wallet is null)
                {
                    return BadRequest("Wallet not found");
                }

                if (wallet.Balance < request.Amount)
                {
                    return BadRequest("Insufficient balance!");
                }

                wallet.Balance -= request.Amount;

                var logEntry3 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress} from his wallet-Account");
                _dbContext.ActivityLogs.Add(logEntry3);
            }
            else
            {
                return BadRequest("Invalid wallet type");
            }


            var transaction = new Transaction()
            {
                Amount = request.Amount,
                SenderId = request.UserId,
                ReceiverWalletAddress = request.ReceiverWalletAddress,
                Details = request.Details,
                Status = TransactionStatus.Successful,
                Type = TransactionType.Transfer,
                Timestamp = DateTime.UtcNow,
                WalletType = request.WalletType,
            };
            await _dbContext.Transactions.AddAsync(transaction);


            //var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress}");
            //_dbContext.ActivityLogs.Add(logEntry);

            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                var response = new TransactionResponse()
                {
                    Amount = transaction.Amount,
                    Timestamp = transaction.Timestamp,
                    Status = transaction.Status.ToString(),
                    Type = transaction.Type.ToString(),
                    SenderId = transaction.SenderId,
                    Sender = user.FullName,
                    SenderEmail = user.Email,
                    ReceiverWalletAddress = transaction.ReceiverWalletAddress,
                    Details = transaction.Details,
                    WalletType = transaction.WalletType.ToString(),
                };
                //Todo: send email to admin with details
                return Ok(response);
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
        public async Task<IActionResult> GetAllTransactions(Guid userId)
        {
            List<Transaction> transactions = new List<Transaction>();
            IEnumerable<TransactionResponse> response = new List<TransactionResponse>();


            //var user = User.Identity!.Name ?? string.Empty;
            //var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
            //Guid.TryParse(userIdString?.Value, out Guid userIdGuid);


            //if (userIdGuid == Guid.Empty)
            //{
            //    return Ok(response);
            //}


            transactions = await _dbContext.Transactions
                .Include(t => t.Sender)
                //.Include(t => t.Receiver)
                .Where(t => t.SenderId == userId)
                .ToListAsync();

            response = transactions.Select(t => new TransactionResponse()
            {
                Amount = t.Amount,
                Sender = t.Sender.FullName,
                SenderId = t.SenderId,
                SenderEmail = t.Sender.Email,
                //Receiver = t.Receiver.FullName,
                //ReceiverId = t.ReceiverId,
                //ReceiverEmail = t.Receiver.Email,
                Status = t.Status.ToString(),
                Type = t.Type.ToString(),
                Timestamp = t.Timestamp,
                ReceiverWalletAddress = t.ReceiverWalletAddress,
                Details = t.Details,
            });

            return Ok(response);
        }

    }
}
