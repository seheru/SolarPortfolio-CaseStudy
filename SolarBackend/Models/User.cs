namespace SolarBackend.Models;

// "User" sınıfı, veritabanımızdaki bir satırı (kullanıcıyı) temsil eder.
public class User
{
    // Her kullanıcının benzersiz bir ID'si olmalı (Primary Key)
    public int Id { get; set; }

    // Kullanıcının giriş yapacağı e-posta adresi
    public string Email { get; set; } = string.Empty;

    // GÜVENLİK: Şifreyi asla düz metin (123456) tutmuyoruz. 
    // BCrypt ile şifrelenmiş (kıyma makinesinden geçmiş) halini tutuyoruz.
    public string PasswordHash { get; set; } = string.Empty;

    // MFA (Karekod) kurulumu için üretilen o şahsa özel 20 haneli gizli anahtar
    public string? MfaSecretKey { get; set; } 

    // Kullanıcı karekodu okutup 6 haneli şifreyi başarıyla girdi mi?
    // Case Study kuralı: Bu "true" olmadan sisteme (Dashboard) giremez.
    public bool IsMfaEnabled { get; set; } = false; 

    // Telefonunu kaybederse diye veritabanında tutacağımız 5 adet yedek şifre.
    public string? RecoveryCodes { get; set; } 
}