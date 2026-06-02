import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './home.html',
  styleUrls: ['./home.css']
})
export class HomeComponent implements OnInit {
  source = '';
  destination = '';
  date = '';
  today = new Date().toISOString().split('T')[0];
  cities: string[] = [];

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit() {
    this.api.getLocations().subscribe({
      next: (res: any) => this.cities = res.map((l: any) => l.cityName)
    });
  }

  swapLocations() {
    [this.source, this.destination] = [this.destination, this.source];
  }

  search() {
    localStorage.setItem('searchSource', this.source);
    localStorage.setItem('searchDestination', this.destination);
    localStorage.setItem('searchDate', this.date);
    const isLoggedIn = !!localStorage.getItem('userId');
    this.router.navigate([isLoggedIn ? '/buses' : '/login']);
  }
}
