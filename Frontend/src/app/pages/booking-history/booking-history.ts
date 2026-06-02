import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-booking-history',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './booking-history.html'
})
export class BookingHistoryComponent implements OnInit {
  bookings: any[] = [];
  loading = true;
  userId = 0;

  constructor(private api: ApiService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.userId = Number(localStorage.getItem('userId'));
    if (!this.userId) {
      this.router.navigate(['/login']);
      return;
    }
    this.api.getUserBookings(this.userId).subscribe({
      next: (res: any) => {
        this.bookings = res;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  viewTicket(groupId: number) {
    localStorage.setItem('groupId', groupId.toString());
    this.router.navigate(['/ticket']);
  }

  // In production: also check b.bus.travelDate < today before showing this button
  giveFeedback(busId: number, groupId: number) {
    localStorage.setItem('feedbackBusId',   busId.toString());
    localStorage.setItem('feedbackGroupId', groupId.toString());
    this.router.navigate(['/feedback']);
  }

  cancel(id: number) {
    if (!confirm('Cancel this booking?')) return;
    this.api.cancelBooking(id).subscribe({
      next: () => {
        const b = this.bookings.find(x => x.id === id);
        if (b) { b.status = 'Cancelled'; b.paymentStatus = 'Refund Initiated'; }
        this.cdr.detectChanges();
      }
    });
  }

  statusClass(status: string): string {
    if (status === 'Paid') return 'badge bg-success';
    if (status === 'Booked') return 'badge bg-primary';
    if (status === 'Cancelled') return 'badge bg-danger';
    return 'badge bg-secondary';
  }
}
