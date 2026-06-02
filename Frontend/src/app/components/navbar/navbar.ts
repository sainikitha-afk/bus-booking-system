import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.html'
})
export class NavbarComponent implements OnInit {
  userName = '';
  userRole = '';
  isLoggedIn = false;

  constructor(private router: Router) {
    // Refresh on every navigation so role-based links update immediately after login
    this.router.events.subscribe(e => {
      if (e instanceof NavigationEnd) this.refresh();
    });
  }

  ngOnInit() { this.refresh(); }

  refresh() {
    this.userName  = localStorage.getItem('userName') || '';
    this.userRole  = localStorage.getItem('userRole') || '';
    this.isLoggedIn = !!localStorage.getItem('userId');
  }

  logout() {
    localStorage.clear();
    this.router.navigate(['/']);
  }

  get dashboardLink(): string {
    if (this.userRole === 'Admin')    return '/admin';
    if (this.userRole === 'Operator') return '/operator';
    return '/buses';
  }
}
