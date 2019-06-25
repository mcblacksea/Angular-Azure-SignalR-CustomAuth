import { Component, OnDestroy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { SubSink } from 'subsink';
import { SignalRService } from './signalr-service';
import { UsersService } from './users.service';
import { UserItem } from './user-item';
import * as _ from 'lodash';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnDestroy {

  private subs = new SubSink();
  private token: string = '';
  loggedInUserItem: UserItem | undefined;
  messages: string[] = [];
  isConnected: boolean = false;
  isDatabaseSeeded: boolean = false;
  isLoggedIn: boolean = false;
  users: UserItem[] = [];

  constructor(private signalRService: SignalRService, private usersService: UsersService) {
    this.subs.sink = signalRService.messageObservable$.subscribe(message => this.log(message));
  }

  async ngOnDestroy() {
    this.subs.unsubscribe();
    await this.signalRService.stopSignalR();
  }

  startSignalR() {
    if (this.loggedInUserItem && this.token) {
      this.signalRService.startSignalR(this.token).then(() => {
        this.isConnected = true;
        if (this.loggedInUserItem) {
          this.log(`SignalR started for user ${this.loggedInUserItem.userName}.`);
        }
      }).catch(err => {
        this.log(err);
      });
    } else {
      this.log('loggedInUserItem was undefined, can not regiester.');
    }
  }

  seedDatabase(): void {
    this.usersService.seedDatabase().subscribe(results => {
      this.isDatabaseSeeded = true;
      this.users = _.sortBy(results, ['userName']);
      this.log('Database seeded, users loaded.');
    }, err => {
      this.isDatabaseSeeded = false;
      this.users = [];
      this.log(err);
    });
  }

  login(userName: string, password: string): void {
    this.usersService.login(userName, password).subscribe(tokenItem => {
      this.isLoggedIn = true;
      this.token = tokenItem.token;
      const userItem = _.find(this.users, u => u.userName.toLowerCase() === userName.toLowerCase());
      if (userItem) {
        this.loggedInUserItem = userItem;
        this.log(`${userItem.userName} logged in.`);
      }
    }, err => {
      this.isLoggedIn = false;
      this.loggedInUserItem = undefined;
      this.log(err);
    });
  }

  sendToAllUsers(message: string): void {
    this.signalRService.sendToAllUsers(message).subscribe(() => { },
      err => {
        this.log(err);
      });
  }

  sendToUser(message: string, userId: string): void {
    this.signalRService.sendToUser(message, userId).subscribe(() => { },
      err => {
        this.log(err);
      });
  }

  private log(content: any) {
    if (content instanceof HttpErrorResponse || content instanceof Error) {
      this.messages.push(`Error: ${content.message}`);
    } else {
      this.messages.push(content);
    }
  }
}
