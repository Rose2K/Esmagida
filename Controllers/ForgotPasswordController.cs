using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EsmaGida.Models;
using System.Security.Cryptography;
using System.Text;
using EsmaGida.Data;

namespace EsmaGida.Controllers
{
    public class ForgotPasswordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUsername = "omerkhve5234@gmail.com";
        private readonly string _smtpPassword = "yiws tyim hnnr pkwp";

        public ForgotPasswordController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Şifre Hashleme Fonksiyonu
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CheckEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendResetLink(string email)
        {
            try
            {
                // Kullanıcıyı veritabanında ara
                var user = await _context.User.FirstOrDefaultAsync(u => u.email == email);
                if (user == null)
                {
                    return Json(new { success = false, message = "Bu e-posta adresi ile kayıtlı bir kullanıcı bulunamadı." });
                }

                // Şifre sıfırlama token'ı oluştur
                var resetToken = Guid.NewGuid().ToString();
                user.ResetPasswordToken = resetToken;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1); // 1 saat geçerli
                await _context.SaveChangesAsync();

                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Host = _smtpServer;
                    smtpClient.Port = _smtpPort;
                    smtpClient.EnableSsl = true;
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.Timeout = 20000;

                    // SSL sertifika doğrulamasını devre dışı bırak (sadece geliştirme ortamında)
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    var resetLink = $"{Request.Scheme}://{Request.Host}/ForgotPassword/ResetPassword?token={resetToken}";

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(_smtpUsername, "Esma Gıda Şifre Sıfırlama");
                        mailMessage.To.Add(email);
                        mailMessage.Subject = "Şifre Sıfırlama Talebi";
                        mailMessage.Body = $@"
                            <html>
                            <head>
                                <style>
                                    body {{ font-family: Arial, sans-serif; }}
                                    .container {{ padding: 20px; }}
                                    .header {{ background-color: #e9ecef; padding: 10px; border-radius: 5px; }}
                                    .content {{ margin-top: 20px; }}
                                    .button {{ 
                                        display: inline-block;
                                        padding: 10px 20px;
                                        background-color: #007bff;
                                        color: white;
                                        text-decoration: none;
                                        border-radius: 5px;
                                        margin-top: 20px;
                                    }}
                                </style>
                            </head>
                            <body>
                                <div class='container'>
                                    <div class='header'>
                                        <h2>Şifre Sıfırlama Talebi</h2>
                                    </div>
                                    <div class='content'>
                                        <p>Merhaba,</p>
                                        <p>Hesabınız için bir şifre sıfırlama talebinde bulunuldu. Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
                                        <a href='{resetLink}' class='button'>Şifremi Sıfırla</a>
                                        <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                                        <p>Bu link 1 saat süreyle geçerlidir.</p>
                                    </div>
                                </div>
                            </body>
                            </html>";
                        mailMessage.IsBodyHtml = true;
                        mailMessage.Priority = MailPriority.High;

                        try
                        {
                            await smtpClient.SendMailAsync(mailMessage);
                            return Json(new { success = true, message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi." });
                        }
                        catch (SmtpException smtpEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"SMTP Error: {smtpEx.Message}");
                            System.Diagnostics.Debug.WriteLine($"Status Code: {smtpEx.StatusCode}");
                            System.Diagnostics.Debug.WriteLine($"Stack Trace: {smtpEx.StackTrace}");
                            
                            return Json(new { success = false, message = $"SMTP Hatası: {smtpEx.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index");
            }

            var user = await _context.User.FirstOrDefaultAsync(u => 
                u.ResetPasswordToken == token && 
                u.ResetPasswordTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                TempData["Error"] = "Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı.";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            try
            {
                var user = await _context.User.FirstOrDefaultAsync(u => 
                    u.ResetPasswordToken == token && 
                    u.ResetPasswordTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    return Json(new { success = false, message = "Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı." });
                }

                // Şifreyi hashle
                using (var sha256 = SHA256.Create())
                {
                    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(newPassword));
                    user.password = Convert.ToBase64String(hashedBytes);
                }

                // Token'ı temizle
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password Reset Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return Json(new { success = false, message = "Şifre güncellenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
            }
        }
    }
} 