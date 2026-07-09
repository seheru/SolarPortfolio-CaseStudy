import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private currentLang: 'tr' | 'en' = 'tr';

  // Tüm anahtarlar HTML dosyanla %100 uyumludur.
  private translations: any = {
    tr: {
      // Step 0 & 1: Login/Register
      create_account: 'Hesap Oluştur',
      join_solar: 'Solar Portfolio dünyasına katılmak için kayıt olun',
      email_placeholder: 'ornekisim@mail.com.tr',
      password_placeholder: 'Güçlü bir şifre belirleyin',
      register: 'Kayıt Ol',
      has_account: 'Zaten hesabınız var mı',
      login_link: 'Giriş Yap',
      welcome: 'Hoş Geldiniz',
      login_desc: 'Devam etmek için lütfen mail adresinizi ve şifrenizi girin',
      pass_placeholder: 'Şifre Gir',
      robot_check: 'Robot Olmadığınızı Kanıtlayın',
      login_btn: 'İleri',
      copyright: 'Justech Yazılım ve Teknoloji Danışmanlık A.Ş. © Copyright 2026',
      copyright_2025: 'Justech Yazılım ve Teknoloji Danışmanlık A.Ş. © Copyright 2025',
      
      // Step 2: MFA Setup
      mfa_setup_title: 'İki Faktörlü Doğrulama Kurulumu',
      mfa_step1: '1. Aşağıdaki QR kodu uygulamanız ile tarayın.',
      mfa_step2: '2. Uygulamanızdan 6 haneli kodu girin:',
      verify_activate: 'Doğrula ve Etkinleştir',

      // Step 3: Recovery Codes
      recovery_title: 'Kurtarma Kodları Oluşturuldu',
      recovery_desc: 'Bu kodlar, hesabınıza erişiminizi geri kazanmanız için kullanılabilir.',
      saved_btn: 'Kaydettim',

      // Step 4 & 5: MFA Verify & Recovery Verify
      hello: 'Merhaba', // <-- BURASI DÜZELTİLDİ
      enter_code: 'Giriş yapmak için doğrulama kodunuzu girin',
      timer_text: 'Kodun yenilenmesine:',
      verify_login: 'Doğrula ve Giriş Yap',
      emergency_pass: 'Yerel Şifre ile Giriş Yap (Acil Durum)',
      no_access: "Authenticator'a Erişemiyorum",
      recovery_login_title: 'Kurtarma Kodu ile Giriş',
      recovery_login_desc: 'Lütfen yedek kurtarma kodlarınızdan birini girin',

      // Step 6: Success
      success_title: 'SİSTEME GİRİLDİ',
      redirect_text: 'Justech Solar Portfolio Paneline Yönlendiriliyorsunuz...',
      register_success: 'Kayıt Başarılı! Şimdi giriş yapabilirsiniz.',

      // HATA MESAJLARI
      error_robot: "HATA: Lütfen robot olmadığınızı kanıtlayın!",
      error_auth: "HATA: E-posta veya şifre hatalı!",
      error_mfa: "HATA: Girdiğiniz kod yanlış veya süresi dolmuş!",
      error_recovery: "HATA: Geçersiz veya kullanılmış kurtarma kodu!",
      error_reg: "HATA: Kayıt olunamadı!"
    },
    en: {
      // Step 0 & 1: Login/Register
      create_account: 'Create Account',
      join_solar: 'Join the Solar Portfolio world by registering',
      email_placeholder: 'example@mail.com',
      password_placeholder: 'Set a strong password',
      register: 'Register',
      has_account: 'Already have an account?',
      login_link: 'Login',
      welcome: 'Welcome',
      login_desc: 'Please enter your email and password to continue',
      pass_placeholder: 'Enter Password',
      robot_check: "Prove You're Not a Robot",
      login_btn: 'Next',
      copyright: 'Justech Software and Tech Consulting Inc. © Copyright 2026',
      copyright_2025: 'Justech Software and Tech Consulting Inc. © Copyright 2025',

      // Step 2: MFA Setup
      mfa_setup_title: 'Two-Factor Authentication Setup',
      mfa_step1: '1. Scan the QR code below with your app.',
      mfa_step2: '2. Enter the 6-digit code from your app:',
      verify_activate: 'Verify and Activate',

      // Step 3: Recovery Codes
      recovery_title: 'Recovery Codes Generated',
      recovery_desc: 'These codes can be used to regain access to your account.',
      saved_btn: 'I Saved Them',

      // Step 4 & 5: MFA Verify & Recovery Verify
      hello: 'Hello', // <-- BURASI DÜZELTİLDİ
      enter_code: 'Enter your verification code to login',
      timer_text: 'Code expires in:',
      verify_login: 'Verify and Login',
      emergency_pass: 'Login with Local Password (Emergency)',
      no_access: "I Can't Access Authenticator",
      recovery_login_title: 'Recovery Code Login',
      recovery_login_desc: 'Please enter one of your backup recovery codes',

      // Step 6: Success
      success_title: 'SYSTEM LOGGED IN',
      redirect_text: 'Redirecting to Justech Solar Portfolio Dashboard...',
      register_success: 'Registration Successful! You can now login.',

      // ERROR MESSAGES
      error_robot: "ERROR: Please prove you are not a robot!",
      error_auth: "ERROR: Invalid email or password!",
      error_mfa: "ERROR: The code is invalid or expired!",
      error_recovery: "ERROR: Invalid or used recovery code!",
      error_reg: "ERROR: Registration failed!"
    }
  };

  constructor() {
    const savedLang = localStorage.getItem('lang') as 'tr' | 'en';
    if (savedLang) {
      this.currentLang = savedLang;
    }
  }

  toggleLang() {
    this.currentLang = this.currentLang === 'tr' ? 'en' : 'tr';
    localStorage.setItem('lang', this.currentLang);
  }

  translate(key: string): string {
    return this.translations[this.currentLang][key] || key; // Bulamazsa key'in kendisini döner
  }

  getLang() {
    return this.currentLang;
  }
}