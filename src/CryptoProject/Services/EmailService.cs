using Microsoft.AspNetCore.Identity;

namespace CryptoProject.Services
{
    public class EmailService
    {
    }

    //public class SmtpEmailSender : IEmailService
    //{
    //    private readonly UserManager<Persona> _userManager;
    //    private readonly ILogger<SmtpEmailSender> _logger;
    //    private readonly IConfiguration _configuration;

    //    public SmtpEmailSender(UserManager<Persona> userManager,
    //        ILogger<SmtpEmailSender> logger, IConfiguration configuration)
    //    {
    //        _userManager = userManager;
    //        _logger = logger;
    //        _configuration = configuration;
    //    }

    //    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    //    {
    //        return Execute(email, subject, htmlMessage);
    //    }

    //    public async Task Execute(string email, string subject, string htmlMessage)
    //    {
    //        var apiKey = _configuration["Smtp:Username"];
    //        var secretKey = _configuration["Smtp:Password"];
    //        var user = await _userManager.FindByEmailAsync(email);

    //        MailjetClient client = new MailjetClient($"{apiKey}", $"{secretKey}")
    //        {
    //            //Version = ApiVersion.V3_1,
    //        };
    //        MailjetRequest request = new MailjetRequest
    //        {
    //            Resource = SendV31.Resource,
    //        }
    //        .Property(Send.Messages, new JArray {
    //        new JObject {
    //        {
    //        "From",
    //        new JObject {
    //        {"Email", "abdulquddusnuhu@gmail.com"},
    //        {"Name", "noreply@smsabuja.com"}
    //        }
    //        }, {
    //        "To",
    //        new JArray {
    //        new JObject {
    //            {
    //            "Email",
    //            email
    //            }, {
    //            "Name",
    //            user.FirstName
    //            }
    //        }
    //        }
    //        }, {
    //        "Subject",
    //        subject
    //        }, {
    //        "HTMLPart",
    //        htmlMessage
    //        },
    //        }
    //            });

    //        _logger.LogInformation("Sending email to {0}", user.FirstName + " " + user.LastName);
    //        await client.PostAsync(request);
    //    }
    //}

}
