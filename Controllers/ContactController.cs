using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EsmaGida.Controllers
{
    public class ContactController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> SendContactForm(string template_contactform_name, string template_contactform_email, 
            string template_contactform_phone, string subject, string template_contactform_message)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("", " "),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("cemal.ekici.eren@gmail.com", "Esma Gıda İletişim Formu"),
                    Subject = subject,
                    Body = $@"
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; }}
                                .container {{ padding: 20px; }}
                                .header {{ background-color: #e9ecef; padding: 10px; border-radius: 5px; }}
                                .content {{ margin-top: 20px; }}
                                .details {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>
                                    <h2>Yeni İletişim Formu Mesajı</h2>
                                    <p><strong>Gönderen:</strong> {template_contactform_name}</p>
                                    <p><strong>E-posta:</strong> {template_contactform_email}</p>
                                    <p><strong>Telefon:</strong> {template_contactform_phone}</p>
                                    <p><strong>Konu:</strong> {subject}</p>
                                    <p><strong>Tarih:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                                </div>
                                <div class='content'>
                                    <h3>Mesaj:</h3>
                                    <div class='details'>
                                        <p>{template_contactform_message}</p>
                                    </div>
                                </div>
                            </div>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };

                // Alıcı e-posta adresini ekleyin
                mailMessage.To.Add("cemal.ekici.eren@gmail.com");

                await smtpClient.SendMailAsync(mailMessage);

                return Json(new { success = true, message = "Mesajınız başarıyla gönderildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Mesaj gönderilirken bir hata oluştu: " + ex.Message });
            }
        }
    }
} 
