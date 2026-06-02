import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './payment.html'
})
export class PaymentComponent implements OnInit {
  groupId    = 0;
  seatNumbers: number[] = [];
  bus: any   = null;
  totalAmount = 0;

  cardNumber = '';
  cardHolder = '';
  expiry     = '';
  cvv        = '';

  loading = false;
  error   = '';

  constructor(
    private api: ApiService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.groupId     = Number(localStorage.getItem('groupId'));
    this.seatNumbers = JSON.parse(localStorage.getItem('seatNumbers') || '[]');
    const busId      = Number(localStorage.getItem('busId'));

    if (!this.groupId) {
      this.router.navigate(['/buses']);
      return;
    }

    this.api.getBus(busId).subscribe({
      next: (res: any) => {
        this.bus         = res;
        this.totalAmount = res.price * this.seatNumbers.length;
        this.cdr.detectChanges();
      }
    });
  }

  pay() {
    if (!this.cardNumber || !this.cardHolder || !this.expiry || !this.cvv) {
      this.error = 'Please fill in all payment details.';
      return;
    }
    this.loading = true;
    this.error   = '';

    this.api.payGroup(this.groupId).subscribe({
      next: () => {
        this.loading = false;
        this.cdr.detectChanges();
        this.router.navigate(['/ticket']);
      },
      error: (err) => {
        this.loading = false;
        this.error   = err.error?.message || err.error || 'Payment failed. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }
}
