import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.html'
})
export class AdminComponent implements OnInit {
  activeTab = 'dashboard';
  stats: any = null;

  // Operators
  pendingOperators: any[] = [];
  allOperators: any[] = [];
  rejectReason = '';

  // Locations
  locations: any[] = [];
  newCity = '';

  // Routes
  routes: any[] = [];
  newRoute = { sourceId: 0, destinationId: 0 };

  // Platform fee
  fee: any = { feeType: 'Fixed', amount: 50 };

  // Ratings (GROUP BY + HAVING)
  topRatedBuses: any[]  = [];
  minRating = 4.0;
  minCount  = 1;
  ratingsLoading = false;

  loading = false;
  message = '';

  constructor(
    private api: ApiService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    const role = localStorage.getItem('userRole');
    if (role !== 'Admin') { this.router.navigate(['/']); return; }
    this.loadDashboard();
    this.loadLocations();
    this.loadRoutes();
    this.loadFee();
    this.loadPendingOperators();
    this.loadAllOperators();
  }

  loadDashboard() {
    this.api.getDashboard().subscribe({ next: (r: any) => this.stats = r });
  }

  loadPendingOperators() {
    this.api.getPendingOperators().subscribe({ next: (r: any) => this.pendingOperators = r });
  }

  loadAllOperators() {
    this.api.getAllOperators().subscribe({ next: (r: any) => this.allOperators = r });
  }

  loadLocations() {
    this.api.getLocations().subscribe({ next: (r: any) => this.locations = r });
  }

  loadRoutes() {
    this.api.getRoutes().subscribe({ next: (r: any) => this.routes = r });
  }

  loadFee() {
    this.api.getPlatformFee().subscribe({ next: (r: any) => this.fee = r });
  }

  approve(userId: number) {
    this.api.approveOperator(userId).subscribe({
      next: () => {
        this.flash('Operator approved!');
        this.loadPendingOperators(); this.loadAllOperators(); this.loadDashboard();
      }
    });
  }

  reject(userId: number) {
    const reason = prompt('Reason for rejection:') || 'Not approved';
    this.api.rejectOperator(userId, reason).subscribe({
      next: () => { this.flash('Operator rejected.'); this.loadPendingOperators(); this.loadAllOperators(); }
    });
  }

  addCity() {
    if (!this.newCity.trim()) return;
    this.api.addLocation({ cityName: this.newCity.trim() }).subscribe({
      next: () => { this.newCity = ''; this.loadLocations(); this.flash('City added!'); },
      error: (e) => this.flash(e.error, true)
    });
  }

  deleteCity(id: number) {
    if (!confirm('Delete this city?')) return;
    this.api.deleteLocation(id).subscribe({
      next: () => { this.loadLocations(); this.flash('City deleted.'); },
      error: (e) => this.flash(e.error, true)
    });
  }

  addRoute() {
    if (!this.newRoute.sourceId || !this.newRoute.destinationId) return;
    this.api.addRoute(this.newRoute).subscribe({
      next: () => { this.newRoute = { sourceId: 0, destinationId: 0 }; this.loadRoutes(); this.flash('Route created!'); },
      error: (e) => this.flash(e.error, true)
    });
  }

  deleteRoute(id: number) {
    if (!confirm('Delete this route?')) return;
    this.api.deleteRoute(id).subscribe({
      next: () => { this.loadRoutes(); this.flash('Route deleted.'); }
    });
  }

  saveFee() {
    this.api.updatePlatformFee(this.fee).subscribe({
      next: () => this.flash('Platform fee updated!')
    });
  }

  loadTopRated() {
    this.ratingsLoading = true;
    this.api.getTopRated(this.minRating, this.minCount).subscribe({
      next: (r: any) => {
        this.topRatedBuses  = r;
        this.ratingsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.ratingsLoading = false; this.cdr.detectChanges(); }
    });
  }

  starsDisplay(avg: number): string {
    const filled = Math.round(avg);
    return '★'.repeat(filled) + '☆'.repeat(5 - filled);
  }

  flash(msg: string, isError = false) {
    this.message = (isError ? '❌ ' : '✅ ') + msg;
    setTimeout(() => this.message = '', 3000);
  }
}
