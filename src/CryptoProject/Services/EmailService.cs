using ElasticEmail.Api;
using ElasticEmail.Client;
using ElasticEmail.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoProject.Services
{
    public class EmailService
    {
        private readonly EmailsApi _emailsApi;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _config;

        public EmailService(IWebHostEnvironment environment, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _config = configuration;

            string emailServiceKey;
            if (environment.IsDevelopment())
            {
                emailServiceKey = _config["ELASTIC_EMAIL_API_KEY"];
            }
            else
            {
                emailServiceKey = Environment.GetEnvironmentVariable("ELASTIC_EMAIL_API_KEY") ?? string.Empty;
            }

            Configuration config = new Configuration();
            config.ApiKey.Add("X-ElasticEmail-ApiKey", emailServiceKey);
            _emailsApi = new EmailsApi(config);
            _logger = logger;
        }

        public void SendEmail(List<string> recipients, string subject, /*string htmlContent,*/ string plainTextContent, string fromAddress)
        {
            var transactionalRecipients = new TransactionalRecipient(to: recipients);
            EmailTransactionalMessageData emailData = new EmailTransactionalMessageData(recipients: transactionalRecipients);

            emailData.Content = new EmailContent
            {
                Body = new List<BodyPart>
                {
                    //new BodyPart
                    //{
                    //    ContentType = BodyContentType.HTML,
                    //    Charset = "utf-8",
                    //    Content = htmlContent
                    //},
                    new BodyPart
                    {
                        ContentType = BodyContentType.PlainText,
                        Charset = "utf-8",
                        Content = plainTextContent
                    }
                },
                From = fromAddress,
                Subject = subject
            };

            try
            {
                _emailsApi.EmailsTransactionalPost(emailData, 0);

                Console.WriteLine("Email sent successfully.");
                _logger.LogInformation("Email sent successfully.");
            }
            catch (ApiException e)
            {
                Console.WriteLine("Exception when calling EmailsApi.EmailsTransactionalPost: " + e.Message);
                _logger.LogInformation("Exception when calling EmailsApi.EmailsTransactionalPost: " + e.Message);

                Console.WriteLine("Status Code: " + e.ErrorCode);
                _logger.LogInformation("Status Code: " + e.ErrorCode);

                Console.WriteLine(e.StackTrace);
                _logger.LogInformation(e.StackTrace);

            }
        }

        //public void SendEmail(List<string> recipients, string subject, string htmlContent, string plainTextContent, string fromAddress)
        //{
        //    var transactionalRecipients = new TransactionalRecipient(to: recipients);
        //    EmailTransactionalMessageData emailData = new EmailTransactionalMessageData(recipients: transactionalRecipients);

        //    emailData.Content = new EmailContent
        //    {
        //        Body = new List<BodyPart>
        //        {
        //            new BodyPart
        //            {
        //                ContentType = BodyContentType.HTML,
        //                Charset = "utf-8",
        //                Content = htmlContent
        //            },
        //            new BodyPart
        //            {
        //                ContentType = BodyContentType.PlainText,
        //                Charset = "utf-8",
        //                Content = plainTextContent
        //            }
        //        },
        //        From = fromAddress,
        //        Subject = subject
        //    };

        //    try
        //    {
        //        _emailsApi.EmailsTransactionalPost(emailData, 0);
        //        Console.WriteLine("Email sent successfully.");
        //    }
        //    catch (ApiException e)
        //    {
        //        Console.WriteLine("Exception when calling EmailsApi.EmailsTransactionalPost: " + e.Message);
        //        Console.WriteLine("Status Code: " + e.ErrorCode);
        //        Console.WriteLine(e.StackTrace);
        //        throw;
        //    }
        //}
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
