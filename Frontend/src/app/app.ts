import { Component } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from './components/navbar/navbar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, NavbarComponent],
  template: `
    <app-navbar *ngIf="showNavbar"></app-navbar>
    <router-outlet></router-outlet>
  `
})
export class App {
  showNavbar = true;
  private hideOn = ['/', '/login', '/register'];

  constructor(private router: Router) {
    this.router.events.subscribe(e => {
      if (e instanceof NavigationEnd) {
        this.showNavbar = !this.hideOn.includes(e.urlAfterRedirects);
      }
    });
  }
}
