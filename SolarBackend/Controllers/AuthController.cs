using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet; // 6 Haneli şifre algoritması
using QRCoder; // Karekod çizim kütüphanesi
using SolarBackend.Data;
using SolarBackend.Models;
using System.Text.Json.Serialization;


namespace SolarBackend.Controllers;

[Route("api/[controller]")] // İnternetten bu koda ulaşmak için adres: api/Auth
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    // Dependency Injection: Veritabanı köprüsü bu sınıfa bağlanır.
    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------
    // 1. KAYIT OL (Test İçin Yazdık)
    // -------------------------------------------------------------
    [HttpPost("register")]
    public async Task<IActionResult> Register(string email, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Bu e-posta zaten kayıtlı!");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) // Şifreyi kırılmaz hale getir
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(); 

        return Ok("Kullanıcı başarıyla oluşturuldu!");
    }

    // -------------------------------------------------------------
    // 2. GİRİŞ YAP (Case Study: Login)
    // -------------------------------------------------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Veritabanına bak (Adam var mı?)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        // 2. KONTROL: Adam yoksa VEYA Şifresi yanlışsa geri gönder! (İşte unuttuğun satır burasıydı)
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("E-posta veya şifre hatalı!");

        // 3. Şifre doğruysa normal işleyişine devam etsin...
        if (user.IsMfaEnabled == false)
            return Ok(new { Message = "Şifre doğru! Ancak MFA kurulumu gerekiyor.", RequiresMfaSetup = true });

        // MFA zaten kuruluysa
        return Ok(new { Message = "Şifre doğru! Lütfen telefonunuzdaki 6 haneli kodu girin.", RequiresMfaVerify = true });
    }

    // -------------------------------------------------------------
    // 3. KAREKOD ÜRET (Case Study: MFA Setup)
    // -------------------------------------------------------------
    [HttpPost("mfa-setup")]
    public async Task<IActionResult> MfaSetup(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        // KRİTİK DÜZELTME: Eğer kullanıcının zaten bir anahtarı varsa, 
        // her seferinde yenisini üretip kafa karıştırma! Eskisini kullan.
        if (string.IsNullOrEmpty(user.MfaSecretKey)) 
        {
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            user.MfaSecretKey = Base32Encoding.ToString(secretKey);
            await _context.SaveChangesAsync();
            Console.WriteLine($"YENİ ANAHTAR ÜRETİLDİ: {user.MfaSecretKey}");
        }
        else 
        {
            Console.WriteLine($"ESKİ ANAHTAR KULLANILIYOR: {user.MfaSecretKey}");
        }

        // QR kod üretme kısmı aynı kalıyor...
        var otpAuthUri = new OtpUri(OtpType.Totp, user.MfaSecretKey, user.Email, "SolarPortfolio").ToString();
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        var base64QrCode = Convert.ToBase64String(qrCodeImage);

        return Ok(new { 
            QrCodeImage = $"data:image/png;base64,{base64QrCode}"
        });
    }

    // -------------------------------------------------------------
    // 4. 6 HANELİ KODU DOĞRULA (Case Study: MFA Verify ve Recovery Codes)
    // -------------------------------------------------------------
    [HttpPost("mfa-verify-setup")]
    public async Task<IActionResult> VerifyMfaSetup([FromBody] VerifyRequest request)
    {
        // DEDEKTİFLİK SATIRLARI (Terminalde bunları göreceğiz)
        Console.WriteLine("--- MFA DOĞRULAMA İSTEĞİ GELDİ ---");
        Console.WriteLine($"Gelen Mail: '{request.Email}'");
        Console.WriteLine($"Gelen Kod: '{request.Code}'");


        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || string.IsNullOrEmpty(user.MfaSecretKey))
            return BadRequest("Kullanıcı bulunamadı.");

        var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecretKey));
        
        // Toleranslı doğrulama (Gecikmeleri veya acele girmeleri affeder)
        bool isValid = totp.VerifyTotp(request.Code, out long timeStepMatched, window: new VerificationWindow(previous: 1, future: 1));

        if (!isValid)
            return Unauthorized("Girdiğiniz 6 haneli kod yanlış veya süresi dolmuş.");

        // Kod doğruysa adamın hesabını aktif yap!
        user.IsMfaEnabled = true;

        // Kurtarma kodları (Recovery Codes) üret
        var recoveryCodesList = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            recoveryCodesList.Add(Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper());
        }
        
        user.RecoveryCodes = string.Join(",", recoveryCodesList);
        await _context.SaveChangesAsync();

        return Ok(new {
            Message = "MFA başarıyla kuruldu!",
            RecoveryCodes = recoveryCodesList
        });
    }
}

// -------------------------------------------------------------
// POSTACININ GETİRECEĞİ KUTU ŞEKİLLERİ (Modeller)
// -------------------------------------------------------------
public class LoginRequest 
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class VerifyRequest 
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}