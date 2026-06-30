using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NextHorizon.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpServer = _configuration["EmailSettings:SmtpServer"];
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            _smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            _smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            _fromEmail = _configuration["EmailSettings:FromEmail"];
            _fromName = _configuration["EmailSettings:FromName"];
        }

        public async Task<bool> SendOTPEmailAsync(string email, string name, string otpCode)
        {
            try
            {
                var subject = "Your Password Reset OTP Code - Next Horizon";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #000; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; text-align: center; }}
                            .otp-code {{ font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #000; margin: 30px 0; padding: 20px; background: #f8f9fa; border-radius: 10px; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <h2>Hello {name},</h2>
                                <p>We received a request to reset your password. Use the OTP code below to proceed:</p>
                                <div class='otp-code'>{otpCode}</div>
                                <p>This code will expire in <strong>15 minutes</strong>.</p>
                                <p>If you didn't request this, please ignore this email or contact support.</p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                <p>This is an automated message, please do not reply.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetConfirmationAsync(string email, string name)
        {
            try
            {
                var subject = "Your Password Has Been Reset - Next Horizon";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #000; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; text-align: center; }}
                            .success-icon {{ font-size: 60px; color: #28a745; margin: 20px 0; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <div class='success-icon'>✓</div>
                                <h2>Hello {name},</h2>
                                <p>Your password has been successfully reset.</p>
                                <p>You can now log in with your new password.</p>
                                <p style='margin-top: 30px;'>
                                    <a href='https://yourdomain.com/Login/AdminLogin' class='btn'>Go to Login</a>
                                </p>
                                <p style='margin-top: 30px; font-size: 14px; color: #6c757d;'>
                                    If you didn't make this change, please contact support immediately.
                                </p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_fromEmail, _fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendAccountDeletionEmailAsync(string email, string name, string adminName)
        {
            try
            {
                var subject = "Account Deletion Notice - Next Horizon";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #dc3545; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; text-align: center; }}
                            .warning-icon {{ font-size: 60px; color: #dc3545; margin: 20px 0; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <div class='warning-icon'>⚠️</div>
                                <h2>Dear {name},</h2>
                                <p>We regret to inform you that your account has been <strong style='color: #dc3545;'>deactivated</strong> by our administrator.</p>
                                <p>This action was performed by: <strong>{adminName}</strong></p>
                                <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 30px 0; text-align: left;'>
                                    <h3 style='margin-top: 0;'>What this means:</h3>
                                    <ul style='padding-left: 20px;'>
                                        <li>You can no longer access your account</li>
                                        <li>Your personal data has been archived</li>
                                        <li>Your order history is preserved for legal purposes</li>
                                    </ul>
                                </div>
                                <p>If you believe this was a mistake or wish to appeal this decision, please contact our support team:</p>
                                <p>
                                    <a href='mailto:nexthorizon398@gmail.com' class='btn'>Contact Support</a>
                                </p>
                                <p style='margin-top: 30px; font-size: 14px; color: #6c757d;'>
                                    Thank you for being part of Next Horizon.
                                </p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                <p>This is an automated message, please do not reply.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account deletion email failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendAccountRestoreEmailAsync(string email, string name, string adminName)
        {
            try
            {
                var subject = "Account Restored - Next Horizon";
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #28a745; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; text-align: center; }}
                            .success-icon {{ font-size: 60px; color: #28a745; margin: 20px 0; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <div class='success-icon'>✓</div>
                                <h2>Welcome Back, {name}!</h2>
                                <p>We're pleased to inform you that your account has been <strong style='color: #28a745;'>restored</strong> by our administrator.</p>
                                <p>This action was performed by: <strong>{adminName}</strong></p>
                                <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 30px 0; text-align: left;'>
                                    <h3 style='margin-top: 0;'>What this means:</h3>
                                    <ul style='padding-left: 20px;'>
                                        <li>You can now access your account again</li>
                                        <li>All your previous data has been restored</li>
                                        <li>You can continue shopping with us</li>
                                    </ul>
                                </div>
                                <p>
                                    <a href='https://nexthorizon.com/Login' class='btn'>Login to Your Account</a>
                                </p>
                                <p style='margin-top: 30px; font-size: 14px; color: #6c757d;'>
                                    If you have any questions, please don't hesitate to contact our support team.
                                </p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                <p>This is an automated message, please do not reply.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account restore email failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSellerStatusUpdateEmailAsync(string email, string businessName, string status, string note, string adminName)
        {
            try
            {
                string subject = "";
                string body = "";
                
                if (status == "Approved")
                {
                    subject = "Seller Application Approved - Next Horizon";
                    body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                                .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                                .header {{ background: #28a745; color: white; padding: 30px; text-align: center; }}
                                .header h1 {{ font-size: 2.5rem; margin: 0; }}
                                .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                                .content {{ padding: 40px; text-align: center; }}
                                .success-icon {{ font-size: 60px; color: #28a745; margin: 20px 0; }}
                                .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                                .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>NH</h1>
                                    <p>Next Horizon</p>
                                </div>
                                <div class='content'>
                                    <div class='success-icon'>✓</div>
                                    <h2>Congratulations, {businessName}!</h2>
                                    <p>Your seller application has been <strong style='color: #28a745;'>approved</strong> by {adminName}.</p>
                                    <p>You can now start listing your products and selling on Next Horizon.</p>
                                    {(string.IsNullOrEmpty(note) ? "" : $@"
                                    <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 30px 0; text-align: left;'>
                                        <h3 style='margin-top: 0;'>Message from Admin:</h3>
                                        <p>{note}</p>
                                    </div>")}
                                    <p>
                                        <a href='https://yourdomain.com/Seller/Dashboard' class='btn'>Go to Seller Dashboard</a>
                                    </p>
                                </div>
                                <div class='footer'>
                                    <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";
                }
                else if (status == "Suspended")
                {
                    subject = "Seller Account Suspended - Next Horizon";
                    body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                                .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                                .header {{ background: #dc3545; color: white; padding: 30px; text-align: center; }}
                                .header h1 {{ font-size: 2.5rem; margin: 0; }}
                                .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                                .content {{ padding: 40px; text-align: center; }}
                                .warning-icon {{ font-size: 60px; color: #dc3545; margin: 20px 0; }}
                                .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>NH</h1>
                                    <p>Next Horizon</p>
                                </div>
                                <div class='content'>
                                    <div class='warning-icon'>⚠️</div>
                                    <h2>Account Suspension Notice</h2>
                                    <p>Your seller account <strong>'{businessName}'</strong> has been <strong style='color: #dc3545;'>suspended</strong> by {adminName}.</p>
                                    <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 30px 0; text-align: left;'>
                                        <h3 style='margin-top: 0;'>Reason for Suspension:</h3>
                                        <p>{note}</p>
                                    </div>
                                    <p>If you have questions, please contact our support team.</p>
                                </div>
                                <div class='footer'>
                                    <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";
                }
                else if (status == "Restored")
                {
                    subject = "Seller Account Restored - Next Horizon";
                    body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                                .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                                .header {{ background: #17a2b8; color: white; padding: 30px; text-align: center; }}
                                .header h1 {{ font-size: 2.5rem; margin: 0; }}
                                .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                                .content {{ padding: 40px; text-align: center; }}
                                .info-icon {{ font-size: 60px; color: #17a2b8; margin: 20px 0; }}
                                .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                                .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>NH</h1>
                                    <p>Next Horizon</p>
                                </div>
                                <div class='content'>
                                    <div class='info-icon'>🔄</div>
                                    <h2>Account Restored</h2>
                                    <p>Great news! Your seller account <strong>'{businessName}'</strong> has been <strong style='color: #17a2b8;'>restored</strong> by {adminName}.</p>
                                    {(string.IsNullOrEmpty(note) ? "" : $@"
                                    <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 30px 0; text-align: left;'>
                                        <h3 style='margin-top: 0;'>Note from Admin:</h3>
                                        <p>{note}</p>
                                    </div>")}
                                    <p>You can now resume selling on Next Horizon.</p>
                                    <p>
                                        <a href='https://yourdomain.com/Seller/Dashboard' class='btn'>Go to Seller Dashboard</a>
                                    </p>
                                </div>
                                <div class='footer'>
                                    <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";
                }
                else if (status == "Rejected")
                {
                    subject = "Seller Application Rejected - Next Horizon";
                    body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                                .container {{ max-width: 500px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                                .header {{ background: #6c757d; color: white; padding: 30px; text-align: center; }}
                                .header h1 {{ font-size: 2.5rem; margin: 0; }}
                                .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                                .content {{ padding: 40px; text-align: center; }}
                                .error-icon {{ font-size: 60px; color: #6c757d; margin: 20px 0; }}
                                .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h1>NH</h1>
                                    <p>Next Horizon</p>
                                </div>
                                <div class='content'>
                                    <div class='error-icon'>❌</div>
                                    <h2>Application Update</h2>
                                    <p>We regret to inform you that your seller application for <strong>'{businessName}'</strong> has been <strong style='color: #6c757d;'>rejected</strong> by {adminName}.</p>
                                    <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 30px 0; text-align: left;'>
                                        <h3 style='margin-top: 0;'>Reason for Rejection:</h3>
                                        <p>{note}</p>
                                    </div>
                                    <p>Please contact support if you need clarification or wish to reapply.</p>
                                </div>
                                <div class='footer'>
                                    <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                </div>
                            </div>
                        </body>
                        </html>";
                }

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seller status update email failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendAdminCredentialsEmailAsync(string email, string firstName, string lastName, string username, string password, string userType, string addedByAdmin)
        {
            try
            {
                string fullName = $"{firstName} {lastName}";
                string loginUrl = "https://yourdomain.com/Login/AdminLogin"; // Update with your actual URL
                
                var subject = $"Welcome to Next Horizon - Your {userType} Account Credentials";
                
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 550px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #000; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; }}
                            .credentials-box {{ background: #f8f9fa; border-radius: 15px; padding: 25px; margin: 20px 0; }}
                            .credential-row {{ display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid #e0e0e0; }}
                            .credential-label {{ font-weight: 600; color: #333; }}
                            .credential-value {{ color: #555; font-family: monospace; }}
                            .badge {{ display: inline-block; padding: 5px 12px; background: #28a745; color: white; border-radius: 20px; font-size: 12px; font-weight: 600; }}
                            .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 8px; }}
                            .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; margin-top: 20px; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <h2>Welcome to the Team, {fullName}!</h2>
                                <p>You have been added as a <strong>{userType}</strong> to the Next Horizon Admin Portal by <strong>{addedByAdmin}</strong>.</p>
                                
                                <div class='credentials-box'>
                                    <h3 style='margin-top: 0; margin-bottom: 20px; text-align: center;'>Your Login Credentials</h3>
                                    <div class='credential-row'>
                                        <span class='credential-label'>Username:</span>
                                        <span class='credential-value'>{username}</span>
                                    </div>
                                    <div class='credential-row'>
                                        <span class='credential-label'>Password:</span>
                                        <span class='credential-value'>{password}</span>
                                    </div>
                                    <div class='credential-row'>
                                        <span class='credential-label'>User Type:</span>
                                        <span class='credential-value'>{userType}</span>
                                    </div>
                                </div>
                                
                                <div class='warning'>
                                    <strong>⚠️ Important Security Notice:</strong>
                                    <ul style='margin-top: 10px; padding-left: 20px;'>
                                        <li>Please change your password immediately after your first login.</li>
                                        <li>Do not share your credentials with anyone.</li>
                                        <li>For security reasons, this email contains sensitive information.</li>
                                    </ul>
                                </div>
                                
                                <div style='text-align: center;'>
                                    <a href='{loginUrl}' class='btn'>Login to Admin Portal</a>
                                </div>
                                
                                <p style='margin-top: 30px; font-size: 14px; color: #6c757d; text-align: center;'>
                                    If you have any questions, please contact the system administrator.
                                </p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                <p>This is an automated message, please do not reply.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin credentials email failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendAdminRevokedEmailAsync(string email, string firstName, string lastName, string userType, string reason, string revokedByAdmin)
        {
            try
            {
                string fullName = $"{firstName} {lastName}";
                string supportEmail = "support@nexthorizon.com";
                
                var subject = $"Important: Your {userType} Access Has Been Revoked - Next Horizon";
                
                var body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: 'Poppins', Arial, sans-serif; background-color: #f4f7f6; margin: 0; padding: 20px; }}
                            .container {{ max-width: 550px; margin: 0 auto; background: white; border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.08); }}
                            .header {{ background: #dc3545; color: white; padding: 30px; text-align: center; }}
                            .header h1 {{ font-size: 2.5rem; margin: 0; }}
                            .header p {{ font-size: 0.75rem; letter-spacing: 4px; text-transform: uppercase; opacity: 0.7; margin: 0; }}
                            .content {{ padding: 40px; }}
                            .warning-icon {{ font-size: 60px; color: #dc3545; text-align: center; margin-bottom: 20px; }}
                            .info-box {{ background: #f8f9fa; border-radius: 15px; padding: 25px; margin: 20px 0; }}
                            .reason-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 8px; }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
                            .btn {{ background: #000; color: white; padding: 12px 30px; text-decoration: none; border-radius: 10px; display: inline-block; font-weight: 600; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>NH</h1>
                                <p>Next Horizon</p>
                            </div>
                            <div class='content'>
                                <div class='warning-icon'>⚠️</div>
                                <h2 style='text-align: center; color: #dc3545;'>Access Revoked</h2>
                                <p>Dear <strong>{fullName}</strong>,</p>
                                <p>We regret to inform you that your <strong>{userType}</strong> access to the Next Horizon Admin Portal has been <strong style='color: #dc3545;'>revoked</strong> by <strong>{revokedByAdmin}</strong>.</p>
                                
                                <div class='reason-box'>
                                    <strong>📝 Reason for Revocation:</strong>
                                    <p style='margin-top: 10px; margin-bottom: 0;'>{reason}</p>
                                </div>
                                
                                <div class='info-box'>
                                    <h3 style='margin-top: 0;'>What this means:</h3>
                                    <ul style='margin-bottom: 0;'>
                                        <li>You can no longer access the Admin Portal</li>
                                        <li>Your admin privileges have been removed</li>
                                        <li>You will be redirected to the customer portal</li>
                                    </ul>
                                </div>
                                
                                <p>If you believe this was a mistake or wish to appeal this decision, please contact our support team:</p>
                                <div style='text-align: center;'>
                                    <a href='mailto:{supportEmail}' class='btn'>Contact Support</a>
                                </div>
                                
                                <p style='margin-top: 30px; font-size: 14px; color: #6c757d; text-align: center;'>
                                    Thank you for your service as part of the Next Horizon team.
                                </p>
                            </div>
                            <div class='footer'>
                                <p>&copy; 2026 Next Horizon. All rights reserved.</p>
                                <p>This is an automated message, please do not reply.</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin revocation email failed: {ex.Message}");
                return false;
            }
        }

    }
}