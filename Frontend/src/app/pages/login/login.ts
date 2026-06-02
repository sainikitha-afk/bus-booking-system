import { Component } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './login.html'
})
export class LoginComponent {
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(private api: ApiService, private router: Router) {}

  login() {
    this.error = '';
    if (!this.email || !this.password) {
      this.error = 'Please enter email and password.';
      return;
    }
    this.loading = true;
    this.api.login({ email: this.email, password: this.password })
      .subscribe({
        next: (res: any) => {
          this.loading = false;
          localStorage.setItem('userId', res.id);
          localStorage.setItem('userName', res.name);
          localStorage.setItem('userRole', res.role);
          this.router.navigate(['/buses']);
        },
        error: () => {
          this.loading = false;
          this.error = 'Invalid email or password.';
        }
      });
  }
}
