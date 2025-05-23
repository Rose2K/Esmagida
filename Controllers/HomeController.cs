using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using EsmaGida.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using EsmaGida.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EsmaGida.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;// Loglama işlemleri için
        private readonly ApplicationDbContext _context; // Veritabanı bağlamı için

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;   // Loglama işlemleri için
            _context = context; // Veritabanı bağlamı için
        }

        // Controller başlamadan önce her action'da sepet sayısını hesapla
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            // Kullanıcı giriş yapmışsa sepet sayısını hesapla ve ViewBag'e ekle
            if (User.Identity.IsAuthenticated)
            {
                var userId = GetCurrentUserId();
                int cartItemCount = GetCartItemCountAsync(userId).Result;
                ViewBag.CartItemCount = cartItemCount;
            }
        }

        [Authorize]
        public IActionResult Index()
        {
        return View(); // Index.cshtml sayfasını döndür
        }

        public IActionResult contact()
        {
            return View(); // contact.cshtml sayfasını döndür
        }

        public async Task<IActionResult> sertifikalar()
        {
            // Sertifikaları veritabanından çek
            var certificates = await _context.certificates
                .Where(c => c.is_active)
                .OrderBy(c => c.category)
                .ToListAsync();
            
            // Sertifikaları kategorilere göre grupla
            var groupedCertificates = certificates
                .GroupBy(c => c.category)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            return View(groupedCertificates);
        }

        public IActionResult forgotpass()
        {
            return View(); // forgotpass.cshtml sayfasını döndür
        }
        public IActionResult indexx()
        {
            return View(); // indexx.cshtml sayfasını döndür
        }
        public async Task<IActionResult> shop()
        {
            var products = await _context.products.ToListAsync(); // Ürünleri veritabanından çekiyoruz
            return View(products); // shop.cshtml sayfasına ürünleri gönderiyoruz
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _context.products.FirstOrDefaultAsync(p => p.id == id);
            if (product == null)
            {
                return NotFound(); // Ürün bulunamazsa 404 döndür
            }
            
            // Similar products - here we're getting products from the same category if available
            // If no category is set, we'll get random products
            ViewBag.RelatedProducts = await _context.products
                .Where(p => p.id != id) // Exclude current product
                .Where(p => product.category_id.HasValue && p.category_id == product.category_id || !product.category_id.HasValue)
                .OrderBy(p => Guid.NewGuid()) // Random order
                .Take(4) // Get maximum 4 related products
                .ToListAsync();
            
            return View(product); // ProductDetail.cshtml sayfasına ürünü gönderiyoruz
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("indexx", "Home");     
            }
            return View(); // Login.cshtml sayfasını döndür
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login", "Home");
            }
            return View();  // Register.cshtml sayfasını döndür
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.email == email);
            if (user == null || !VerifyPassword(password, user.password))
            {
                ViewBag.Error = "Geçersiz e-posta veya şifre";
                return View();
            }   // Kullanıcı adı ve şifre doğru değilse hata mesajı göster

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.name + " " + user.surname),
                new Claim(ClaimTypes.Email, user.email),
                new Claim(ClaimTypes.Role, user.Role), // Rol bilgisini ekle
                new Claim("UserId", user.Id.ToString()) // UserId bilgisini ekle
            }; // Kullanıcı bilgilerini saklamak için claim oluşturur

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);  // Kullanıcı bilgilerini saklamak için principal oluşturur

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7) // Çerez 7 gün boyunca geçerli olacak
                });

            return RedirectToAction("indexx");   // Index sayfasına yönlendir
        }
            
        public async Task<IActionResult> Logout()   // Çıkış işlemi
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("login");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string firstName, string lastName, string email, string password)
        {
            var userExists = await _context.User.AnyAsync(u => u.email == email);
            if (userExists) // E-posta adresi zaten kayıtlı mı kontrol et
            {
                ViewBag.Error = "Bu e-posta adresi zaten kayıtlı.";
                return View();  // E-posta adresi zaten kayıtlıysa hata mesajı göster
            }

            var hashedPassword = HashPassword(password);    // Şifreyi hashle
            var user = new User // Yeni kullanıcı oluştur
            {
                name = firstName,   
                surname = lastName,
                email = email,
                password = hashedPassword,
                Role = "User" // Yeni kayıt olan kullanıcılar için varsayılan rol
            };  // Kullanıcı bilgilerini doldur

            _context.User.Add(user);    // Kullanıcıyı veritabanına ekle
            await _context.SaveChangesAsync();  // Veritabanını güncelle
            
                TempData["SuccessMessage"] = "Kayıt işlemi başarılı! Giriş yapabilirsiniz.";

            return RedirectToAction("Login");   // Login sayfasına yönlendir
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
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

        // Şifre Doğrulama Fonksiyonu
        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            var inputHash = HashPassword(inputPassword);
            return inputHash == hashedPassword;
        }
        
        // Sepetteki ürün sayısını alma - yardımcı metot
        private async Task<int> GetCartItemCountAsync(int userId)
        {
            var activeCart = await _context.carts
                .FirstOrDefaultAsync(c => c.user_id == userId && c.status == "active");
                
            if (activeCart == null)
                return 0;
                
            return await _context.cart_items
                .Where(ci => ci.cart_id == activeCart.id)
                .SumAsync(ci => ci.quantity);
        }
        
        // Kullanıcı ID'si alma - yardımcı metot
        private int GetCurrentUserId()
        {
            // Gerçek uygulamada Claims'den alınmalı
            // Örnek olarak giriş yapmış kullanıcının email'ini alıp User tablosundan ID'sini bulabiliriz
            if (User.Identity.IsAuthenticated)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var user = _context.User.FirstOrDefault(u => u.email == email);
                if (user != null)
                {
                    return user.Id;
                }
            }
            
            // Eğer kullanıcı ID'si bulunamazsa varsayılan olarak 1 dönelim
            return 1;
        }
    }
    
}
