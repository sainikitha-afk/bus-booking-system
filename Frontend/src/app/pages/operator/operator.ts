import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-operator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './operator.html'
})
export class OperatorComponent implements OnInit {
  activeTab = 'overview';
  profile: any = null;
  revenue: any = null;
  buses: any[] = [];
  routes: any[] = [];
  userId = 0;
  isApproved = false;

  // Add bus form
  newBus: any = {
    name: '', vehicleRegNumber: '', price: 0, busType: 'AC Seater',
    travelDate: '', departureTime: '', arrivalTime: '',
    totalSeats: 40, layoutType: '2x2', isWomenOnly: false, routeId: 0
  };

  // Add route form
  selectedRouteId = 0;
  officeAddress = '';

  allRoutes: any[] = [];
  message = '';
  loading = false;

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit() {
    const role = localStorage.getItem('userRole');
    if (role !== 'Operator') { this.router.navigate(['/']); return; }
    this.userId = Number(localStorage.getItem('userId'));
    this.loadProfile();
    this.loadRevenue();
    this.loadBuses();
    this.api.getRoutes().subscribe({ next: (r: any) => this.allRoutes = r });
  }

  loadProfile() {
    this.api.getOperatorProfile(this.userId).subscribe({
      next: (r: any) => {
        this.profile = r;
        this.isApproved = r.profile?.isApproved ?? false;
        this.routes = r.routes ?? [];
      }
    });
  }

  loadRevenue() {
    this.api.getOperatorRevenue(this.userId).subscribe({
      next: (r: any) => this.revenue = r
    });
  }

  loadBuses() {
    this.api.getOperatorBuses(this.userId).subscribe({
      next: (r: any) => this.buses = r
    });
  }

  addRoute() {
    if (!this.selectedRouteId || !this.officeAddress) {
      this.flash('Select a route and enter your office address.', true); return;
    }
    this.api.addOperatorRoute({
      operatorUserId: this.userId,
      routeId: this.selectedRouteId,
      officeAddress: this.officeAddress
    }).subscribe({
      next: () => {
        this.selectedRouteId = 0; this.officeAddress = '';
        this.loadProfile(); this.flash('Route added!');
      },
      error: (e) => this.flash(typeof e.error === 'string' ? e.error : e.error?.title || 'Failed to add route.', true)
    });
  }

  addBus() {
    if (!this.newBus.name || !this.newBus.vehicleRegNumber || !this.newBus.routeId) {
      this.flash('Fill in bus name, registration number, and select a route.', true); return;
    }
    const payload = {
      ...this.newBus,
      operatorUserId: this.userId,
      travelDate: this.newBus.travelDate || null
    };
    this.api.addBus(payload).subscribe({
      next: () => {
        this.newBus = { name: '', vehicleRegNumber: '', price: 0, busType: 'AC Seater', travelDate: '', departureTime: '', arrivalTime: '', totalSeats: 40, layoutType: '2x2', isWomenOnly: false, routeId: 0 };
        this.loadBuses(); this.flash('Bus added successfully!');
      },
      error: (e) => {
        const msg = typeof e.error === 'string' ? e.error
          : e.error?.title || e.error?.message || JSON.stringify(e.error) || 'Failed to add bus.';
        this.flash(msg, true);
      }
    });
  }

  toggleBus(bus: any) {
    const action = bus.isActive ? this.api.disableBus(bus.id) : this.api.enableBus(bus.id);
    action.subscribe({
      next: () => { bus.isActive = !bus.isActive; this.flash(bus.isActive ? 'Bus enabled.' : 'Bus disabled.'); }
    });
  }

  deleteBus(id: number) {
    if (!confirm('Permanently remove this bus?')) return;
    this.api.deleteBus(id).subscribe({
      next: () => { this.buses = this.buses.filter(b => b.id !== id); this.flash('Bus removed.'); }
    });
  }

  flash(msg: string, isError = false) {
    this.message = (isError ? '❌ ' : '✅ ') + msg;
    setTimeout(() => this.message = '', 3000);
  }

  operatedRouteIds(): number[] {
    return (this.routes || []).map((r: any) => r.routeId);
  }

  availableRoutes(): any[] {
    const operated = this.operatedRouteIds();
    return this.allRoutes.filter(r => !operated.includes(r.id));
  }
}
