import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { HubConnection } from '@aspnet/signalr';
import * as signalR from '@aspnet/signalr';
import { Observable } from "rxjs";
import { SignalRConnectionInfo } from "./SignalRConnectionInfo";
import { Subject } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class SignalRService {

  private _token: string = '';
  private readonly _baseUrl: string = "http://localhost:7071/api/";
  private hubConnection: HubConnection | undefined;
  messages: Subject<string> = new Subject();

  constructor(private _httpClient: HttpClient) { }

  getSignalRConnectionInfo(token: string): Observable<SignalRConnectionInfo> {
    this._token = token;
    let requestUrl: string = `${this._baseUrl}SignalRConnection`;

    return this._httpClient.get<SignalRConnectionInfo>(
      requestUrl, {
        headers: new HttpHeaders({ 'Authorization': `Bearer ${this._token}` })
      });
  }

  init(signalRConnectionInfo: SignalRConnectionInfo): void {

    let options: signalR.IHttpConnectionOptions = {
      accessTokenFactory: () => signalRConnectionInfo.accessToken
    };

    this.messages.next('User registered with SignalR.');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(signalRConnectionInfo.url, options)
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.hubConnection.serverTimeoutInMilliseconds = 300000;

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
      if (this.hubConnection) {
        this.hubConnection.start();
      }

      // TODO don't like how this works, need much more robust code
      console.error(`Something went wrong: ${error}`);
    });
  }

  sendToAllUsers(message: string): Observable<object> {
    let requestUrl = `${this._baseUrl}SendMessageToAllUsers`;

    message = 'Sent To All Users: '.concat(message);

    return this._httpClient.post(
      requestUrl,
      message, {
        headers: new HttpHeaders({ "Authorization": `Bearer ${this._token}` })
      });
  }

  sendToUser(message: string, userId: string): Observable<object> {
    let requestUrl = `${this._baseUrl}SendMessageToUser/${userId}`;

    message = `Sent Only To ${userId}: `.concat(message);

    return this._httpClient.post(
      requestUrl,
      message, {
        headers: new HttpHeaders({ "Authorization": `Bearer ${this._token}` })
      });
  }
}
