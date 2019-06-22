import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import * as signalR from '@aspnet/signalr';
import { HubConnection } from '@aspnet/signalr';
import { SignalRConnectionInfo } from './SignalRConnectionInfo';
import { Observable } from 'rxjs';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {

  private _token: string = '';
  private readonly _baseUrl: string = 'http://localhost:7071/api/';
  private hubConnection: HubConnection | undefined;
  messages: Subject<string> = new Subject();

  constructor(private _httpClient: HttpClient) { }

  getSignalRConnectionInfo(token: string): Observable<SignalRConnectionInfo> {
    this._token = token;
    const requestUrl: string = `${this._baseUrl}SignalRConnection`;

    return this._httpClient.get<SignalRConnectionInfo>(
      requestUrl, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this._token}` })
      });
  }

  init(signalRConnectionInfo: SignalRConnectionInfo): void {

    const options: signalR.IHttpConnectionOptions = {
      accessTokenFactory: () => signalRConnectionInfo.accessToken
    };

    this.messages.next('User registered with SignalR.');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(signalRConnectionInfo.url, options)
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.serverTimeoutInMilliseconds = 300000;  // five minutes

    this.hubConnection.start()
      .then(() => {
        this.messages.next('Hub Connection Started.');
      }).catch(
        err => {
          this.messages.next(`Hub Unable To Start Connection. ${err.toString()}`);
        }
      );

    this.hubConnection.on('notify', (data: any) => {
      this.messages.next(data);
    });

    this.hubConnection.onclose((error) => {
      // FYI:  this will be invoked when the serverTimeoutInMilliseconds is reached without any activity.
      //
      // TODO don't like how this works, need much more robust code
      // I think this will be application dependent.
      // Each app will probably handle signalR connection issues differently
      //   along with the user experience and affect on the app without signalR
      if (this.hubConnection) {
        this.hubConnection.start();
      }
      console.error(`Something went wrong: ${error}`);
    });
  }

  sendToAllUsers(message: string): Observable<object> {
    const requestUrl = `${this._baseUrl}SendMessageToAllUsers`;

    message = 'Sent To All Users: '.concat(message);

    return this._httpClient.post(
      requestUrl,
      message, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this._token}` })
      });
  }

  sendToUser(message: string, userId: string): Observable<object> {
    const requestUrl = `${this._baseUrl}SendMessageToUser/${userId}`;

    message = `Sent Only To ${userId}: `.concat(message);

    return this._httpClient.post(
      requestUrl,
      message, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this._token}` })
      });
  }
}
