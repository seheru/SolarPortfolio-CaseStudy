using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using SolarBackend.Data;
using SolarBackend.Models;
using System.Text.Json.Serialization;

namespace SolarBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------
    // 1. KAYIT OL (Test Hesabı Oluşturmak İçin)
    // -------------------------------------------------------------
[HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequest request) // Body'den alacak
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("Bu e-posta zaten kayıtlı!");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Kullanıcı başarıyla oluşturuldu!" });
    }

    // -------------------------------------------------------------
    // 2. GİRİŞ YAP (E-posta ve Şifre Kontrolü)
    // -------------------------------------------------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("E-posta veya şifre hatalı!");

        // Eğer kullanıcı henüz MFA kurmadıysa
        if (!user.IsMfaEnabled)
            return Ok(new { Message = "MFA kurulumu gerekiyor.", RequiresMfaSetup = true });

        // Eğer kullanıcı zaten MFA kurmuşsa (Normal giriş akışı)
        return Ok(new { Message = "Lütfen 6 haneli kodu girin.", RequiresMfaVerify = true });
    }

    // -------------------------------------------------------------
    // 3. MFA KURULUM (QR Kod Üretme)
    // -------------------------------------------------------------
    [HttpPost("mfa-setup")]
    public async Task<IActionResult> MfaSetup(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("Kullanıcı bulunamadı.");

        // ÖNEMLİ: Eğer kullanıcının anahtarı varsa yenisini üretme (Senkronizasyon hatasını önler)
        if (string.IsNullOrEmpty(user.MfaSecretKey))
        {
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            user.MfaSecretKey = Base32Encoding.ToString(secretKey);
            await _context.SaveChangesAsync();
        }

        var otpAuthUri = new OtpUri(OtpType.Totp, user.MfaSecretKey, user.Email, "SolarPortfolio").ToString();
        
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        var base64QrCode = Convert.ToBase64String(qrCodeImage);

        return Ok(new { QrCodeImage = $"data:image/png;base64,{base64QrCode}" });
    }

    // -------------------------------------------------------------
    // 4. MFA DOĞRULA (6 Haneli Kod ve Kurtarma Kodları Üretimi)
    // -------------------------------------------------------------
    [HttpPost("mfa-verify-setup")]
    public async Task<IActionResult> VerifyMfaSetup([FromBody] VerifyRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || string.IsNullOrEmpty(user.MfaSecretKey))
            return BadRequest("Kullanıcı veya MFA anahtarı bulunamadı.");

        var totp = new OtpNet.Totp(Base32Encoding.ToBytes(user.MfaSecretKey));
        
        // Zaman toleransı ekleyerek doğrula (Gecikmeleri önler)
        bool isValid = totp.VerifyTotp(request.Code, out long timeStepMatched, window: new VerificationWindow(previous: 1, future: 1));

        if (!isValid)
            return Unauthorized("Girdiğiniz kod yanlış veya süresi dolmuş.");

        // Eğer MFA zaten kurulu değilse, bu ilk kurulumdur: Kurtarma Kodlarını Üret
        if (!user.IsMfaEnabled)
        {
            user.IsMfaEnabled = true;
            var recoveryCodesList = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                recoveryCodesList.Add(Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper());
            }
            user.RecoveryCodes = string.Join(",", recoveryCodesList);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "MFA Aktif Edildi!", RecoveryCodes = recoveryCodesList });
        }

        return Ok(new { Message = "Giriş Başarılı!" });
    }

    // -------------------------------------------------------------
    // 5. KURTARMA KODUYLA GİRİŞ (Acil Durum)
    // -------------------------------------------------------------
    [HttpPost("verify-recovery-code")]
    public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || string.IsNullOrEmpty(user.RecoveryCodes))
            return BadRequest("Geçerli kurtarma kodu bulunamadı.");

        var codes = user.RecoveryCodes.Split(',').ToList();
        var submittedCode = request.Code.ToUpper().Trim();

        if (codes.Contains(submittedCode))
        {
            // Kullanılan kodu listeden sil (Güvenlik gereği tek kullanımlık)
            codes.Remove(submittedCode);
            user.RecoveryCodes = string.Join(",", codes);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Kurtarma koduyla giriş başarılı!" });
        }

        return Unauthorized("Geçersiz kurtarma kodu!");
    }
}

// -------------------------------------------------------------
// VERİ TRANSFER MODELLERİ (DTO)
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