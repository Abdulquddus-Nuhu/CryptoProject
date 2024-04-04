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
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Mime;

namespace CryptoProject.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _dbContext;
        private const string AccessCode = "1234"; 

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
        public IActionResult VerifyAccessCode([FromBody] string accessCode)
        {
            if (accessCode == AccessCode)
            {
                return Ok(new { Message = "Access granted" });
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
            //log this activity in activity log
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
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
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
                    Details = $"User with email {model.Email} registered"
                };
                await _dbContext.ActivityLogs.AddAsync(activityLog);


                await _dbContext.SaveChangesAsync();
                return StatusCode(201);
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
                return Unauthorized("Invalid Email");
            }

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


            var loginResponse = new LoginResponse()
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
                State = user.State,
                City = user.City,
                WalletId = user.WalletId,
            };

            var walletBalance = await _dbContext.Wallets.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (walletBalance is null)
            {
                loginResponse.WalletBalance = 0;
            }
            else
            {
                loginResponse.WalletBalance = walletBalance.Balance;
            }

            var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
            if (result.Succeeded)
            {
                var activityLog = new ActivityLog
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    ActivityType = ActivityType.UserLoggedIn,
                    Timestamp = DateTime.UtcNow,
                    Details = $"User with email {user.Email} logged in"
                };
                await _dbContext.ActivityLogs.AddAsync(activityLog);
                await _dbContext.SaveChangesAsync();

                loginResponse.Token = (_tokenService.GetToken(persona)).Token;

                return Ok(new {  loginResponse });
            }

            _logger.LogInformation("User with email {0} tried to login but password is invalid", request.Email);
            return Unauthorized("Invalid Password");
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
