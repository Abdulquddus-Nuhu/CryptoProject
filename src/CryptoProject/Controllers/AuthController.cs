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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace CryptoProject.Controllers
{
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _dbContext;

        public AuthController(TokenService tokenService, UserManager<User> userManager, SignInManager<User> signInManager, ILogger<AuthController> logger, AppDbContext dbContext)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyAccessCode([FromBody] string accessCode)
        {
            if (!_dbContext.AccessCodes.Any())
            {
                var defaultCode = Environment.GetEnvironmentVariable("ACCESS_CODE") ?? "DEFAULT_CODE";
                _dbContext.AccessCodes.Add(new AccessCode { Code = defaultCode });

                await _dbContext.SaveChangesAsync();
            }
            
            if (!_dbContext.CryptoWallets.Any())
            {
                var cryptoWalletKey = Environment.GetEnvironmentVariable("Crptocurrency_API_KEY");
                _dbContext.CryptoWallets.Add(new CryptoWallet { Address = cryptoWalletKey });

                await _dbContext.SaveChangesAsync();
            }

            var accessCodeDb = await _dbContext.AccessCodes.FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(accessCodeDb.Code))
            {
                return StatusCode(500,new BaseResponse() { Message = "Access code not found! Please contact administrator", Code = 500, Status = false });
            }

            if (accessCodeDb.Code == accessCode)
            {
                _logger.LogInformation("Access granted: {0}", accessCode);
                return Ok(new { Message = "Access granted", });
            }

            _logger.LogInformation("Invalid access code supplied: {0}", accessCode);
            return Unauthorized(new { Message = "Invalid access code" });
        }

        [AllowAnonymous]
        [SwaggerOperation(
         Summary = "Register a user",
         Description = "AccountTypes:-  SavingsAccount == 0, CurrentAccount == 1," +
            " FixedDepositAccount == 2, RecurringDepositAccount == 3," +
            " CheckingAccount == 4, OffshoreAccount == 5, MMAccount == 6, CDAccount == 7,")
         //OperationId = "auth.login",
         //Tags = new[] { "AuthEndpoints" })
        ]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterUser model)
        {
            var response = new BaseResponse();
            var user = new User 
            {
                Id = Guid.NewGuid(),
                UserName = model.Email, 
                Email = model.Email,
                Address = model.Address,
                AccountType = model.AccountType,
                State = model.State,
                City = model.City,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                MiddleName = model.MiddleName,
                Role = Entities.Enums.RoleType.User,
                Country = model.Country,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Password = model.Password,
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                //assign user to role
                await _userManager.AddToRoleAsync(user, nameof(RoleType.User));

                //create wallet for user
                var wallet = new Wallet
                {
                    UserId = user.Id,
                    Balance = 0
                };
                await _dbContext.Wallets.AddAsync(wallet);

                var usdAccount = new USDAccount 
                { 
                    Balance = 0,
                    UserId = user.Id 
                };
                await _dbContext.USDAccounts.AddAsync(usdAccount);

                var ledgerAccount = new LedgerAccount
                {
                    Balance = 0,
                    UserId = user.Id
                };
                await _dbContext.LedgerAccounts.AddAsync(ledgerAccount);

                //update user with wallet id
                user.WalletId = wallet.Id;
                user.LedgerAccountId = ledgerAccount.Id;
                user.USDAccountId = usdAccount.Id;
                _dbContext.Users.Update(user);


                var activityLog = new ActivityLog
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    ActivityType = ActivityType.UserRegistered,
                    Timestamp = DateTime.UtcNow,
                    Details = $"User with email {model.Email} registered",
                    Data = JsonSerializer.Serialize(model)
                };
                await _dbContext.ActivityLogs.AddAsync(activityLog);
                //Todo: send email to user/admin with details
                await _dbContext.SaveChangesAsync();


                //get a user from database including account number
                var newUser = await _userManager.FindByEmailAsync(user.Email);
                if (newUser is null)
                {
                    return StatusCode(201);
                }

                var cryptoWallet = await _dbContext.CryptoWallets.FirstOrDefaultAsync();

                var userResponse = new UserResponse()
                {
                    Id = newUser.Id,
                    AccountNumber = newUser.AccountNumber,
                    PhoneNumber = newUser.PhoneNumber,
                    USDAccountBalance = 0,
                    City = newUser.City,
                    Country = newUser.Country,
                    AccountType = newUser.AccountType.ToString(),
                    Address = newUser.Address,
                    LedgerAccountBalance = 0,
                    Email = newUser.Email,
                    FirstName = newUser.FirstName,
                    LastName = newUser.LastName,
                    MiddleName = newUser.MiddleName,
                    Pin = newUser.Pin,
                    State = newUser.State,
                    WalletBalance = 0,
                    Role = newUser.Role.ToString(),
                    LedgerAccountNumber = cryptoWallet.Address ?? string.Empty,
                };

                return StatusCode(201, userResponse);
            }

            _logger.LogInformation(result.Errors.First().ToString());
            response.Message = string.Join(',', result.Errors.Select(a => a.Description));
            response.Status = false;
            response.Code = 400;
            return BadRequest(response);
        }

        [AllowAnonymous]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(LoginResponse),StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("User with email {0} tried to login but account doesnt exist", request.Email);
                return Unauthorized(new BaseResponse() { Message = "Invalid Email", Code = 401, Status = false });
            }

            var userAccount = await _dbContext.Users
                        .Include(x => x.Wallet)
                        .Include(x => x.LedgerAccount)
                        .Include(x => x.USDAccount)
                        .FirstOrDefaultAsync(x => x.Id == user.Id);

            var persona = new PersonaResponse()
            {
                Email = user.Email,
                UserName = user.UserName,
                Id = user.Id,
                Roles = await _userManager.GetRolesAsync(user),
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
            };

            var cryptoWallet = await _dbContext.CryptoWallets.FirstOrDefaultAsync();

            var loginResponse = new LoginResponse();
            if (persona.Role == RoleType.Admin.ToString())
            {
                loginResponse.Id = user.Id;
                loginResponse.FirstName = user.FirstName;
                loginResponse.LastName = user.LastName;
                loginResponse.MiddleName = user.MiddleName;
                loginResponse.Email = user.Email;
                loginResponse.UserName = user.UserName;
                loginResponse.PhoneNumber = user.PhoneNumber;
                loginResponse.Role = user.Role.ToString();
                loginResponse.AccountType = user.AccountType.ToString();
                loginResponse.Address = user.Address;
                loginResponse.State = user.State;
                loginResponse.City = user.City;
                loginResponse.LedgerAccountNumber = cryptoWallet.Address ?? string.Empty;
            }
            else
            {
                loginResponse.Id = user.Id;
                loginResponse.FirstName = user.FirstName;
                loginResponse.LastName = user.LastName;
                loginResponse.MiddleName = user.MiddleName;
                loginResponse.Email = user.Email;
                loginResponse.UserName = user.UserName;
                loginResponse.PhoneNumber = user.PhoneNumber;
                loginResponse.Role = user.Role.ToString();
                loginResponse.AccountType = user.AccountType.ToString();
                loginResponse.Address = user.Address;
                loginResponse.State = user.State;
                loginResponse.City = user.City;
                loginResponse.WalletId = user.WalletId;
                loginResponse.WalletBalance = userAccount?.Wallet.Balance ?? 0;
                loginResponse.LedgerAccountId = userAccount.USDAccountId;
                loginResponse.LedgerAccountBalance = userAccount?.LedgerAccount?.Balance ?? 0;
                loginResponse.USDAccountId = userAccount.USDAccountId;
                loginResponse.USDAccountBalance = userAccount?.USDAccount?.Balance ?? 0;
                loginResponse.LedgerAccountNumber = cryptoWallet.Address ?? string.Empty;
                loginResponse.Pin = userAccount.Pin;
                loginResponse.Country = userAccount.Country;
                loginResponse.AccountNumber = userAccount.AccountNumber;
            }


            var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
            if (result.Succeeded)
            {
                if (user.IsActive is false)
                {
                    var activityLogFalse = new ActivityLog
                    {
                        UserId = user.Id,
                        UserEmail = user.Email,
                        ActivityType = ActivityType.UserLoggedIn,
                        Timestamp = DateTime.UtcNow,
                        Details = $"User with email {user.Email} logged in but account is not active",
                    };
                    await _dbContext.ActivityLogs.AddAsync(activityLogFalse);
                    await _dbContext.SaveChangesAsync();


                    _logger.LogInformation("User with email {0} logged in but account is not active", request.Email);
                    return Unauthorized(new BaseResponse() { Message = "Account is not active", Code = 401, Status = false });
                }

                var activityLog = new ActivityLog
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    ActivityType = ActivityType.UserLoggedIn,
                    Timestamp = DateTime.UtcNow,
                    Details = $"User with email {user.Email} logged in",
                };
                await _dbContext.ActivityLogs.AddAsync(activityLog);
                await _dbContext.SaveChangesAsync();

                loginResponse.Token = (_tokenService.GetToken(persona)).Token;


                //Todo: send email to admin with details
                _logger.LogInformation("User with email {0} logged in", request.Email);
                return Ok(new {  loginResponse });
            }

            _logger.LogInformation("User with email {0} tried to login but password is invalid", request.Email);
            return Unauthorized(new BaseResponse() { Message = "Invalid Password", Status = false, Code = 401});
        }


        [AllowAnonymous]
        // POST: api/Auth/Logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User with email {0} logs out", User.Identity.Name);
            await _signInManager.SignOutAsync();
            return Ok();
        }
    }
}
