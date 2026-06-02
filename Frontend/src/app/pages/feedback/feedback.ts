import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-feedback',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './feedback.html',
  styleUrls: ['./feedback.css']
})
export class FeedbackComponent implements OnInit {
  bus: any      = null;
  groupId       = 0;
  busId         = 0;
  userId        = 0;

  rating        = 0;
  hoveredRating = 0;
  message       = '';

  loading   = false;
  submitted = false;
  alreadySubmitted = false;
  error     = '';

  constructor(
    private api: ApiService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.userId  = Number(localStorage.getItem('userId'));
    this.busId   = Number(localStorage.getItem('feedbackBusId'));
    this.groupId = Number(localStorage.getItem('feedbackGroupId'));

    if (!this.userId || !this.busId) {
      this.router.navigate(['/history']);
      return;
    }

    // Load bus info for display
    this.api.getBus(this.busId).subscribe({
      next: (res: any) => { this.bus = res; this.cdr.detectChanges(); }
    });

    // Check if feedback already submitted
    if (this.groupId) {
      this.api.checkFeedback(this.groupId).subscribe({
        next: (res: any) => {
          this.alreadySubmitted = res.submitted;
          this.cdr.detectChanges();
        }
      });
    }
  }

  setRating(star: number) { this.rating = star; }
  hoverRating(star: number) { this.hoveredRating = star; }
  clearHover() { this.hoveredRating = 0; }

  getStarClass(star: number): string {
    const active = this.hoveredRating || this.rating;
    return star <= active ? 'star filled' : 'star';
  }

  submit() {
    if (this.rating === 0) {
      this.error = 'Please select a star rating.';
      return;
    }
    this.loading = true;
    this.error   = '';

    this.api.submitFeedback({
      userId:         this.userId,
      busId:          this.busId,
      bookingGroupId: this.groupId || null,
      rating:         this.rating,
      message:        this.message.trim() || null
    }).subscribe({
      next: () => {
        this.submitted = true;
        this.loading   = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        this.error   = err.status === 409
          ? 'You have already submitted feedback for this booking.'
          : (err.error?.message || 'Could not submit feedback. Please try again.');
        this.cdr.detectChanges();
      }
    });
  }
}
