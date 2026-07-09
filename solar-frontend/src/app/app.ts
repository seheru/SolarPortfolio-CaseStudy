import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; 
import { HttpClient } from '@angular/common/http'; 
import { ThemeService } from './services/theme'; 
import { LanguageService } from './services/language';



@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule], 
  templateUrl: './app.html',
})
export class AppComponent {
  // -------------------------------------------------------------------------
  // DEĞİŞKENLER
  // -------------------------------------------------------------------------
  isVerifying = false; // İşlem sürerken çift tıklamayı engelleyecek kilit
  
  userEmail = '';
  userPassword = '';
  isRobotChecked = false;
  
  // loginStep Takibi:
  // 0: Kayıt, 1: Login, 2: MFA Setup, 3: Recovery Codes, 4: MFA Verify, 5: Recovery Verify, 6: Dashboard
  // Eski "loginStep: number = 1;" yerine bu bloğu ekliyoruz:
  private _loginStep: number = 1;

  get loginStep(): number {
    return this._loginStep;
  }

  set loginStep(value: number) {
    if (value === 6) {
      console.error("DİKKAT! SİSTEM 6. AŞAMAYA (DASHBOARD) GEÇİRİLDİ!");
      console.trace("Bunu tetikleyen fonksiyon şurada:"); 
    }
    this._loginStep = value;
  }

  mfaCode = ''; 
  recoveryInputCode = ''; 
  loginMessage = ''; 
  qrCodeImageUrl = ''; 
  recoveryCodes: string[] = []; 
  userName = 'Ahmet'; 

  constructor(
    private http: HttpClient, 
    public themeService: ThemeService,
    public langService: LanguageService 
  ) { }

  // -------------------------------------------------------------------------
  // 1. ADIM: GİRİŞ BUTONU (E-POSTA VE ŞİFRE KONTROLÜ)
  // -------------------------------------------------------------------------
  onLoginClick() {
    if (!this.isRobotChecked) {
      this.loginMessage = this.langService.translate('error_robot');
      return;
    }

    const backendUrl = 'http://localhost:5032/api/Auth/login';
    const loginData = { email: this.userEmail, password: this.userPassword };

    this.http.post(backendUrl, loginData).subscribe({
      next: (cevap: any) => {
        // MFA Kurulumu gerekiyor mu kontrolü
        if (cevap.requiresMfaSetup || cevap.RequiresMfaSetup) { 
          const qrUrl = `http://localhost:5032/api/Auth/mfa-setup?email=${this.userEmail}`;
          
          this.http.post(qrUrl, {}).subscribe({
            next: (qrCevap: any) => {
              this.qrCodeImageUrl = qrCevap.QrCodeImage || qrCevap.qrCodeImage; 
              this.loginStep = 2; 
              this.loginMessage = ''; 
            }
          });
        } else {
          this.loginStep = 4; 
          this.loginMessage = ''; 
        }
      },
      error: () => {
        this.loginMessage = this.langService.translate('error_auth');
      }
    });
  }

  // -------------------------------------------------------------------------
  // 2. ADIM: MFA DOĞRULAMA (İLK KURULUM VEYA NORMAL GİRİŞ)
  // -------------------------------------------------------------------------
  // -------------------------------------------------------------------------
  // 2. ADIM: MFA DOĞRULAMA (İLK KURULUM)
  // -------------------------------------------------------------------------
  onVerifyClick() {
    // Eğer halihazırda bir istek atıldıysa ve cevap bekleniyorsa, durdur (Çift tıklama engeli)
    if (this.isVerifying) return; 
    
    this.isVerifying = true; // Kilidi kapat

    const backendUrl = 'http://localhost:5032/api/Auth/mfa-verify-setup';
    const verifyData = {
      email: this.userEmail,
      code: this.mfaCode.replace(/\s/g, '') 
    };

    this.http.post(backendUrl, verifyData).subscribe({
      next: (cevap: any) => {
        this.isVerifying = false; // Kilidi aç
        
        // GÜVENLİK AĞI: Eğer arka planda gecikmeli bir istek geldiyse ve biz 
        // çoktan 3. aşamaya geçtiysek, bu gecikmeli isteği tamamen yok say!
        if (this.loginStep !== 2) return;

        if (cevap.RecoveryCodes || cevap.recoveryCodes) {
          this.recoveryCodes = cevap.RecoveryCodes || cevap.recoveryCodes;
          this.loginStep = 3; 
        } else {
          this.loginStep = 6; 
        }
        this.loginMessage = '';
      },
      error: () => {
        this.isVerifying = false; // Kilidi aç
        this.loginMessage = this.langService.translate('error_mfa');
      }
    });
  }

  // -------------------------------------------------------------------------
  // 4. ADIM: NORMAL MFA İLE GİRİŞ (KURULUMDAN SONRAKİ STANDART GİRİŞ)
  // -------------------------------------------------------------------------
  // -------------------------------------------------------------------------
  // 4. ADIM: NORMAL MFA İLE GİRİŞ
  // -------------------------------------------------------------------------
  onNormalMfaVerify() {
    if (this.isVerifying) return; // Çift tıklamayı engelle
    this.isVerifying = true;      // Kilidi kapat

    const backendUrl = 'http://localhost:5032/api/Auth/mfa-verify-setup'; 
    const verifyData = {
      email: this.userEmail,
      code: this.mfaCode.replace(/\s/g, '') 
    };

    this.http.post(backendUrl, verifyData).subscribe({
      next: () => {
        this.isVerifying = false; // İşlem bitti kilidi aç
        this.loginStep = 6; 
        this.loginMessage = '';
      },
      error: () => {
        this.isVerifying = false; // Hata oldu kilidi aç
        this.loginMessage = this.langService.translate('error_mfa');
      }
    });
  }

  // -------------------------------------------------------------------------
  // 3. ADIM: KURTARMA KODUYLA GİRİŞ
  // -------------------------------------------------------------------------
  onRecoveryVerify() {
    const backendUrl = 'http://localhost:5032/api/Auth/verify-recovery-code';
    const recoveryData = {
      email: this.userEmail,
      code: this.recoveryInputCode.trim()
    };

    this.http.post(backendUrl, recoveryData).subscribe({
      next: () => {
        this.loginStep = 6; 
        this.loginMessage = '';
      },
      error: () => {
        this.loginMessage = this.langService.translate('error_recovery');
      }
    });
  }

  // -------------------------------------------------------------------------
  // 4. ADIM: KAYIT OLMA
  // -------------------------------------------------------------------------
  onRegisterClick() {
    const backendUrl = 'http://localhost:5032/api/Auth/register';
    const registerData = { email: this.userEmail, password: this.userPassword };

    this.http.post(backendUrl, registerData).subscribe({
      next: () => {
        alert(this.langService.translate('register_success'));
        this.loginStep = 1; 
        this.loginMessage = '';
      },
      error: () => {
        this.loginMessage = this.langService.translate('error_reg');
      }
    });
  }
  // -------------------------------------------------------------------------
  // 3. AŞAMADAN 6. AŞAMAYA GEÇİŞ (KAYDETTİM BUTONU)
  // -------------------------------------------------------------------------
  onSavedClick() {
    console.log("Kaydettim butonuna basıldı, Sisteme giriliyor...");
    this.loginStep = 6;
  }
}