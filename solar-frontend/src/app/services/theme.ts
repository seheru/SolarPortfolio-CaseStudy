import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root' // Bu satır çok önemli, Angular'ın servisi tanımasını sağlar
})
export class ThemeService {
  private darkMode = false;

  constructor() {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      this.enableDarkMode();
    }
  }

  toggleTheme() {
    this.darkMode ? this.disableDarkMode() : this.enableDarkMode();
  }

  private enableDarkMode() {
    document.documentElement.classList.add('dark');
    localStorage.setItem('theme', 'dark');
    this.darkMode = true;
  }

  private disableDarkMode() {
    document.documentElement.classList.remove('dark');
    localStorage.setItem('theme', 'light');
    this.darkMode = false;
  }

  isDarkMode() {
    return this.darkMode;
  }
}