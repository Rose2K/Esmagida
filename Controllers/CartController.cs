using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EsmaGida.Data;
using EsmaGida.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace EsmaGida.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Controller başlamadan önce her action'da sepet sayısını hesapla
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            
            // Kullanıcı giriş yapmışsa sepet sayısını hesapla ve ViewBag'e ekle
            if (User.Identity.IsAuthenticated)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    int cartItemCount = GetCartItemCountAsync(userId).Result;
                    ViewBag.CartItemCount = cartItemCount;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Sepet sayısı hesaplanırken hata: {ex.Message}");
                    ViewBag.CartItemCount = 0;
                }
            }
            else
            {
                ViewBag.CartItemCount = 0;
            }
        }

        // Sepeti görüntüleme
        [Authorize] // Sadece giriş yapmış kullanıcılar erişebilir
        public async Task<IActionResult> Index()
        {
            try
            {
                // Kullanıcı ID'sini al
                var userId = GetCurrentUserId();
                
                // Kullanıcının aktif sepetini bul, yoksa oluştur
                var cart = await GetOrCreateCartAsync(userId);
                
                // Sepet öğelerini ürün bilgileriyle birlikte getir
                var cartItems = await _context.cart_items
                    .Where(ci => ci.cart_id == cart.id)
                    .ToListAsync();
                    
                // Ürün bilgilerini ayrıca getir
                var productIds = cartItems.Select(ci => ci.product_id).ToList();
                var products = await _context.products
                    .Where(p => productIds.Contains(p.id))
                    .ToDictionaryAsync(p => p.id, p => p);
                    
                // Görünüm modelini oluştur
                var viewModel = new CartViewModel
                {
                    Cart = cart,
                    CartItems = cartItems,
                    Products = products,
                    TotalPrice = cartItems.Sum(ci => ci.price_at_addition * ci.quantity)
                };
                
                return View(viewModel);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet görüntülenirken hata oluştu");
                TempData["ErrorMessage"] = "Sepetiniz görüntülenirken bir hata oluştu. Lütfen tekrar giriş yapın.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Sepet toplamlarını getiren metot (AJAX için)
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCartTotals()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetActiveCartAsync(userId);
                
                if (cart == null)
                {
                    return Json(new { success = false, message = "Aktif sepet bulunamadı." });
                }
                
                var cartItems = await _context.cart_items
                    .Where(ci => ci.cart_id == cart.id)
                    .ToListAsync();
                
                var totalPrice = cartItems.Sum(ci => ci.price_at_addition * ci.quantity);
                
                return Json(new { 
                    success = true, 
                    subtotal = totalPrice.ToString("C", new CultureInfo("tr-TR")),
                    total = totalPrice.ToString("C", new CultureInfo("tr-TR")) 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız.", requireLogin = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Sepet bilgileri alınırken hata oluştu: {ex.Message}" });
            }
        }

        // Sepete ürün ekleme
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Miktar 0'dan büyük olmalıdır." });
                }
                
                // Ürünü kontrol et
                var product = await _context.products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }
                
                // Stok kontrolü
                if (product.stock_quantity < quantity)
                {
                    return Json(new { success = false, message = $"Yeterli stok bulunmamaktadır. Mevcut stok: {product.stock_quantity}" });
                }
                
                // Kullanıcı ID'sini al
                var userId = GetCurrentUserId();
                
                // Kullanıcının aktif sepetini bul, yoksa oluştur
                var cart = await GetOrCreateCartAsync(userId);
                
                // Sepette bu ürün var mı kontrol et
                var existingItem = await _context.cart_items
                    .FirstOrDefaultAsync(ci => ci.cart_id == cart.id && ci.product_id == productId);
                
                if (existingItem != null)
                {
                    try 
                    {
                        // Varsa miktarı güncelle
                        existingItem.quantity += quantity;
                        // PostgreSQL için DateTime değerini UTC olarak güncelle
                        existingItem.added_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                        
                        // Veritabanı değişikliklerini izleme mekanizmasından kaldırıp tekrar ekleyelim
                        // Bu, bazen Entity Framework'ün izleme durumunda oluşan sorunları çözer
                        _context.Entry(existingItem).State = EntityState.Detached;
                        _context.cart_items.Update(existingItem);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Mevcut sepet öğesini güncellerken hata: {Message}", ex.Message);
                        
                        // Farklı bir yaklaşım deneyelim - mevcut öğeyi silelim ve yenisini ekleyelim
                        _context.cart_items.Remove(existingItem);
                        await _context.SaveChangesAsync();
                        
                        // Yeni sepet öğesi oluşturup ekleyelim
                        var newCartItem = new CartItem
                        {
                            cart_id = cart.id,
                            product_id = productId,
                            quantity = existingItem.quantity + quantity, // Toplam miktar
                            price_at_addition = product.price,
                            added_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                        };
                        
                        _context.cart_items.Add(newCartItem);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Yoksa yeni sepet öğesi oluştur
                    var cartItem = new CartItem
                    {
                        cart_id = cart.id,
                        product_id = productId,
                        quantity = quantity,
                        price_at_addition = product.price,
                        added_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                    };
                    
                    _context.cart_items.Add(cartItem);
                    await _context.SaveChangesAsync();
                }
                
                // Sepet özeti için öğe sayısını al
                var itemCount = await _context.cart_items
                    .Where(ci => ci.cart_id == cart.id)
                    .SumAsync(ci => ci.quantity);
                
                return Json(new { success = true, itemCount });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız.", requireLogin = true });
            }
            catch (Exception ex)
            {
                // Hata detaylarını logla
                _logger.LogError(ex, "Sepete ekleme hatası");
                
                // İç hataları da yakala
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                
                // İç hatanın stack trace'ini de al
                var innerStackTrace = ex.InnerException != null ? ex.InnerException.StackTrace : "";
                
                // Daha detaylı hata mesajı döndür
                return Json(new { 
                    success = false, 
                    message = $"Ürün sepete eklenirken bir hata oluştu: {ex.Message}",
                    detail = innerMessage,
                    stackTrace = innerStackTrace
                });
            }
        }
        
        // Sepetten ürün çıkarma
        [HttpPost]
        [Authorize]
        // AJAX çağrıları için token doğrulamasını kullanmıyoruz
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetActiveCartAsync(userId);
                
                if (cart == null)
                {
                    return Json(new { success = false, message = "Aktif sepet bulunamadı." });
                }
                
                var cartItem = await _context.cart_items
                    .FirstOrDefaultAsync(ci => ci.id == cartItemId && ci.cart_id == cart.id);
                
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sepet öğesi bulunamadı." });
                }
                
                _context.cart_items.Remove(cartItem);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız.", requireLogin = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
        
        // Sepet ürün miktarını güncelleme
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Miktar 0'dan büyük olmalıdır." });
                }
                
                var userId = GetCurrentUserId();
                var cart = await GetActiveCartAsync(userId);
                
                if (cart == null)
                {
                    return Json(new { success = false, message = "Aktif sepet bulunamadı." });
                }
                
                var cartItem = await _context.cart_items
                    .FirstOrDefaultAsync(ci => ci.id == cartItemId && ci.cart_id == cart.id);
                
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Sepet öğesi bulunamadı." });
                }
                
                // Ürün stok kontrolü
                var product = await _context.products.FindAsync(cartItem.product_id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }
                
                if (product.stock_quantity < quantity)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Yeterli stok bulunmamaktadır. Mevcut stok: {product.stock_quantity}" 
                    });
                }
                
                cartItem.quantity = quantity;
                // PostgreSQL'in timestamp with time zone için UTC DateTime gerektiğinden,
                // updated_at değerini de güncelliyoruz
                cartItem.added_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                _context.Update(cartItem);
                await _context.SaveChangesAsync();
                
                // Yeni fiyat hesapla
                var newPrice = cartItem.price_at_addition * quantity;
                
                return Json(new { 
                    success = true, 
                    newPrice = newPrice.ToString("C", new CultureInfo("tr-TR")) 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "Bu işlem için giriş yapmalısınız.", requireLogin = true });
            }
            catch (Exception ex)
            {
                // Hata detaylarını logla
                _logger.LogError(ex, "Sepet miktarı güncellenirken hata oluştu");
                
                // İç hataları da yakala
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                
                // İç hatanın stack trace'ini de al
                var innerStackTrace = ex.InnerException != null ? ex.InnerException.StackTrace : "";
                
                // Daha detaylı hata mesajı döndür
                return Json(new { 
                    success = false, 
                    message = $"Sepet miktarı güncellenirken bir hata oluştu: {ex.Message}",
                    detail = innerMessage,
                    stackTrace = innerStackTrace
                });
            }
        }
        
        // Test amaçlı ödeme işlemi olmadan sipariş oluşturma
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetActiveCartAsync(userId);
                
                if (cart == null)
                {
                    TempData["ErrorMessage"] = "Aktif sepet bulunamadı.";
                    return RedirectToAction("Index");
                }
                
                var cartItems = await _context.cart_items
                    .Where(ci => ci.cart_id == cart.id)
                    .ToListAsync();
                
                if (cartItems.Count == 0)
                {
                    TempData["ErrorMessage"] = "Sepetiniz boş.";
                    return RedirectToAction("Index");
                }
                
                try
                {
                    // Tüm veritabanı işlemlerini bir transaction içinde yap
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        // Stok kontrolü ve güncelleme
                        foreach (var item in cartItems)
                        {
                            // CartItem tarih alanlarını UTC'ye çevir
                            if (item.added_at.Kind != DateTimeKind.Utc)
                            {
                                item.added_at = DateTime.SpecifyKind(item.added_at, DateTimeKind.Utc);
                            }
                            
                            var product = await _context.products.FindAsync(item.product_id);
                            if (product == null)
                            {
                                TempData["ErrorMessage"] = "Ürün bulunamadı.";
                                return RedirectToAction("Index");
                            }
                            
                            if (product.stock_quantity < item.quantity)
                            {
                                TempData["ErrorMessage"] = $"{product.name} için yeterli stok bulunmamaktadır. Mevcut stok: {product.stock_quantity}";
                                return RedirectToAction("Index");
                            }
                            
                            // Stok güncelleme
                            product.stock_quantity -= item.quantity;
                            _context.Update(product);
                        }
                        
                        // Sepetin durumunu "completed" olarak işaretle
                        cart.status = "completed";
                        
                        // PostgreSQL için tüm DateTime alanlarını UTC olarak ayarla
                        if (cart.created_at.Kind != DateTimeKind.Utc)
                        {
                            cart.created_at = DateTime.SpecifyKind(cart.created_at, DateTimeKind.Utc);
                        }
                        
                        _context.Update(cart);
                        
                        // Değişiklikleri kaydet
                        await _context.SaveChangesAsync();
                        
                        // Transaction'ı commit et
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    string innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                    _logger.LogError(ex, "Transaction içinde hata: {Inner}", innerMsg);
                    throw; // Üst seviye catch'e gönder
                }
                
                try
                {
                    // Kullanıcı için yeni bir aktif sepet oluştur
                    await GetOrCreateCartAsync(userId);
                    
                    TempData["SuccessMessage"] = "Siparişiniz başarıyla oluşturuldu. Ödeme işlemi test amaçlı atlandı.";
                    return RedirectToAction("indexx", "Home");
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Yeni sepet oluşturulurken hata: {ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMsg += $" - Detay: {ex.InnerException.Message}";
                    }
                    _logger.LogError(ex, "Yeni sepet oluşturma hatası: {Message}", errorMsg);
                    
                    TempData["SuccessMessage"] = "Siparişiniz başarıyla oluşturuldu, ancak yeni sepet oluşturulurken hata oluştu.";
                    return RedirectToAction("indexx", "Home");
                }
            }
            catch (UnauthorizedAccessException)
            {
                TempData["ErrorMessage"] = "Bu işlem için giriş yapmalısınız.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Sipariş oluşturulurken hata oluştu: {ex.Message}";
                
                // Capture inner exception details
                if (ex.InnerException != null)
                {
                    errorMessage += $" - İç hata: {ex.InnerException.Message}";
                    _logger.LogError(ex.InnerException, "Sipariş inner exception: {Message}", ex.InnerException.Message);
                }
                
                _logger.LogError(ex, "Sipariş oluşturulurken hata: {Message}", errorMessage);
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction("Index");
            }
        }
        
        // Mevcut oturumdan kullanıcı ID'sini alır
        private int GetCurrentUserId()
        {
            try 
            {
                // Eğer kullanıcı oturum açmışsa gerçek ID'yi kullan
                if (User.Identity.IsAuthenticated && User.Claims.Any())
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        return userId;
                    }
                    
                    // Kullanıcı ID'si bulunamazsa hata fırlat
                    throw new InvalidOperationException("Kullanıcı ID'si bulunamadı. Lütfen tekrar giriş yapın.");
                }
                else
                {
                    // Kullanıcı giriş yapmamışsa hata fırlat
                    throw new UnauthorizedAccessException("Bu işlem için giriş yapmalısınız.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kullanıcı ID alınırken hata: {ex.Message}");
                throw; // Hatayı tekrar fırlat
            }
        }
        
        // Kullanıcının aktif sepetini döndürür veya yoksa yeni sepet oluşturur
        private async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            // Önce kullanıcının veritabanında olup olmadığını kontrol et
            var user = await _context.User.FindAsync(userId);
            
            // Kullanıcı yoksa yeni bir kullanıcı oluştur
            if (user == null)
            {
                user = new User
                {
                    Id = userId,
                    name = "Demo Kullanıcı",
                    surname = "Demo",
                    email = "demo@example.com",
                    password = "demopassword",
                    Role = "User"
                };
                _context.User.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Otomatik olarak Demo Kullanıcı oluşturuldu (ID: {userId})");
            }
            
            var cart = await GetActiveCartAsync(userId);
            
            if (cart == null)
            {
                // Her zaman UTC DateTime ile sepet oluştur
                cart = new Cart
                {
                    user_id = userId,
                    created_at = DateTime.UtcNow,  // DateTime.UtcNow zaten Kind=UTC olarak gelir
                    status = "active"
                };
                
                _context.carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            
            return cart;
        }
        
        // Kullanıcının aktif sepetini bulur
        private async Task<Cart> GetActiveCartAsync(int userId)
        {
            return await _context.carts
                .FirstOrDefaultAsync(c => c.user_id == userId && c.status == "active");
        }
        
        // Kullanıcının sepetindeki toplam ürün sayısını döndürür
        private async Task<int> GetCartItemCountAsync(int userId)
        {
            var cart = await GetActiveCartAsync(userId);
            
            if (cart == null)
            {
                return 0;
            }
            
            return await _context.cart_items
                .Where(ci => ci.cart_id == cart.id)
                .SumAsync(ci => ci.quantity);
        }
    }
    
    // Sepet görünümü için model sınıfı
    public class CartViewModel
    {
        public Cart Cart { get; set; }
        public List<CartItem> CartItems { get; set; }
        public Dictionary<int, Product> Products { get; set; }
        public decimal TotalPrice { get; set; }
    }
} 