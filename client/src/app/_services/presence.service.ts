import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { User } from '../_models/user';
import { BehaviorSubject, take } from 'rxjs';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class PresenceService {
  hubUrl = environment.hubUrl;
  private hubConnection?: HubConnection;

  private onlineUsersSource = new BehaviorSubject<string[]>([]);
  onlineUsers$ = this.onlineUsersSource.asObservable();

  constructor(private toastr: ToastrService, private router: Router) {}

  createHubConnection(user: User) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + 'presence', {
        accessTokenFactory: () => user.token,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch(console.log);
    this.hubConnection.on('UserIsOnline', (userName) => {
      this.onlineUsers$.pipe(take(1)).subscribe({
        next: (userNames) => {
          this.onlineUsersSource.next([...userNames, userName]);
        },
      });
    });

    this.hubConnection.on('UserIsOffline', (userName) => {
      this.onlineUsers$.pipe(take(1)).subscribe({
        next: (userNames) =>
          this.onlineUsersSource.next(userNames.filter((x) => x !== userName)),
      });
    });

    this.hubConnection.on('GetOnlineUsers', (userNames) =>
      this.onlineUsersSource.next(userNames)
    );

    this.hubConnection.on('NewMessageReceived', ({ userName, knownAs }) => {
      this.toastr
        .info(knownAs + ' has sent you a new message! Click to see it')
        .onTap.pipe(take(1))
        .subscribe({
          next: () =>
            this.router.navigateByUrl('/members/' + userName + '?tab=Messages'),
        });
    });
  }

  stopHubConnection() {
    this.hubConnection?.stop().catch(console.log);
  }
}
