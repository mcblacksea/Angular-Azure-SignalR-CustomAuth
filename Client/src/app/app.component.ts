import { Component, HostListener } from '@angular/core';
import { SignalRService } from './signalr-service';
import { UsersService } from './users.service';
import { UserItem } from './UserItem';
import { HttpErrorResponse } from '@angular/common/http';
import * as _ from 'lodash';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {

  loggedInUserItem: UserItem | undefined;
  messages: string[] = [];
  isConnected: boolean = false;
  isDatabaseSeeded: boolean = false;
  isLoggedIn: boolean = false;
  users: UserItem[] = [];
  private _token: string = '';

  constructor(private _signalRService: SignalRService, private _usersService: UsersService) { }

  startSignalRClient() {
    if (this.loggedInUserItem && this._token) {

      this._signalRService.getSignalRConnectionInfo(this._token).subscribe(results => {
        this._signalRService.init(results);
        this._signalRService.messages.subscribe(message => {
          this.log(message);
        });
        this.isConnected = true;
        if (this.loggedInUserItem) {
          this.log(`SignalR started for user ${this.loggedInUserItem.userName}.`)
        }
      }, err => {
        this.log(err);
      });
    } else {
      this.log('loggedInUserItem was undefined, can not regiester.');
    }
  }

  seedDatabase(): void {
    this._usersService.seedDatabase().subscribe(results => {
      this.isDatabaseSeeded = true;
      this.users = _.sortBy(results, ['userName']);
      this.log('Database seeded, users loaded.')
    }, err => {
      this.isDatabaseSeeded = false;
      this.users = [];
      this.log(err);
    });
  }

  login(userName: string, password: string): void {
    this._usersService.login(userName, password).subscribe(tokenItem => {
      this.isLoggedIn = true;
      this._token = tokenItem.token;
      let userItem = _.find(this.users, u => u.userName === userName);
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
    this._signalRService.sendToAllUsers(message).subscribe(() => { },
      err => {
        this.log(err);
      });
  }

  sendToUser(message: string, userId: string): void {
    this._signalRService.sendToUser(message, userId).subscribe(() => { },
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
