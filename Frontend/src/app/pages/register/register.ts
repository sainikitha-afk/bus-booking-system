import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './register.html'
})
export class RegisterComponent {
  name = '';
  email = '';
  password = '';
  confirmPassword = '';
  role = 'Customer';
  error = '';
  loading = false;

  constructor(private api: ApiService, private router: Router) {}

  register() {
    this.error = '';
    if (!this.name || !this.email || !this.password) {
      this.error = 'All fields are required.';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }
    this.loading = true;
    this.api.register({ name: this.name, email: this.email, password: this.password, role: this.role })
      .subscribe({
        next: () => {
          this.loading = false;
          this.router.navigate(['/login']);
        },
        error: (err) => {
          this.loading = false;
          this.error = err.error || 'Registration failed. Try again.';
        }
      });
  }
}
