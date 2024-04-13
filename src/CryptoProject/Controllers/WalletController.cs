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
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status=false });
            }

            if (request.WalletType is WalletType.WalletAccount)
            {
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (wallet is null)
                {
                    return BadRequest(new BaseResponse { Message = "Wallet not found", Code = 400, Status = false });
                }
                response.Balance = wallet.Balance;
            }
            else if (request.WalletType is WalletType.LedgerAccount)
            {
                var ledgerAccount = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (ledgerAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "Ledger-Account not found", Code = 400, Status = false });
                }
                response.Balance = ledgerAccount.Balance;
            }
            else if (request.WalletType is WalletType.UsdAccount)
            {
                var uSDAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (uSDAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "USD-Account not found", Code = 400, Status = false });
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
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status = false });
            }

            if (string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest(new BaseResponse { Message = "Ivalid or empty pin", Code = 400, Status = false });
            }
            user.Pin = request.Pin;


            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserSetPin, $"User with email {user.Email} sets a new pin");
            _dbContext.ActivityLogs.Add(logEntry);

            if (await _dbContext.TrySaveChangesAsync())
            {
                return Ok(new BaseResponse() { Message = "Pin updated successfully"});
            }
            else
            {
                return StatusCode(500, new BaseResponse() { Message = "Unable to update record please try again or contact administrator", Status = false });
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
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status = false });
            }

            string otp = _otpGenerator.Generate(user.Email, 2, 6);
            _logger.LogInformation("User with id: {0} requested otp: {1}",request.UserId,otp);

            string subject = "Transfer OTP";
            string message = $"Your OTP is {otp} valid for 2 minutes";
            List<string> receivers = [user.Email];

            _emailService.SendEmail(receivers, subject, message, "abdulquddusnuhu@gmail.com");

            var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserInitiateTransfer, $"User with email {user.Email} initiated a transfer");
            _dbContext.ActivityLogs.Add(logEntry);

            return Ok(new { otp = otp, Message = "Otp sent successfully" });
        }


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
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status = false });
            }

            //var receiver = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            //if (receiver is null)
            //{
            //    return BadRequest("Receiver account not found");
            //}

            if (!_otpGenerator.Verify(user.Email, request.Otp, 2, 6))
            {
                _logger.LogInformation("Invalid OTP: {}",request.Otp);
                return BadRequest(new BaseResponse { Message = "Invalid OTP", Code = 400, Status = false });
            }

            if (user.Pin != request.Pin)
            {
                _logger.LogInformation("Incorrect pin: {0}", request.Pin);
                return BadRequest(new BaseResponse { Message = "Incorrect pin", Code = 400, Status = false });
            }


            if (request.WalletType is WalletType.UsdAccount)
            {
                var usdAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "USD-Account not found", Code = 400, Status = false });
                }

                if (usdAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
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
                    return BadRequest(new BaseResponse { Message = "Ledger-Account not found", Code = 400, Status = false });
                }

                if (ledgerAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
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
                    return BadRequest(new BaseResponse { Message = "Wallet not found", Code = 400, Status = false });
                }

                if (wallet.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
                }

                wallet.Balance -= request.Amount;

                var logEntry3 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress} from his wallet-Account");
                _dbContext.ActivityLogs.Add(logEntry3);
            }
            else
            {
                return BadRequest(new BaseResponse { Message = "Invalid wallet type", Code = 400, Status = false });
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
                return StatusCode(500, new BaseResponse { Message = "Unable to process transfer please try again later or contact administrator", Status = false });
            }


        }


        //[Authorize(Roles = nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("wire-transfer")]
        public async Task<ActionResult> WireTransfer([FromBody] WireTransferRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status = false });
            }

            if (!_otpGenerator.Verify(user.Email, request.Otp, 2, 6))
            {
                _logger.LogInformation("Invalid OTP: {0}", request.Otp);
                return BadRequest(new BaseResponse { Message = "Invalid OTP", Code = 400, Status = false });
            }

            if (user.Pin != request.Pin)
            {
                _logger.LogInformation("Incorrect pin: {0}", request.Pin);
                return BadRequest(new BaseResponse { Message = "Incorrect pin", Code = 400, Status = false });
            }

            if (request.WalletType is WalletType.UsdAccount)
            {
                var usdAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "USD-Account not found", Code = 400, Status = false });
                }

                if (usdAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
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
                    return BadRequest(new BaseResponse { Message = "Ledger-Account not found", Code = 400, Status = false });
                }

                if (ledgerAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
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
                    return BadRequest(new BaseResponse { Message = "Wallet not found", Code = 400, Status = false });
                }

                if (wallet.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
                }

                wallet.Balance -= request.Amount;

                var logEntry3 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress} from his wallet-Account");
                _dbContext.ActivityLogs.Add(logEntry3);
            }
            else
            {
                return BadRequest(new BaseResponse { Message = "Invalid wallet type", Code = 400, Status = false });
            }


            var transaction = new Transaction()
            {
                Amount = request.Amount,
                SenderId = request.UserId,
                ReceiverWalletAddress = request.ReceiverWalletAddress,
                Details = request.Note,
                Status = TransactionStatus.Successful,
                Type = TransactionType.WireTransfer,
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
                return StatusCode(500, new BaseResponse { Message = "Unable to process transfer please try again later or contact administrator", Status = false });
            }
        }


        //[Authorize(Roles = nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BitcoinTransferResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("bitcoin-transfer")]
        public async Task<ActionResult> BitcoinTransfer([FromBody] BitcoinTransferRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status = false });
            }

            if (!_otpGenerator.Verify(user.Email, request.Otp, 2, 6))
            {
                _logger.LogInformation("Invalid OTP: {0}", request.Otp);
                return BadRequest(new BaseResponse { Message = "Invalid OTP", Code = 400, Status = false });
            }

            if (user.Pin != request.Pin)
            {
                _logger.LogInformation("Incorrect pin: {0}", request.Pin);
                return BadRequest(new BaseResponse { Message = "Incorrect pin", Code = 400, Status = false });
            }

            if (request.WalletType is WalletType.UsdAccount)
            {
                var usdAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "USD-Account not found", Code = 400, Status = false });
                }

                if (usdAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
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
                    return BadRequest(new BaseResponse { Message = "Ledger-Account not found", Code = 400, Status = false });
                }

                if (ledgerAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
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
                    return BadRequest(new BaseResponse { Message = "Wallet not found", Code = 400, Status = false });
                }

                if (wallet.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
                }

                wallet.Balance -= request.Amount;

                var logEntry3 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress} from his wallet-Account");
                _dbContext.ActivityLogs.Add(logEntry3);
            }
            else
            {
                return BadRequest(new BaseResponse { Message = "Invalid wallet type", Code = 400, Status = false });
            }


            var transaction = new Transaction()
            {
                Amount = request.Amount,
                SenderId = request.UserId,
                ReceiverWalletAddress = request.ReceiverWalletAddress,
                Details = request.Details,
                Status = TransactionStatus.Successful,
                Type = TransactionType.BitcoinTransfer,
                Timestamp = DateTime.UtcNow,
                WalletType = request.WalletType,
                CoinType = request.CoinType,
            };
            await _dbContext.Transactions.AddAsync(transaction);


            //var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.ReceiverWalletAddress}");
            //_dbContext.ActivityLogs.Add(logEntry);

            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                var response = new BitcoinTransferResponse()
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
                    CoinType = transaction.CoinType,
                };

                //Todo: send email to admin with details
                return Ok(response);
            }
            else
            {
                return StatusCode(500, new BaseResponse { Message = "Unable to process transfer please try again later or contact administrator", Status = false });
            }
        }


        //[Authorize(Roles = nameof(RoleType.User))]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("topUp-wallet")]
        public async Task<ActionResult> TopUpWallet([FromBody] TopUpWalletRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse { Message = "User not found", Code = 400, Status = false });
            }

            if (!_otpGenerator.Verify(user.Email, request.Otp, 2, 6))
            {
                _logger.LogInformation("Invalid OTP: {0}", request.Otp);
                return BadRequest(new BaseResponse { Message = "Invalid OTP", Code = 400, Status = false });
            }

            if (user.Pin != request.Pin)
            {
                _logger.LogInformation("Incorrect pin: {0}", request.Pin);
                return BadRequest(new BaseResponse { Message = "Incorrect pin", Code = 400, Status = false });
            }

            if (request.FromWalletType is WalletType.UsdAccount)
            {
                var usdAccount = await _dbContext.USDAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "USD-Account not found", Code = 400, Status = false });
                }

                if (usdAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
                }

                usdAccount.Balance -= request.Amount;

                var logEntry1 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.FromWalletType.ToString()} from his USD-Account");
                _dbContext.ActivityLogs.Add(logEntry1);

            }
            else if (request.FromWalletType is WalletType.LedgerAccount)
            {

                var ledgerAccount = await _dbContext.LedgerAccounts.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (ledgerAccount is null)
                {
                    return BadRequest(new BaseResponse { Message = "Ledger-Account not found", Code = 400, Status = false });
                }

                if (ledgerAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
                }

                ledgerAccount.Balance -= request.Amount;

                var logEntry2 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.FromWalletType.ToString()} from his Ledger-Account");
                _dbContext.ActivityLogs.Add(logEntry2);
            }
            else if (request.FromWalletType is WalletType.WalletAccount)
            {
                var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (wallet is null)
                {
                    return BadRequest(new BaseResponse { Message = "Wallet not found", Code = 400, Status = false });
                }

                if (wallet.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse { Message = "Insufficient balance!", Code = 400, Status = false });
                }

                wallet.Balance -= request.Amount;

                var logEntry3 = ActivityLogService.CreateLogEntry(request.UserId, userEmail: User.Identity.Name, ActivityType.UserTransfer, $"User with email {user.Email} transfered {request.Amount} to {request.FromWalletType.ToString()} from his wallet-Account");
                _dbContext.ActivityLogs.Add(logEntry3);
            }
            else
            {
                return BadRequest(new BaseResponse { Message = "Invalid wallet type", Code = 400, Status = false });
            }


            var transaction = new Transaction()
            {
                Amount = request.Amount,
                SenderId = request.UserId,
                Status = TransactionStatus.Successful,
                Type = TransactionType.WireTransfer,
                Timestamp = DateTime.UtcNow,
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
                return StatusCode(500, new BaseResponse { Message = "Unable to process transfer please try again later or contact administrator", Status = false });
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
                Status = t.Status.ToString(),
                Type = t.Type.ToString(),
                Timestamp = t.Timestamp,
                ReceiverWalletAddress = t.ReceiverWalletAddress,
                Details = t.Details,
                WalletType = t.WalletType.ToString(),
            });

            return Ok(response);
        }

    }
}
