import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home';
import { LoginComponent } from './pages/login/login';
import { RegisterComponent } from './pages/register/register';
import { BusListComponent } from './pages/bus-list/bus-list';
import { SeatComponent } from './pages/seat/seat';
import { PaymentComponent } from './pages/payment/payment';
import { TicketComponent } from './pages/ticket/ticket';
import { BookingHistoryComponent } from './pages/booking-history/booking-history';
import { AdminComponent } from './pages/admin/admin';
import { OperatorComponent } from './pages/operator/operator';
import { FeedbackComponent } from './pages/feedback/feedback';

export const routes: Routes = [
  { path: '',         component: HomeComponent },
  { path: 'login',    component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'buses',    component: BusListComponent },
  { path: 'seat',     component: SeatComponent },
  { path: 'payment',  component: PaymentComponent },
  { path: 'ticket',   component: TicketComponent },
  { path: 'history',  component: BookingHistoryComponent },
  { path: 'feedback', component: FeedbackComponent },
  { path: 'admin',    component: AdminComponent },
  { path: 'operator', component: OperatorComponent },
  { path: '**',       redirectTo: '' }
];
