﻿using CryptoProject.Data;
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
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;

namespace CryptoProject.Controllers
{
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    //[Authorize(Roles = nameof(RoleType.Admin))]
    [EnableRateLimiting("fixed")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AdminController> _logger;
        public AdminController(AppDbContext dbContext, ILogger<AdminController> logger, UserManager<User> userManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userManager = userManager;
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _dbContext.Users.Where(x => x.Role == RoleType.User)
                .Include(x => x.Wallet)
                .Include(x => x.LedgerAccount)
                .Include(x => x.USDAccount)
                .OrderByDescending(x => x.Created)
                .ToListAsync();

            var cryptoWallet = await _dbContext.CryptoWallets.FirstOrDefaultAsync();

            //todo: add pagination searching and filtering to this endpoint
            var response = users.Select(x => new UserResponse()
            {
                IsActive = x.IsActive,
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
                LedgerAccountNumber = cryptoWallet.Address ?? string.Empty,
                Pin = x.Pin,
                Country = x.Country,
                AccountNumber = x.AccountNumber,
                Password = x.Password,
                CanTransact = x.CanTransact
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

            var cryptoWallet = await _dbContext.CryptoWallets.FirstOrDefaultAsync();


            var response = new UserResponse()
            {
                IsActive = user.IsActive,
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
                LedgerAccountNumber = cryptoWallet.Address ?? string.Empty,
                Country = user.Country,
                AccountNumber = user.AccountNumber,
                Password = user.Password,
                CanTransact = user.CanTransact
            };

            return Ok(response);
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("activate-user/{userId}")]
        public async Task<IActionResult> ActivateUserAccount(Guid userId)
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

            user.IsActive = true;

            var logEntry = new ActivityLog
            {
                UserEmail = User.Identity.Name ?? string.Empty,
                ActivityType = ActivityType.UserDeactivation,
                Timestamp = DateTime.UtcNow,
                Details = $"User with email {user.Email} account is activated",
            };
            _dbContext.ActivityLogs.Add(logEntry);
            await _dbContext.SaveChangesAsync();

            var cryptoWallet = await _dbContext.CryptoWallets.FirstOrDefaultAsync();
            var response = new UserResponse()
            {
                IsActive = user.IsActive,
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
                LedgerAccountNumber = cryptoWallet.Address ?? string.Empty,
                Country = user.Country,
                AccountNumber = user.AccountNumber,
                CanTransact = user.CanTransact
            };

            return Ok(new { message = "User account activated", response = response });
        }
        
        
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("deactivate-user/{userId}")]
        public async Task<IActionResult> DeactivateUserAccount(Guid userId)
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

            user.IsActive = false;

            var logEntry = new ActivityLog
            {
                UserEmail = User.Identity.Name ?? string.Empty,
                ActivityType = ActivityType.UserDeactivation,
                Timestamp = DateTime.UtcNow,
                Details = $"User with email {user.Email} account is deactivated",
            };
            _dbContext.ActivityLogs.Add(logEntry);

            await _dbContext.SaveChangesAsync();

            var cryptoWallet = await _dbContext.CryptoWallets.FirstOrDefaultAsync();
            var response = new UserResponse()
            {
                IsActive = user.IsActive,
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
                LedgerAccountNumber = cryptoWallet.Address ?? string.Empty,
                Country = user.Country,
                AccountNumber = user.AccountNumber,
                CanTransact = user.CanTransact
            };

            return Ok(new { message = "User account deactivated", response = response });
        }


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<ActivityLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("activities")]
        public async Task<IActionResult> GetAllActivities()
        {
            var logs = _dbContext.ActivityLogs
                .OrderByDescending(x => x.Created);

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
            var transactions = _dbContext.Transactions
                .Include(t => t.Sender)
                //.Include(t => t.Receiver)
                .OrderByDescending(x => x.Created);

            var response = transactions.Select(t => new TransactionResponse()
            {
                Id = t.Id,
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
                CoinType = t.CoinType,
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
                return BadRequest(new BaseResponse() { Message = "Transaction not found", Status = false, Code = 400 });
            }

            if (transaction.Status is TransactionStatus.AutoReversed)
            {
                return BadRequest(new BaseResponse() { Message = "Transaction status is already reverted", Status = false, Code = 400 });
            }


            if (transaction.WalletType is WalletType.USD)
            {
                var uSDAccount = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                if (uSDAccount is null)
                {
                    return BadRequest(new BaseResponse() { Message = "Sender's USD-Account not found", Status = false, Code = 400 });
                }

                uSDAccount.Balance += transaction.Amount;


                if (transaction.Type == TransactionType.WalletTranfer)
                {
                    switch (transaction.ToWalletType)
                    {
                        case WalletType.USD:
                            var receiverUSD = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverUSD is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's USD-Account not found", Status = false, Code = 400 });
                            }

                            receiverUSD.Balance -= transaction.Amount;
                            _dbContext.USDAccounts.Update(receiverUSD);
                            break;

                        case WalletType.LedgerAccount:
                            var receiverLedger = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverLedger is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's Ledger-Account not found", Status = false, Code = 400 });
                            }

                            receiverLedger.Balance -= transaction.Amount;
                            _dbContext.LedgerAccounts.Update(receiverLedger);
                            break;

                        case WalletType.WalletAccount:
                            var receiverWallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverWallet is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's Wallet-Account not found", Status = false, Code = 400 });
                            }

                            receiverWallet.Balance -= transaction.Amount;
                            _dbContext.Wallets.Update(receiverWallet);
                            break;
                    }
                }

            }
            else if (transaction.WalletType is WalletType.LedgerAccount)
            {
                var ledger = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                if (ledger is null)
                {
                    return BadRequest(new BaseResponse() { Message = "Sender's ledger-Account not found", Status = false, Code = 400 });
                }

                ledger.Balance += transaction.Amount;


                if (transaction.Type == TransactionType.WalletTranfer)
                {
                    switch (transaction.ToWalletType)
                    {
                        case WalletType.USD:
                            var receiverUSD = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverUSD is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's USD-Account not found", Status = false, Code = 400 });
                            }

                            receiverUSD.Balance -= transaction.Amount;
                            _dbContext.USDAccounts.Update(receiverUSD);
                            break;

                        case WalletType.LedgerAccount:
                            var receiverLedger = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverLedger is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's Ledger-Account not found", Status = false, Code = 400 });
                            }

                            receiverLedger.Balance -= transaction.Amount;
                            _dbContext.LedgerAccounts.Update(receiverLedger);
                            break;

                        case WalletType.WalletAccount:
                            var receiverWallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverWallet is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's Wallet-Account not found", Status = false, Code = 400 });
                            }

                            receiverWallet.Balance -= transaction.Amount;
                            _dbContext.Wallets.Update(receiverWallet);
                            break;
                    }
                }

            }
            else if (transaction.WalletType is WalletType.WalletAccount)
            {
                var wallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == transaction.SenderId);
                if (wallet is null)
                {
                    return BadRequest(new BaseResponse() { Message = "Sender's wallet-Account not found", Status = false, Code = 400 });
                }

                wallet.Balance += transaction.Amount;


                if (transaction.Type == TransactionType.WalletTranfer)
                {
                    switch (transaction.ToWalletType)
                    {
                        case WalletType.USD:
                            var receiverUSD = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverUSD is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's USD-Account not found", Status = false, Code = 400 });
                            }

                            receiverUSD.Balance -= transaction.Amount;
                            _dbContext.USDAccounts.Update(receiverUSD);
                            break;

                        case WalletType.LedgerAccount:
                            var receiverLedger = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverLedger is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's Ledger-Account not found", Status = false, Code = 400 });
                            }

                            receiverLedger.Balance -= transaction.Amount;
                            _dbContext.LedgerAccounts.Update(receiverLedger);
                            break;

                        case WalletType.WalletAccount:
                            var receiverWallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == transaction.SenderId);
                            if (receiverWallet is null)
                            {
                                return BadRequest(new BaseResponse() { Message = "Receiver's Wallet-Account not found", Status = false, Code = 400 });
                            }

                            receiverWallet.Balance -= transaction.Amount;
                            _dbContext.Wallets.Update(receiverWallet);
                            break;
                    }
                }

            }

            transaction.Status = TransactionStatus.AutoReversed;
            transaction.Modified = DateTime.UtcNow;

            _dbContext.Transactions.Update(transaction);

            var userIdString = User.Claims.FirstOrDefault(x => x.Type == "id");
            Guid.TryParse(userIdString?.Value, out Guid userIdGuid);
            if (userIdGuid == Guid.Empty)
            {
                //todo:
            }

            var logEntry = ActivityLogService.CreateLogEntry(null, userEmail: User.Identity.Name, ActivityType.WalletTransferReverted, transaction.Amount);
            logEntry.Data = JsonSerializer.Serialize(transaction);
            _dbContext.ActivityLogs.Add(logEntry);

            var result = await _dbContext.TrySaveChangesAsync();
            if (result)
            {
                _logger.LogInformation("Admin reverted a user transaction with id: {0}", transaction.Id);
                return Ok(new BaseResponse() { Message = "Transaction reverted successfully"});
            }
            else
            {
                _logger.LogInformation("500 - Unable to rervert transaction with id: {0}", transaction.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to rervert transaction! Please try again or contact administrator" });
            }
        }


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Credit a user balance",
         Description = "WalletType:-   UsdAccount == 0, LedgerAccount == 1, WalleAccount == 2,")]
        //OperationId = "auth.login",
        //Tags = new[] { "AuthEndpoints" })
        [HttpPost("credit-user")]
        public async Task<IActionResult> CreditUser([FromBody] CreditRequest request)
        {

            //log credit activity
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }


            if (request.WalletType is WalletType.USD)
            {
                var usdAccount = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "User USD-Account not found", Code = 400 });
                }

                usdAccount.Balance += request.Amount;
                _dbContext.USDAccounts.Update(usdAccount);

                var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.USDFundsAdded, request.Amount);
                logEntry.Data = JsonSerializer.Serialize(request);
                _dbContext.ActivityLogs.Add(logEntry);

                //log acivity
                var result = await _dbContext.TrySaveChangesAsync();
                if (!result)
                {
                    _logger.LogInformation("Unable to credit user's USD-Account! Please try again or contact administrator");
                    return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to credit user's USD-Account! Please try again or contact administrator" });
                }

            }
            else if (request.WalletType is WalletType.LedgerAccount)
            {
                var ledger = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == request.UserId);
                if (ledger is null)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "User Ledger-Account not found", Code = 400 });
                }

                ledger.Balance += request.Amount;
                _dbContext.LedgerAccounts.Update(ledger);

                var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.LedgerFundsAdded, request.Amount);
                logEntry.Data = JsonSerializer.Serialize(request);
                _dbContext.ActivityLogs.Add(logEntry);

                var result = await _dbContext.TrySaveChangesAsync();
                if (!result)
                {
                    _logger.LogInformation("Unable to credit user's Ledger-Account! Please try again or contact administrator");
                    return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to credit user's Ledger-Account! Please try again or contact administrator" });
                }

            }
            else if (request.WalletType is WalletType.WalletAccount)
            {

                var wallet = _dbContext.Wallets.FirstOrDefault(w => w.UserId == request.UserId);
                if (wallet is null)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "User wallet not found", Code = 400 });
                }

                wallet.Balance += request.Amount;
                _dbContext.Wallets.Update(wallet);

                var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.WalletFundsAdded, request.Amount);
                logEntry.Data = JsonSerializer.Serialize(request);
                _dbContext.ActivityLogs.Add(logEntry);

                var result = await _dbContext.TrySaveChangesAsync();
                if (!result)
                {
                    _logger.LogInformation("Unable to credit user's wallet! Please try again or contact administrator");
                    return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to credit user's wallet! Please try again or contact administrator" });
                }
            }


            return Ok(new BaseResponse());
        }


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Debit a user balance",
         Description = "WalletType:-   UsdAccount == 0, LedgerAccount == 1, WalleAccount == 2,")]
        //OperationId = "auth.login",
        //Tags = new[] { "AuthEndpoints" })
        [HttpPost("debit-user")]
        public async Task<IActionResult> DebitUser([FromBody] DebitRequest request)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == request.UserId);
            if (user is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "User not found", Code = 400 });
            }


            if (request.WalletType is WalletType.WalletAccount)
            {
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
                logEntry.Data = JsonSerializer.Serialize(request);
                _dbContext.ActivityLogs.Add(logEntry);

                var result = await _dbContext.TrySaveChangesAsync();
                if (!result)
                {
                    _logger.LogInformation("Unable to debit user's wallet! Please try again or contact administrator");
                    return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to debit user's wallet! Please try again or contact administrator" });

                }
            }
            else if (request.WalletType is WalletType.USD)
            {
                var usdAccount = _dbContext.USDAccounts.FirstOrDefault(w => w.UserId == request.UserId);
                if (usdAccount is null)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "User USD-Account not found", Code = 400 });
                }

                if (usdAccount.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "Insufficient funds", Code = 400 });
                }

                usdAccount.Balance -= request.Amount;
                _dbContext.USDAccounts.Update(usdAccount);

                var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.USDFundsDeducted, request.Amount);
                logEntry.Data = JsonSerializer.Serialize(request);
                _dbContext.ActivityLogs.Add(logEntry);

                var result = await _dbContext.TrySaveChangesAsync();
                if (!result)
                {
                    _logger.LogInformation("Unable to debit user's usd account! Please try again or contact administrator");
                    return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to debit user's wallet! Please try again or contact administrator" });

                }
            }
            else if (request.WalletType is WalletType.LedgerAccount)
            {
                var ledger = _dbContext.LedgerAccounts.FirstOrDefault(w => w.UserId == request.UserId);
                if (ledger is null)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "User Ledger-Account not found", Code = 400 });
                }

                if (ledger.Balance < request.Amount)
                {
                    return BadRequest(new BaseResponse() { Status = false, Message = "Insufficient funds", Code = 400 });
                }

                ledger.Balance -= request.Amount;
                _dbContext.LedgerAccounts.Update(ledger);

                var logEntry = ActivityLogService.CreateLogEntry(request.UserId, userEmail: user.Email, ActivityType.ledgerFundsDeducted, request.Amount);
                logEntry.Data = JsonSerializer.Serialize(request);
                _dbContext.ActivityLogs.Add(logEntry);

                var result = await _dbContext.TrySaveChangesAsync();
                if (!result)
                {
                    _logger.LogInformation("Unable to debit user's ledger account! Please try again or contact administrator");
                    return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse() { Code = 500, Status = false, Message = "Unable to debit user's wallet! Please try again or contact administrator" });

                }
            }

            return Ok(new BaseResponse());

        }


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Edit AccessCode",
         Description = "Edit AccessCode entity in the database")]
        [HttpPut("edit-access-code")]
        public async Task<IActionResult> EditAccessCode([FromBody] EditAccessCodeRequest request)
        {
            var accessCode = _dbContext.AccessCodes.FirstOrDefault();
            if (accessCode is null)
            {
                return BadRequest(new BaseResponse() { Status = false, Message = "AccessCode not found", Code = 400 });
            }

            accessCode.Code = request.NewAccessCode;

            _dbContext.AccessCodes.Update(accessCode);
            _dbContext.SaveChanges();

            return Ok(new BaseResponse());
        }

        //endpoint to be able to edit CryptoWallet in the databse        //endpoint to be able to edit CryptoWallet in the databse
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Edit CryptoWallet",
         Description = "Edit CryptoWallet entity in the database")]
        [HttpPut("edit-crypto-wallet")]
        public async Task<IActionResult> EditCryptoWallet([FromBody] EditCryptoWalletRequest request)
        {
            var cryptoWallet = _dbContext.CryptoWallets.FirstOrDefault();
            if (cryptoWallet is null)
            {
                return StatusCode(404, new BaseResponse() { Status = false, Message = "CryptoWallet not found", Code = 404 });
            }

            cryptoWallet.Address = request.Address;

            _dbContext.CryptoWallets.Update(cryptoWallet);
            _dbContext.SaveChanges();

            return Ok(new BaseResponse());
        }

        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Disable User From Perfoming Transactoin")]
        [HttpPut("disable-transfer")]
        public async Task<IActionResult> DisableUserTransaction(Guid userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                return NotFound(new BaseResponse() { Status = false, Message = "User not found", Code = 404 });
            }

            user.CanTransact = false;

            _dbContext.Users.Update(user);
            _dbContext.SaveChanges();

            return Ok(new BaseResponse());
        }
        
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [SwaggerOperation(
         Summary = "Enable User To Perfome Transactoin")]
        [HttpPut("enable-transfer")]
        public async Task<IActionResult> EnableUserTransaction(Guid userId)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                return NotFound(new BaseResponse() { Status = false, Message = "User not found", Code = 404 });
            }

            user.CanTransact = true;

            _dbContext.Users.Update(user);
            _dbContext.SaveChanges();

            return Ok(new BaseResponse());
        }


        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status500InternalServerError)]
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return BadRequest(new BaseResponse() { Code = 400, Status = false, Message = "Invalid request data." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound(new BaseResponse() { Message = "User not found", Status = false, Code = 404 });
            }

            // Generate a password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Reset the password
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (result.Succeeded)
            {
                user.Password = model.NewPassword;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                return Ok(new BaseResponse() { Message = "Password updated successfully.", Code = 200 });
            }

            return BadRequest(new BaseResponse() {Message = string.Join(',', result.Errors.Select(a => a.Description)) , Code = 400, Status = false});
        }
    }
}
