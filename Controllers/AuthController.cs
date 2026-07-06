using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SolarBackend.Data;
using SolarBackend.Models;

namespace SolarBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context; // Veritabanı köprümüzü bu sınıfa bağlıyoruz
    }

    // 1. GİZLİ GÖREV: Test Edebilmek İçin Kullanıcı Kayıt Etme Yeri
    [HttpPost("register")]
    public async Task<IActionResult> Register(string email, string password)
    {
        // E-posta daha önce kullanılmış mı diye bak
        if (await _context.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Bu e-posta zaten kayıtlı!");

        // Yeni kullanıcı oluştur ve şifresini BCrypt ile kilitle (Hashle)
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(); // Kasaya kaydet

        return Ok("Kullanıcı başarıyla oluşturuldu! Artık giriş yapabilirsin.");
    }

    // 2. CASE STUDY GÖREVİ: Login (Giriş Yapma) Kontrolü
    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        // Kasada bu e-postaya sahip birini bul
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Kullanıcı yoksa VEYA şifrenin kilidi (Hash) uyuşmuyorsa kilitli kapı göster
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Unauthorized("E-posta veya şifre hatalı!");

        // ŞİFRE DOĞRUYSA MFA (Karekod) DURUMUNA BAK:
        if (user.IsMfaEnabled == false)
        {
            return Ok(new { Message = "Şifre doğru! Ancak MFA kurulumu gerekiyor.", RequiresMfaSetup = true });
        }

        return Ok(new { Message = "Şifre doğru! Lütfen telefonunuzdaki 6 haneli kodu girin.", RequiresMfaVerify = true });
    }
}