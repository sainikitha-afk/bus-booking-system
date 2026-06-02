import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { lastValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';

interface SeatInfo {
  seatNumber: number;
  status: 'available' | 'booked' | 'locked';
  gender?: string;
}

@Component({
  selector: 'app-seat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './seat.html',
  styleUrls: ['./seat.css']
})
export class SeatComponent implements OnInit, OnDestroy {

  bus: any = null;
  seats: SeatInfo[] = [];
  seatRows: SeatInfo[][] = [];

  selectedSeat: number | null = null;
  selectedSeats: number[] = [];

  passengers: any[] = [];

  userId = 0;
  busId = 0;
  travelDate = '';

  loading = true;
  locking = false;
  error = '';

  lockTimer = 0;
  timerInterval: any = null;

  passengerName = '';
  age: number | null = null;
  gender = '';
  showPassengerForm = false;

  get seatsPerRow(): number {
    const layout = this.bus?.layoutType?.toLowerCase() || '2x2';
    if (layout === '2x3') return 5;
    if (layout === 'sleeper') return 2;
    return 4;
  }

  get leftCount(): number {
    const layout = this.bus?.layoutType?.toLowerCase() || '2x2';
    if (layout === 'sleeper') return 1;
    return 2;
  }

  get availableCount(): number {
    return this.seats.filter(s => s.status === 'available').length;
  }

  constructor(private api: ApiService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.userId = Number(localStorage.getItem('userId'));
    this.busId  = Number(localStorage.getItem('busId'));
    this.travelDate = localStorage.getItem('searchDate') || '';

    console.log('[Seat] userId:', this.userId, 'busId:', this.busId);

    if (!this.userId || !this.busId) {
      console.warn('[Seat] Missing userId or busId — redirecting');
      this.router.navigate(['/buses']);
      return;
    }

    this.loadBusAndSeats();
  }

  loadBusAndSeats() {
    console.log('[Seat] Fetching bus', this.busId);

    this.api.getBus(this.busId).subscribe({
      next: (res: any) => {
        console.log('[Seat] Bus loaded:', res);
        this.bus = res;
        this.buildSeatGrid(res.totalSeats || 40);

        console.log('[Seat] Fetching booked seats...');
        this.api.getBookedSeats(this.busId).subscribe({
          next: (booked: any) => {
            console.log('[Seat] Booked seats:', booked);
            (booked ?? []).forEach((b: any) => {
              const seat = this.seats.find(s => s.seatNumber === b.seatNumber);
              if (seat) {
                seat.status = b.status?.toLowerCase() === 'locked' ? 'locked' : 'booked';
                seat.gender = b.gender;
              }
            });
            this.generateRows();
            this.loading = false;
            this.cdr.detectChanges();
            console.log('[Seat] Done — rows:', this.seatRows.length);
          },
          error: (err) => {
            console.error('[Seat] getBookedSeats error:', err);
            this.generateRows();
            this.loading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        console.error('[Seat] getBus error:', err);
        this.error = 'Could not load bus details. Is the backend running?';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  buildSeatGrid(total: number) {
    this.seats = Array.from({ length: total }, (_, i) => ({
      seatNumber: i + 1,
      status: 'available'
    }));
  }

  generateRows() {
    this.seatRows = [];
    const perRow = this.seatsPerRow;
    for (let i = 0; i < this.seats.length; i += perRow) {
      this.seatRows.push(this.seats.slice(i, i + perRow));
    }
  }

  selectSeat(seat: SeatInfo) {
    if (seat.status === 'booked' || seat.status === 'locked') return;

    const index = this.selectedSeats.indexOf(seat.seatNumber);

    if (index > -1) {
      this.selectedSeats.splice(index, 1);
    } else {
      this.selectedSeats.push(seat.seatNumber);
    }

    this.selectedSeat = this.selectedSeats.length > 0 
      ? this.selectedSeats[this.selectedSeats.length - 1] 
      : null;
    
    const updatedPassengers = this.selectedSeats.map(seatNumber => {
      const existing = this.passengers.find(p => p.seatNumber === seatNumber);

      return existing || {
        seatNumber: seatNumber,
        name: '',
        age: null,
        gender: ''
      };
    });

    this.passengers = updatedPassengers;

    this.showPassengerForm = this.selectedSeats.length > 0;
    this.error = '';

  }

  seatTooltip(seat: SeatInfo): string {
    if (seat.status === 'available') return `Seat ${seat.seatNumber} — Available`;
    if (seat.status === 'locked') return `Seat ${seat.seatNumber} — Being booked`;
    if (seat.status === 'booked') {
      return seat.gender ? `Seat ${seat.seatNumber} — Booked (${seat.gender})` : `Seat ${seat.seatNumber} — Booked`;
    }
    return `Seat ${seat.seatNumber}`;
  }

  // ✅ UPDATED
  lockSeat() {
    if (this.passengers.length === 0) {
      this.error = 'Please select at least one seat';
      return;
    }

    for (let p of this.passengers) {
      if (!p.name || !p.age || !p.gender) {
        this.error = 'Please fill all passenger details';
        return;
      }
    }

    this.locking = true;
    this.error = '';

    const lockRequests = this.passengers.map(p =>
      lastValueFrom(this.api.lockSeat({
        userId: this.userId,
        busId: this.busId,
        seatNumber: p.seatNumber
      }))
    );

    Promise.all(lockRequests)
      .then((locks: any[]) => {
        localStorage.setItem('locks', JSON.stringify(locks));
        this.proceedToBook();
      })
      .catch(() => {
        this.error = 'Some seats could not be locked';
        this.locking = false;
      });
  }

  proceedToBook() {
    const bookingRequests = this.passengers.map(p =>
      lastValueFrom(this.api.bookSeat({
        userId: this.userId,
        busId: this.busId,
        seatNumber: p.seatNumber,
        passengerName: p.name,
        age: p.age,
        gender: p.gender
      }))
    );

    Promise.all(bookingRequests)
      .then((res: any[]) => {
        const bookingIds: number[] = res.map(r => r.id);

        this.api.finalizeGroup({
          userId: this.userId,
          busId: this.busId,
          bookingIds
        }).subscribe({
          next: (gRes: any) => {
            localStorage.setItem('groupId',      gRes.groupId.toString());
            localStorage.setItem('seatNumbers',  JSON.stringify(this.passengers.map(p => p.seatNumber)));
            localStorage.setItem('seatCount',    this.passengers.length.toString());
            this.router.navigate(['/payment']);
          },
          error: () => {
            this.error = 'Could not finalise booking group. Please try again.';
            this.locking = false;
            this.cdr.detectChanges();
          }
        });
      })
      .catch(() => {
        this.error = 'Booking failed for one or more seats.';
        this.locking = false;
        this.cdr.detectChanges();
      });
  }

  startLockCountdown(seconds: number) {
    this.lockTimer = seconds;
    this.timerInterval = setInterval(() => {
      this.lockTimer--;
      if (this.lockTimer <= 0) clearInterval(this.timerInterval);
    }, 1000);
  }

  ngOnDestroy() {
    if (this.timerInterval) clearInterval(this.timerInterval);
  }
}