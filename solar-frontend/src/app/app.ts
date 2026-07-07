import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; 
import { HttpClient } from '@angular/common/http'; 

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule], 
  templateUrl: './app.html',
})
export class AppComponent {
  userEmail = '';
  userPassword = '';
  isRobotChecked = false;
  
  // TypeScript'e zorla "Bu bir sayıdır, her şey olabilir" diyoruz
  loginStep: number = 1; 
  mfaCode = ''; 
  loginMessage = ''; 
  
  qrCodeImageUrl = ''; 

  constructor(private http: HttpClient) {} 

  // ==========================================
  // 1. AŞAMA: E-POSTA VE ŞİFRE İLE GİRİŞ
  // ==========================================
  onLoginClick() {
    if (this.isRobotChecked === false) {
      this.loginMessage = "HATA: Lütfen robot olmadığınızı kanıtlayın!";
      return;
    }

    const backendUrl = 'http://localhost:5032/api/Auth/login';
    
    // BURASI DÜZELTİLDİ: Login aşamasında sadece e-posta ve şifre yollanır!
    const loginKutu = {
      email: this.userEmail,
      password: this.userPassword
    };

    this.http.post(backendUrl, loginKutu).subscribe({
      next: (cevap: any) => {
        
        // Şifre doğruysa ve MFA Kurulumu (Karekod) gerekiyorsa:
        if (cevap.requiresMfaSetup || cevap.RequiresMfaSetup) { 
          const qrUrl = `http://localhost:5032/api/Auth/mfa-setup?email=${this.userEmail}`;
          
          this.http.post(qrUrl, {}).subscribe({
            next: (qrCevap: any) => {
              this.qrCodeImageUrl = qrCevap.QrCodeImage || qrCevap.qrCodeImage; 
              this.loginStep = 2; // 2. AŞAMAYA GEÇ!
              this.loginMessage = ''; 
            }
          });
        }
        else {
          // Zaten kuruluysa, karekodsuz 2. aşamaya geç
          this.loginStep = 2;
          this.loginMessage = ''; 
        }

      },
      error: (hata) => {
        this.loginMessage = "HATA: E-posta veya şifre hatalı!";
      }
    });
  }

  // ==========================================
  // 2. AŞAMA: 6 HANELİ KODU DOĞRULA
  // ==========================================
  // 2. Aşamada çalışacak buton
recoveryCodes: string[] = [];

  onVerifyClick() {
    const backendUrl = 'http://localhost:5032/api/Auth/mfa-verify-setup';
    
    // En standart JSON formatı (Küçük harflerle)
    const kutu = {
      email: this.userEmail,
      code: this.mfaCode.toString().trim()
    };

    this.http.post(backendUrl, kutu).subscribe({
      next: (cevap: any) => {
        this.recoveryCodes = cevap.RecoveryCodes || cevap.recoveryCodes; // Kodları hafızaya al
        this.loginStep = 3; 
        this.loginMessage = '';
      },
      error: (hata) => {
        this.loginMessage = "HATA: " + (hata.error || "Kod yanlış!");
      }
    });
  }
}