import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; 
import { HttpClient } from '@angular/common/http'; 
import { ThemeService } from './services/theme'; 

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule], 
  templateUrl: './app.html',
})
export class AppComponent {
  // -------------------------------------------------------------------------
  // DEĞİŞKENLER (HAFIZA)
  // -------------------------------------------------------------------------
  userEmail = '';
  userPassword = '';
  isRobotChecked = false;
  
  // loginStep Takibi (Figma ekranları arasındaki geçiş):
  // 1: Login Ekranı
  // 2: MFA Kurulum Ekranı (QR Kod)
  // 3: Kurtarma Kodlarını Görüntüleme Ekranı (MFA Kurulumu sonrası)
  // 4: Normal MFA Doğrulama Ekranı (Zaten kurulu olanlar için)
  // 5: Kurtarma Kodu ile Giriş Ekranı (Authenticator'a erişemeyenler için)
  // 6: Dashboard (Hoşgeldiniz)
  loginStep: number = 1; 

  mfaCode = ''; // Telefon daki 6 haneli kod
  recoveryInputCode = ''; // Kullanıcının girdiği yedek kurtarma kodu
  loginMessage = ''; // Hata veya bilgi mesajları
  qrCodeImageUrl = ''; // C#'tan gelen QR resim yolu
  recoveryCodes: string[] = []; // C#'tan gelen 5 adet yedek kod
  userName = 'Ahmet'; // Figma'daki "Merhaba, Ahmet" yazısı için

  constructor(
  private http: HttpClient, 
  public themeService: ThemeService
) { }

  // -------------------------------------------------------------------------
  // 1. ADIM: İLERİ BUTONU (E-POSTA VE ŞİFRE KONTROLÜ)
  // -------------------------------------------------------------------------
  onLoginClick() {
    if (this.isRobotChecked === false) {
      this.loginMessage = "HATA: Lütfen robot olmadığınızı kanıtlayın!";
      return;
    }

    const backendUrl = 'http://localhost:5032/api/Auth/login';
    const loginKutu = {
      email: this.userEmail,
      password: this.userPassword
    };

    this.http.post(backendUrl, loginKutu).subscribe({
      next: (cevap: any) => {
        // C# "Şifre doğru" dedi. Şimdi MFA durumuna göre ekran seçeceğiz:
        
        // DURUM A: Eğer kullanıcının ilk girişi ise ve MFA kurması gerekiyorsa (RequiresMfaSetup)
        if (cevap.requiresMfaSetup || cevap.RequiresMfaSetup) { 
          const qrUrl = `http://localhost:5032/api/Auth/mfa-setup?email=${this.userEmail}`;
          
          this.http.post(qrUrl, {}).subscribe({
            next: (qrCevap: any) => {
              this.qrCodeImageUrl = qrCevap.QrCodeImage || qrCevap.qrCodeImage; 
              this.loginStep = 2; // QR Kodu gösterme ekranına git (Figma Sol Üst)
              this.loginMessage = ''; 
            }
          });
        }
        // DURUM B: Eğer kullanıcının zaten MFA'sı kuruluysa
        else {
          this.loginStep = 4; // Normal 6 haneli kod sorma ekranına git (Figma Sol Alt)
          this.loginMessage = ''; 
        }
      },
      error: (hata) => {
        this.loginMessage = "HATA: E-posta veya şifre hatalı!";
      }
    });
  }

  // -------------------------------------------------------------------------
  // 2. ADIM: DOĞRULA BUTONU (6 HANELİ KODU ONAYLAMA)
  // -------------------------------------------------------------------------
  onVerifyClick() {
    const backendUrl = 'http://localhost:5032/api/Auth/mfa-verify-setup';
    
    const verifyKutu = {
      email: this.userEmail,
      code: this.mfaCode.replace(/\s/g, '') // Aradaki boşlukları silerek yolluyoruz
    };

    this.http.post(backendUrl, verifyKutu).subscribe({
      next: (cevap: any) => {
        // C# Kodu onayladı! 
        
        // Eğer bu ilk kurulumsa (recoveryCodes gelmişse), kodları gösterelim:
        if (cevap.RecoveryCodes || cevap.recoveryCodes) {
          this.recoveryCodes = cevap.RecoveryCodes || cevap.recoveryCodes;
          this.loginStep = 3; // Kurtarma kodları ekranına git (Figma Sağ Üst)
        } else {
          // Eğer normal giriş yapıyorsa doğrudan Dashboard'a:
          this.loginStep = 6; 
        }
        this.loginMessage = '';
      },
      error: (hata) => {
        this.loginMessage = "HATA: Girdiğiniz kod yanlış veya süresi dolmuş!";
      }
    });
  }

  // -------------------------------------------------------------------------
  // 3. ADIM: KURTARMA KODUYLA GİRİŞ (Authenticator'a erişilemiyorsa)
  // -------------------------------------------------------------------------
  onRecoveryVerify() {
    const backendUrl = 'http://localhost:5032/api/Auth/verify-recovery-code';
    
    const recoveryKutu = {
      email: this.userEmail,
      code: this.recoveryInputCode.trim()
    };

    this.http.post(backendUrl, recoveryKutu).subscribe({
      next: (cevap: any) => {
        // Yedek kod doğru! Sisteme alıyoruz.
        this.loginStep = 6; // Dashboard ekranı
        this.loginMessage = '';
      },
      error: (hata) => {
        this.loginMessage = "HATA: Geçersiz veya kullanılmış kurtarma kodu!";
      }
    });
  }

  // KAYIT OL BUTONU
  onRegisterClick() {
    const backendUrl = 'http://localhost:5032/api/Auth/register';
    const kutu = { email: this.userEmail, password: this.userPassword };

    this.http.post(backendUrl, kutu).subscribe({
      next: (cevap: any) => {
        alert("Kayıt Başarılı! Şimdi giriş yapabilirsiniz.");
        this.loginStep = 1; // Başarılıysa Login ekranına gönder
        this.loginMessage = '';
      },
      error: (hata) => this.loginMessage = "HATA: Kayıt olunamadı! " + hata.error
    });
  }

}