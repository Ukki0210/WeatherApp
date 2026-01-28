using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Threading.Tasks;

namespace WeatherApp.Server.Services
{
    public interface IEmailService
    {
        System.Threading.Tasks.Task SendWeatherAlertAsync(string toEmail, string userName, string city, string alertMessage);
    }

    public class EmailService : IEmailService
    {
        private readonly string _brevoApiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _brevoApiKey = configuration["Brevo:ApiKey"] ?? "";
            _fromEmail = configuration["Brevo:FromEmail"] ?? "ukki0210@gmail.com";
            _fromName = configuration["Brevo:FromName"] ?? "Weather App Alerts";
            _logger = logger;
        }

        public async System.Threading.Tasks.Task SendWeatherAlertAsync(string toEmail, string userName, string city, string alertMessage)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Configure Brevo API
                    Configuration.Default.ApiKey.Clear();
                    Configuration.Default.ApiKey.Add("api-key", _brevoApiKey);

                    var apiInstance = new TransactionalEmailsApi();

                    // Sender and receiver
                    var sender = new SendSmtpEmailSender(_fromName, _fromEmail);
                    var to = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(toEmail, userName) };

                    var subject = $"⚠️ Severe Weather Alert for {city}";

                    var htmlContent = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <div style='background: linear-gradient(135deg, #f56565 0%, #ed8936 100%); padding: 20px; text-align: center;'>
                                <h1 style='color: white; margin: 0;'>⚠️ Weather Alert</h1>
                            </div>
                            <div style='padding: 20px; background: #f7fafc;'>
                                <h2>Hello {userName},</h2>
                                <p style='font-size: 16px; line-height: 1.6;'>
                                    We've detected severe weather conditions in <strong>{city}</strong>, 
                                    one of your favorite cities.
                                </p>
                                <div style='background: white; padding: 15px; border-left: 4px solid #f56565; margin: 20px 0;'>
                                    <p style='margin: 0; font-size: 15px;'>{alertMessage}</p>
                                </div>
                                <p style='color: #666; font-size: 14px;'>
                                    Stay safe and check the app for detailed weather information.
                                </p>
                                <a href='http://localhost:5159/dashboard' 
                                   style='display: inline-block; background: #667eea; color: white; 
                                          padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 10px;'>
                                    View Dashboard
                                </a>
                            </div>
                            <div style='background: #e2e8f0; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                                <p>You're receiving this because you favorited {city} in Weather App.</p>
                                <p>© 2026 Weather App. All rights reserved.</p>
                            </div>
                        </div>
                    ";

                    var sendSmtpEmail = new SendSmtpEmail(sender, to)
                    {
                        Subject = subject,
                        HtmlContent = htmlContent
                    };

                    var result = apiInstance.SendTransacEmail(sendSmtpEmail);

                    _logger.LogInformation("Email sent to {Email} for {City}. Message ID: {MessageId}", 
                        toEmail, city, result.MessageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                }
            });
        }
    }
}
