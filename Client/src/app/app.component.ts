import { Component } from '@angular/core';
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
  isRegistered: boolean = false;
  isDatabaseSeeded: boolean = false;
  isLoggedIn: boolean = false;
  users: UserItem[] = [];
  private _token: string = '';

  constructor(private _signalRService: SignalRService, private _usersService: UsersService) { }

  registerUser() {
    if (this.loggedInUserItem && this._token) {

      this._signalRService.getSignalRConnectionInfo(this._token).subscribe(results => {
        this._signalRService.init(results);
        this._signalRService.messages.subscribe(message => {
          this.messages.push(message);
        });
        this.isRegistered = true;
        if (this.loggedInUserItem) {
          this.messages.push(`SignalR started for user ${this.loggedInUserItem.userName}.`)
        }
      }, err => {
        if (err instanceof HttpErrorResponse) {
          this.logError(err);
        } else {
          this.logMessage(`Error: ${err.message}.`);
        }
      });
    } else {
      this.logMessage('loggedInUserItem was undefined, can not regiester.');
    }
  }

  seedDatabase(): void {
    this._usersService.seedDatabase().subscribe(results => {
      this.isDatabaseSeeded = true;
      this.users = results;
      this.logMessage('Database seeded')
    }, err => {
      this.isDatabaseSeeded = false;
      this.users = [];
      this.logError(err);
    });
  }

  login(userName: string, password: string): void {
    this._usersService.login(userName, password).subscribe(tokenItem => {
      this.isLoggedIn = true;
      this._token = tokenItem.token;
      let userItem = _.find(this.users, u => u.userName === userName);
      if (userItem) {
        this.loggedInUserItem = userItem;
        this.logMessage(`${userItem.userName} logged in.`);
      }
    }, err => {
      this.isLoggedIn = false;
      this.loggedInUserItem = undefined;
      this.logError(err);
    });
  }

  sendToAllUsers(message: string): void {
    this._signalRService.sendToAllUsers(message).subscribe(() => {}, 
    err => {
      this.logError(err);
    });
  }

  sendToUser(message: string, userId: string): void {
    this._signalRService.sendToUser(message, userId).subscribe(() => {},
    err => {
      this.logError(err);
    });
  }

  private logError(err: HttpErrorResponse): void {
    this.messages.push(`Error: ${err.message}`);
  }

  private logMessage(message: string) {
    this.messages.push(message);
  }

}
