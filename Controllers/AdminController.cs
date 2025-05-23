using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EsmaGida.Data;
using EsmaGida.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System;

namespace EsmaGida.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Products()
        {
            // Ürünleri veritabanından çekerek görünüme aktarıyoruz
            var products = await _context.products.ToListAsync();
            return View(products);
        }

        public IActionResult Customers()
        {
            // Buraya müşteri yönetimi için gerekli veriler eklenecek
            return View();
        }

        public IActionResult Reports()
        {
            // Buraya raporlar için gerekli veriler eklenecek
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.products.ToListAsync();
            return Json(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct([FromForm] Product product, IFormFile ImageFile)
        {
            try
            {
                // Form verilerini loglama
                Console.WriteLine($"Form Verileri: ID={product.id}, İsim={product.name}, Fiyat={product.price}, Stok={product.stock_quantity}, Kategori={product.category_id}");
                
                // Kategori ID kontrolü - Foreign Key hatasını önlemek için
                if (product.category_id.HasValue)
                {
                    // Kategori tablosunda bu ID var mı diye kontrol et
                    // 0 veya geçersiz değer veya veritabanında olmayan kategori ise null yap
                    if (product.category_id.Value <= 0)
                    {
                        product.category_id = null;
                    }
                    // Burada kategori tablosunda olup olmadığını kontrol edebilirsiniz
                    // Şimdilik sadece null yapıyoruz
                    else
                    {
                        // TODO: Kategori tablosunda ID var mı kontrol et
                        // Şimdilik basit çözüm: Null yap
                        product.category_id = null;
                    }
                }

                // ModelState içindeki "ImageFile" ile ilgili hataları temizle
                if (ModelState.ContainsKey("ImageFile"))
                {
                    ModelState.Remove("ImageFile");
                }
                
                if (ModelState.IsValid)
                {
                    try
                    {
                        // Dosya yükleme işlemi
                        if (ImageFile != null)
                        {
                            try
                            {
                                // Yükleme için klasör oluştur
                                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                                if (!Directory.Exists(uploadsFolder))
                                    Directory.CreateDirectory(uploadsFolder);

                                // Benzersiz dosya adı oluştur
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                // Dosyayı kaydet
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await ImageFile.CopyToAsync(fileStream);
                                }

                                // Ürüne dosya yolunu kaydet
                                product.image_url = "/images/products/" + uniqueFileName;
                            }
                            catch (Exception imgEx)
                            {
                                return Json(new { success = false, message = "Görsel yükleme sırasında hata: " + imgEx.Message + " - " + imgEx.StackTrace });
                            }
                        }
                        else
                        {
                            // Resim zorunlu değil, varsayılan bir değer veya null olarak ayarla
                            product.image_url = null;
                        }

                        // Oluşturulma zamanını UTC olarak ayarla
                        product.created_at = DateTime.UtcNow;

                        _context.products.Add(product);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, product });
                    }
                    catch (System.Exception ex)
                    {
                        // Daha detaylı hata mesajı
                        return Json(new { success = false, message = "Hata: " + ex.Message + (ex.InnerException != null ? " - İç Hata: " + ex.InnerException.Message : "") });
                    }
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, errors = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Genel hata: " + ex.Message + (ex.InnerException != null ? " - İç Hata: " + ex.InnerException.Message : "") });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.products.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Ürün bulunamadı." });
            }

            try
            {
                // Eski resmi sil
                if (!string.IsNullOrEmpty(product.image_url))
                {
                    string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, product.image_url.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _context.products.Remove(product);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (System.Exception ex)
            {
                // Hata durumunda loglama yapılabilir
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Json(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct([FromForm] Product product, IFormFile ImageFile)
        {
            try
            {
                // Form verilerini loglama
                Console.WriteLine($"Güncelleme Form Verileri: ID={product.id}, İsim={product.name}, Fiyat={product.price}, Stok={product.stock_quantity}, Kategori={product.category_id}");
                
                // Kategori ID kontrolü - Foreign Key hatasını önlemek için
                if (product.category_id.HasValue)
                {
                    // Kategori tablosunda bu ID var mı diye kontrol et
                    // 0 veya geçersiz değer veya veritabanında olmayan kategori ise null yap
                    if (product.category_id.Value <= 0)
                    {
                        product.category_id = null;
                    }
                    // Burada kategori tablosunda olup olmadığını kontrol edebilirsiniz
                    // Şimdilik sadece null yapıyoruz
                    else
                    {
                        // TODO: Kategori tablosunda ID var mı kontrol et
                        // Şimdilik basit çözüm: Null yap
                        product.category_id = null;
                    }
                }

                // ModelState içindeki "ImageFile" ile ilgili hataları temizle
                if (ModelState.ContainsKey("ImageFile"))
                {
                    ModelState.Remove("ImageFile");
                }
                
                if (ModelState.IsValid)
                {
                    try
                    {
                        var existingProduct = await _context.products.FindAsync(product.id);
                        if (existingProduct == null)
                        {
                            return Json(new { success = false, message = "Ürün bulunamadı." });
                        }

                        // Dosya yükleme işlemi
                        if (ImageFile != null)
                        {
                            try
                            {
                                // Eski resmi sil
                                if (!string.IsNullOrEmpty(existingProduct.image_url))
                                {
                                    string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingProduct.image_url.TrimStart('/'));
                                    if (System.IO.File.Exists(oldImagePath))
                                    {
                                        System.IO.File.Delete(oldImagePath);
                                    }
                                }

                                // Yükleme için klasör oluştur
                                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "products");
                                if (!Directory.Exists(uploadsFolder))
                                    Directory.CreateDirectory(uploadsFolder);

                                // Benzersiz dosya adı oluştur
                                string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                // Dosyayı kaydet
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await ImageFile.CopyToAsync(fileStream);
                                }

                                // Ürüne dosya yolunu kaydet
                                product.image_url = "/images/products/" + uniqueFileName;
                            }
                            catch (Exception imgEx)
                            {
                                return Json(new { success = false, message = "Görsel yükleme sırasında hata: " + imgEx.Message + " - " + imgEx.StackTrace });
                            }
                        }
                        else
                        {
                            // Yeni resim yüklenmemişse eski resmi koru
                            product.image_url = existingProduct.image_url;
                        }

                        // Değerleri güncelle
                        _context.Entry(existingProduct).CurrentValues.SetValues(product);
                        
                        // created_at değerini UTC zamanı olarak güncelle
                        if (product.created_at == null || product.created_at.Value.Kind != DateTimeKind.Utc)
                        {
                            existingProduct.created_at = DateTime.UtcNow;
                        }
                        
                        await _context.SaveChangesAsync();
                        return Json(new { success = true });
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!ProductExists(product.id))
                        {
                            return Json(new { success = false, message = "Ürün bulunamadı." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Güncelleme işlemi sırasında eşzamanlılık hatası: " + ex.Message });
                        }
                    }
                    catch (System.Exception ex)
                    {
                        return Json(new { success = false, message = "Hata: " + ex.Message + (ex.InnerException != null ? " - İç Hata: " + ex.InnerException.Message : "") });
                    }
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, errors = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Genel hata: " + ex.Message + (ex.InnerException != null ? " - İç Hata: " + ex.InnerException.Message : "") });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.products.Any(e => e.id == id);
        }

        // Kullanıcı yönetimi için yeni metodlar
        public async Task<IActionResult> Users()
        {
            var users = await _context.User.ToListAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                var allUsers = await _context.User.ToListAsync();
                return PartialView("_UserListPartial", allUsers);
            }

            searchTerm = searchTerm.ToLower();
            var filteredUsers = await _context.User
                .Where(u => u.name.ToLower().Contains(searchTerm) || 
                            u.surname.ToLower().Contains(searchTerm) || 
                            u.email.ToLower().Contains(searchTerm))
                .ToListAsync();

            return PartialView("_UserListPartial", filteredUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetUserRole(int userId, string role)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            try
            {
                user.Role = role;
                _context.Update(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Sertifikalar Yönetimi
        public async Task<IActionResult> Certificates()
        {
            var certificates = await _context.certificates.ToListAsync();
            return View(certificates);
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificates()
        {
            var certificates = await _context.certificates.ToListAsync();
            return Json(certificates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCertificate([FromForm] Certificate certificate, IFormFile ImageFile, IFormFile CategoryLogoFile, IFormFile DocumentFile)
        {
            try
            {
                // ModelState içindeki dosya ile ilgili hataları temizle
                if (ModelState.ContainsKey("ImageFile"))
                {
                    ModelState.Remove("ImageFile");
                }
                if (ModelState.ContainsKey("DocumentFile"))
                {
                    ModelState.Remove("DocumentFile");
                }
                if (ModelState.ContainsKey("CategoryLogoFile"))
                {
                    ModelState.Remove("CategoryLogoFile");
                }
                
                // image_url zorunluluğunu kaldır
                if (ModelState.ContainsKey("image_url"))
                {
                    ModelState.Remove("image_url");
                }
                
                // is_active değerini düzelt - artık buna gerek yok çünkü formda value="true" ve hidden field kullanıyoruz
                
                if (ModelState.IsValid)
                {
                    try
                    {
                        // Sertifika görseli yükleme işlemi
                        if (ImageFile != null)
                        {
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "certificates");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await ImageFile.CopyToAsync(fileStream);
                            }

                            certificate.image_url = "/images/certificates/" + uniqueFileName;
                        }
                        else
                        {
                            // Varsayılan bir görsel yolu atayabilirsiniz
                            certificate.image_url = "/images/certificates/default-certificate.png";
                        }
                        
                        // Kategori logosu yükleme işlemi (opsiyonel)
                        if (CategoryLogoFile != null)
                        {
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "category_logos");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + CategoryLogoFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await CategoryLogoFile.CopyToAsync(fileStream);
                            }

                            certificate.category_logo_url = "/images/category_logos/" + uniqueFileName;
                        }
                        
                        // PDF dosyası yükleme işlemi (opsiyonel)
                        if (DocumentFile != null)
                        {
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "documents", "certificates");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + DocumentFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await DocumentFile.CopyToAsync(fileStream);
                            }

                            certificate.document_url = "/documents/certificates/" + uniqueFileName;
                        }

                        certificate.created_at = DateTime.UtcNow;

                        _context.certificates.Add(certificate);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, certificate });
                    }
                    catch (System.Exception ex)
                    {
                        return Json(new { success = false, message = "Hata: " + ex.Message + (ex.InnerException != null ? " - İç Hata: " + ex.InnerException.Message : "") });
                    }
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, errors = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Genel hata: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var certificate = await _context.certificates.FindAsync(id);
            if (certificate == null)
            {
                return Json(new { success = false, message = "Sertifika bulunamadı." });
            }

            try
            {
                // Eski görseli sil
                if (!string.IsNullOrEmpty(certificate.image_url))
                {
                    string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, certificate.image_url.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                
                // Eski kategori logosunu sil
                if (!string.IsNullOrEmpty(certificate.category_logo_url))
                {
                    string oldLogoPath = Path.Combine(_hostEnvironment.WebRootPath, certificate.category_logo_url.TrimStart('/'));
                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        System.IO.File.Delete(oldLogoPath);
                    }
                }
                
                // Eski PDF dosyasını sil
                if (!string.IsNullOrEmpty(certificate.document_url))
                {
                    string oldDocPath = Path.Combine(_hostEnvironment.WebRootPath, certificate.document_url.TrimStart('/'));
                    if (System.IO.File.Exists(oldDocPath))
                    {
                        System.IO.File.Delete(oldDocPath);
                    }
                }

                _context.certificates.Remove(certificate);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificate(int id)
        {
            var certificate = await _context.certificates.FindAsync(id);
            if (certificate == null)
            {
                return NotFound();
            }
            return Json(certificate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCertificate([FromForm] Certificate certificate, IFormFile ImageFile, IFormFile CategoryLogoFile, IFormFile DocumentFile)
        {
            try
            {
                // ModelState içindeki dosya ile ilgili hataları temizle
                if (ModelState.ContainsKey("ImageFile"))
                {
                    ModelState.Remove("ImageFile");
                }
                if (ModelState.ContainsKey("DocumentFile"))
                {
                    ModelState.Remove("DocumentFile");
                }
                if (ModelState.ContainsKey("CategoryLogoFile"))
                {
                    ModelState.Remove("CategoryLogoFile");
                }
                
                // image_url zorunluluğunu kaldır
                if (ModelState.ContainsKey("image_url"))
                {
                    ModelState.Remove("image_url");
                }
                
                // is_active değerini düzelt - artık buna gerek yok çünkü formda value="true" ve hidden field kullanıyoruz
                
                if (ModelState.IsValid)
                {
                    try
                    {
                        var existingCertificate = await _context.certificates.FindAsync(certificate.id);
                        if (existingCertificate == null)
                        {
                            return Json(new { success = false, message = "Sertifika bulunamadı." });
                        }

                        // Görsel yükleme işlemi
                        if (ImageFile != null)
                        {
                            // Eski görseli sil
                            if (!string.IsNullOrEmpty(existingCertificate.image_url) && 
                                !existingCertificate.image_url.Contains("default-certificate.png"))
                            {
                                string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingCertificate.image_url.TrimStart('/'));
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }

                            // Yeni görseli yükle
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "certificates");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await ImageFile.CopyToAsync(fileStream);
                            }

                            certificate.image_url = "/images/certificates/" + uniqueFileName;
                        }
                        else
                        {
                            // Yeni görsel yüklenmemişse eski görseli koru
                            certificate.image_url = existingCertificate.image_url;
                        }
                        
                        // Kategori logosu yükleme işlemi (opsiyonel)
                        if (CategoryLogoFile != null)
                        {
                            // Eski kategori logosunu sil
                            if (!string.IsNullOrEmpty(existingCertificate.category_logo_url))
                            {
                                string oldLogoPath = Path.Combine(_hostEnvironment.WebRootPath, existingCertificate.category_logo_url.TrimStart('/'));
                                if (System.IO.File.Exists(oldLogoPath))
                                {
                                    System.IO.File.Delete(oldLogoPath);
                                }
                            }

                            // Yeni logoyu yükle
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "category_logos");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + CategoryLogoFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await CategoryLogoFile.CopyToAsync(fileStream);
                            }

                            certificate.category_logo_url = "/images/category_logos/" + uniqueFileName;
                        }
                        else
                        {
                            // Yeni logo yüklenmemişse eski logoyu koru
                            certificate.category_logo_url = existingCertificate.category_logo_url;
                        }
                        
                        // PDF dosyası yükleme işlemi (opsiyonel)
                        if (DocumentFile != null)
                        {
                            // Eski PDF'i sil
                            if (!string.IsNullOrEmpty(existingCertificate.document_url))
                            {
                                string oldDocPath = Path.Combine(_hostEnvironment.WebRootPath, existingCertificate.document_url.TrimStart('/'));
                                if (System.IO.File.Exists(oldDocPath))
                                {
                                    System.IO.File.Delete(oldDocPath);
                                }
                            }

                            // Yeni PDF'i yükle
                            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "documents", "certificates");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + DocumentFile.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await DocumentFile.CopyToAsync(fileStream);
                            }

                            certificate.document_url = "/documents/certificates/" + uniqueFileName;
                        }
                        else
                        {
                            // Yeni PDF yüklenmemişse eski PDF'i koru
                            certificate.document_url = existingCertificate.document_url;
                        }

                        // Değerleri güncelle
                        _context.Entry(existingCertificate).CurrentValues.SetValues(certificate);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true });
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        if (!CertificateExists(certificate.id))
                        {
                            return Json(new { success = false, message = "Sertifika bulunamadı." });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Güncelleme işlemi sırasında eşzamanlılık hatası: " + ex.Message });
                        }
                    }
                    catch (System.Exception ex)
                    {
                        return Json(new { success = false, message = "Hata: " + ex.Message });
                    }
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, errors = errors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Genel hata: " + ex.Message });
            }
        }

        private bool CertificateExists(int id)
        {
            return _context.certificates.Any(e => e.id == id);
        }
    }
} 