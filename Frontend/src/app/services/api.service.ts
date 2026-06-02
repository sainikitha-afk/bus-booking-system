import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  baseUrl = 'http://localhost:5163/api';

  constructor(private http: HttpClient) {}

  // ── Auth ──────────────────────────────────────────
  login(data: any)    { return this.http.post(`${this.baseUrl}/User/login`, data); }
  register(data: any) { return this.http.post(`${this.baseUrl}/User/register`, data); }

  // ── Locations (admin) ────────────────────────────
  getLocations()              { return this.http.get(`${this.baseUrl}/Location`); }
  addLocation(data: any)      { return this.http.post(`${this.baseUrl}/Location`, data); }
  deleteLocation(id: number)  { return this.http.delete(`${this.baseUrl}/Location/${id}`); }

  // ── Routes (admin) ───────────────────────────────
  getRoutes()              { return this.http.get(`${this.baseUrl}/Route`); }
  addRoute(data: any)      { return this.http.post(`${this.baseUrl}/Route`, data); }
  deleteRoute(id: number)  { return this.http.delete(`${this.baseUrl}/Route/${id}`); }

  // ── Buses ────────────────────────────────────────
  getBuses()  { return this.http.get(`${this.baseUrl}/Bus`); }
  getBus(id: number) { return this.http.get(`${this.baseUrl}/Bus/${id}`); }

  searchBuses(source: string, destination: string, date: string) {
    let params = new HttpParams();
    if (source.trim())      params = params.set('source', source.trim());
    if (destination.trim()) params = params.set('destination', destination.trim());
    if (date)               params = params.set('date', date);
    return this.http.get(`${this.baseUrl}/Bus/search`, { params });
  }

  addBus(data: any)          { return this.http.post(`${this.baseUrl}/Bus`, data); }
  updateBus(id: number, data: any) { return this.http.put(`${this.baseUrl}/Bus/${id}`, data); }
  disableBus(id: number)     { return this.http.put(`${this.baseUrl}/Bus/${id}/disable`, {}); }
  enableBus(id: number)      { return this.http.put(`${this.baseUrl}/Bus/${id}/enable`, {}); }
  deleteBus(id: number)      { return this.http.delete(`${this.baseUrl}/Bus/${id}`); }

  // ── Seats & Bookings ─────────────────────────────
  getBookedSeats(busId: number)   { return this.http.get(`${this.baseUrl}/Booking/seats/${busId}`); }
  lockSeat(data: any)             { return this.http.post(`${this.baseUrl}/Booking/lock`, data); }
  bookSeat(data: any)             { return this.http.post(`${this.baseUrl}/Booking`, data); }
  finalizeGroup(data: any)        { return this.http.post(`${this.baseUrl}/Booking/finalize-group`, data); }
  payGroup(groupId: number)       { return this.http.post(`${this.baseUrl}/Booking/pay-group/${groupId}`, {}); }
  getGroupTicket(groupId: number) { return this.http.get(`${this.baseUrl}/Booking/ticket-group/${groupId}`); }
  getUserBookings(userId: number) { return this.http.get(`${this.baseUrl}/Booking/user/${userId}`); }
  cancelBooking(id: number)       { return this.http.post(`${this.baseUrl}/Booking/cancel/${id}`, {}); }
  pay(id: number)                 { return this.http.post(`${this.baseUrl}/Booking/pay/${id}`, {}); }
  getTicket(id: number)           { return this.http.get(`${this.baseUrl}/Booking/ticket/${id}`); }

  // ── Operator ─────────────────────────────────────
  registerOperator(data: any)          { return this.http.post(`${this.baseUrl}/Operator/register`, data); }
  getAllOperators()                     { return this.http.get(`${this.baseUrl}/Operator`); }
  getOperatorProfile(userId: number)   { return this.http.get(`${this.baseUrl}/Operator/${userId}`); }
  approveOperator(userId: number)      { return this.http.put(`${this.baseUrl}/Operator/approve/${userId}`, {}); }
  rejectOperator(userId: number, reason: string) {
    return this.http.put(`${this.baseUrl}/Operator/reject/${userId}`, { reason });
  }
  addOperatorRoute(data: any)          { return this.http.post(`${this.baseUrl}/Operator/route`, data); }
  getOperatorBuses(userId: number)     { return this.http.get(`${this.baseUrl}/Operator/${userId}/buses`); }
  getOperatorRevenue(userId: number)   { return this.http.get(`${this.baseUrl}/Operator/${userId}/revenue`); }

  // ── Feedback ─────────────────────────────────────
  submitFeedback(data: any)        { return this.http.post(`${this.baseUrl}/Feedback`, data); }
  checkFeedback(groupId: number)   { return this.http.get(`${this.baseUrl}/Feedback/check/${groupId}`); }
  getBusReviews(busId: number)     { return this.http.get(`${this.baseUrl}/Feedback/bus/${busId}`); }
  getAllRatings()                   { return this.http.get(`${this.baseUrl}/Feedback/ratings`); }
  getTopRated(minRating: number, minCount: number) {
    return this.http.get(`${this.baseUrl}/Feedback/top-rated?minRating=${minRating}&minCount=${minCount}`);
  }

  // ── Admin ────────────────────────────────────────
  getDashboard()         { return this.http.get(`${this.baseUrl}/Admin/dashboard`); }
  getPlatformFee()       { return this.http.get(`${this.baseUrl}/Admin/platform-fee`); }
  updatePlatformFee(data: any) { return this.http.put(`${this.baseUrl}/Admin/platform-fee`, data); }
  getPendingOperators()  { return this.http.get(`${this.baseUrl}/Admin/pending-operators`); }
}
