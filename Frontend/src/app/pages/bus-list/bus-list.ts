import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-bus-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './bus-list.html'
})
export class BusListComponent implements OnInit {
  buses: any[]         = [];
  filteredBuses: any[] = [];
  loading = true;
  error   = '';

  searchSource      = '';
  searchDestination = '';
  searchDate        = '';
  filterType        = '';
  cities: string[]  = [];

  // busId → { avgRating, totalReviews }
  ratingsMap: { [busId: number]: { avgRating: number; totalReviews: number } } = {};

  constructor(
    private api: ApiService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.searchSource      = localStorage.getItem('searchSource')      || '';
    this.searchDestination = localStorage.getItem('searchDestination') || '';
    this.searchDate        = localStorage.getItem('searchDate')        || '';

    this.api.getLocations().subscribe({
      next: (r: any) => { this.cities = r.map((l: any) => l.cityName); }
    });

    // Load all bus ratings (GROUP BY bus_id)
    this.api.getAllRatings().subscribe({
      next: (ratings: any) => {
        this.ratingsMap = {};
        (ratings as any[]).forEach(r => {
          this.ratingsMap[r.busId] = { avgRating: r.avgRating, totalReviews: r.totalReviews };
        });
        this.cdr.detectChanges();
      }
    });

    this.loadBuses();
  }

  loadBuses() {
    this.error   = '';
    this.loading = true;

    const hasFilter = this.searchSource.trim() || this.searchDestination.trim() || this.searchDate;

    const obs = hasFilter
      ? this.api.searchBuses(this.searchSource, this.searchDestination, this.searchDate)
      : this.api.getBuses();

    obs.subscribe({
      next: (res: any) => {
        this.buses = res;
        this.applyFilter();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.error   = hasFilter
          ? 'Search failed. Please try again.'
          : 'Could not load buses. Is the backend running?';
        this.cdr.detectChanges();
      }
    });
  }

  doSearch() {
    localStorage.setItem('searchSource',      this.searchSource);
    localStorage.setItem('searchDestination', this.searchDestination);
    localStorage.setItem('searchDate',        this.searchDate);
    this.loadBuses();
  }

  applyFilter() {
    this.filteredBuses = this.filterType
      ? this.buses.filter(b => b.busType?.toLowerCase().includes(this.filterType.toLowerCase()))
      : [...this.buses];
  }

  selectBus(bus: any) {
    localStorage.setItem('busId', bus.id);
    this.router.navigate(['/seat']);
  }

  getRating(busId: number) {
    return this.ratingsMap[busId] ?? null;
  }

  // Returns '★★★★☆' style string for a given average
  starsDisplay(avg: number): string {
    const filled = Math.round(avg);
    return '★'.repeat(filled) + '☆'.repeat(5 - filled);
  }

  busTypeClass(type: string): string {
    if (!type) return 'badge bg-secondary';
    const t = type.toLowerCase();
    if (t.includes('sleeper')) return 'badge bg-primary';
    if (t.includes('ac'))      return 'badge bg-info text-dark';
    if (t.includes('non-ac'))  return 'badge bg-secondary';
    return 'badge bg-secondary';
  }
}
