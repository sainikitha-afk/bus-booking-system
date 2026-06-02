import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-ticket',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './ticket.html'
})
export class TicketComponent implements OnInit {
  ticket: any  = null;
  loading = true;
  error   = '';

  constructor(
    private api: ApiService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    const groupId = Number(localStorage.getItem('groupId'));
    if (!groupId) {
      this.router.navigate(['/buses']);
      return;
    }

    this.api.getGroupTicket(groupId).subscribe({
      next: (res: any) => {
        this.ticket  = res;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error   = 'Could not load ticket.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  printTicket() {
    window.print();
  }
}
