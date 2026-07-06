using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet; // 6 Haneli şifre algoritması
using QRCoder; // Karekod çizim kütüphanesi
using SolarBackend.Data;
using SolarBackend.Models;

namespace SolarBackend.Controllers;

[Route("api/[controller]")] // İnternetten bu koda ulaşmak için adres: api/Auth
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    // Dependency Injection: Bu sınıf her çalıştığında veritabanı köprüsü otomatik olarak içine verilir.
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
        // 1. E-posta kontrolü: Tabloda bu maile sahip biri var mı?
        if (await _context.Users.AnyAsync(u => u.Email == email))
            return BadRequest("Bu e-posta zaten kayıtlı!");

        // 2. Yeni Kullanıcı oluştur ve Şifreyi Hash'le (Kriptola)
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        // 3. Veritabanına yaz
        _context.Users.Add(user);
        await _context.SaveChangesAsync(); // C++ ve Python'daki commit() mantığı

        return Ok("Kullanıcı başarıyla oluşturuldu!");
    }

    // -------------------------------------------------------------
    // 2. GİRİŞ YAP (Case Study: Login)
    // -------------------------------------------------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        // 1. Kasadan bu e-postaya sahip kişiyi getir
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        // 2. Kullanıcı yoksa VEYA girdiği şifre (Hash'ten geçirilince) veritabanındakiyle eşleşmiyorsa REDDET.
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Unauthorized("E-posta veya şifre hatalı!");

        // 3. Şifre doğru. Ancak MFA (İki aşamalı doğrulama) kurulu mu?
        // Case Study Kuralı: İlk girişte MFA kurulumuna yönlendir.
        if (user.IsMfaEnabled == false)
            return Ok(new { Message = "Şifre doğru! Ancak MFA kurulumu gerekiyor.", RequiresMfaSetup = true });

        // MFA kuruluysa sadece kod girmesini iste
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

        // 1. Google Authenticator mantığı için rastgele 20 karakterli ortak bir "Gizli Sır (Secret)" üretiyoruz.
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // 2. Bu sırrı veritabanına kaydediyoruz ki yarın adam giriş yaparken kodları karşılaştırabilelim.
        user.MfaSecretKey = base32Secret;
        await _context.SaveChangesAsync();

        // 3. Sırrımızı karekoda çevirmek için Authenticator uygulamasının anlayacağı özel bir Link (URI) yapıyoruz.
        var otpAuthUri = new OtpUri(OtpType.Totp, base32Secret, user.Email, "SolarPortfolio").ToString();

        // 4. Linki siyah-beyaz Karekod Resmine (QR) çeviriyoruz.
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        
        // 5. Resmi API üzerinden (JSON olarak) gönderebilmek için Base64 (Metin) formatına dönüştürüyoruz.
        var base64QrCode = Convert.ToBase64String(qrCodeImage);

        return Ok(new { 
            Message = "QR Kod başarıyla üretildi.",
            QrCodeImage = $"data:image/png;base64,{base64QrCode}",
            ManualEntryKey = base32Secret 
        });
    }

    // -------------------------------------------------------------
    // 4. 6 HANELİ KODU DOĞRULA (Case Study: MFA Verify ve Recovery Codes)
    // -------------------------------------------------------------
    [HttpPost("mfa-verify-setup")]
    public async Task<IActionResult> VerifyMfaSetup(string email, string code)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || string.IsNullOrEmpty(user.MfaSecretKey))
            return BadRequest("Kullanıcı bulunamadı.");

        // 1. Veritabanındaki Gizli Sırrı (Secret) alıp, TOTP (Zamana Dayalı Şifre) algoritmasına sokuyoruz.
        var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecretKey));
        
        // 2. Sistemin o an hesapladığı 6 haneli kod ile, Adamın telefondan bakıp girdiği kodu karşılaştırıyoruz.
        bool isValid = totp.VerifyTotp(code, out long timeStepMatched, window: null);

        if (!isValid)
            return Unauthorized("Girdiğiniz 6 haneli kod yanlış veya süresi dolmuş.");

        // 3. KOD DOĞRU! Artık adamın hesabı tamamen güvenli. MFA durumunu True yapıyoruz.
        user.IsMfaEnabled = true;

        // 4. Case Study Kuralı: Adama 5 adet Kurtarma Kodu (Recovery Code) ver.
        var recoveryCodesList = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            // Rastgele 8 haneli harf/rakam üretiyoruz
            recoveryCodesList.Add(Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper());
        }
        
        // Veritabanına bu 5 kodu aralarına virgül koyarak yazıyoruz (Örn: A1B2,C3D4,E5F6)
        user.RecoveryCodes = string.Join(",", recoveryCodesList);
        await _context.SaveChangesAsync();

        return Ok(new {
            Message = "MFA başarıyla kuruldu!",
            RecoveryCodes = recoveryCodesList
        });
    }
}