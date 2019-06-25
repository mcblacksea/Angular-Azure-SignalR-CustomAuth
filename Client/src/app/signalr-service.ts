import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Subject } from 'rxjs';
import { HubConnection } from '@aspnet/signalr';
import * as signalR from '@aspnet/signalr';
import * as global from './globals';
import { SignalRConnectionInfo } from './signal-r-connection-info';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {

  private readonly baseUrl: string = 'http://localhost:7071/api/';
  private token: string = '';
  private hubConnection: HubConnection | undefined;
  private signalRCloseRequested: boolean = false;

  private message$: Subject<string>;
  public messageObservable$: Observable<string>;

  constructor(private httpClient: HttpClient) {
    this.message$ = new Subject<string>();
    this.messageObservable$ = this.message$.asObservable();
  }

  public async startSignalR(token: string): Promise<void> {

    await this.stopSignalR();

    this.token = token;
    const requestUrl: string = `${this.baseUrl}SignalRConnection`;

    const signalRConnectionInfo = await this.httpClient.get<SignalRConnectionInfo>(
      requestUrl, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this.token}` })
      }).toPromise();

    const options: signalR.IHttpConnectionOptions = {
      accessTokenFactory: () => signalRConnectionInfo.accessToken
    };

    this.announceMessage('User received SignalR JWT.');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(signalRConnectionInfo.url, options)
      .configureLogging(signalR.LogLevel.Information)
      .build();

    await this.hubConnection.start().
      then(() => {
        this.announceMessage('Hub Connection Started.');
      }).catch(
        err => {
          this.announceErrorMessage(err);
        }
      );

    this.hubConnection.on(global.messageTarget, (data: any) => {
      this.announceMessage(data);
    });

    this.hubConnection.onclose((error) => {
      // if stopSignalR is invoked, do not restart
      if (this.signalRCloseRequested) {
        this.signalRCloseRequested = false;
        return;
      }

      // TODO don't like how this works, need more robust code
      if (this.hubConnection) {
        this.hubConnection.start().
          then(() => {
            this.announceMessage('Hub Connection Restarted.');
          }).catch(
            err => {
              this.announceErrorMessage(err);
            }
          );
      }
      if (error) {
        this.announceErrorMessage(error);
      }
    });
  }

  public async stopSignalR(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.signalRCloseRequested = true;
      this.hubConnection.off(global.messageTarget);
      return await this.hubConnection.stop();
    }
    return;
  }

  sendToAllUsers(message: string): Observable<object> {
    const requestUrl = `${this.baseUrl}SendMessageToAllUsers`;

    message = 'Sent To All Users: '.concat(message);

    return this.httpClient.post(
      requestUrl,
      message, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this.token}` })
      });
  }

  sendToUser(message: string, userId: string): Observable<object> {
    const requestUrl = `${this.baseUrl}SendMessageToUser/${userId}`;

    message = `Sent Only To ${userId}: `.concat(message);

    return this.httpClient.post(
      requestUrl,
      message, {
        headers: new HttpHeaders({ Authorization: `Bearer ${this.token}` })
      });
  }

  private announceMessage(message: string): void {
    this.message$.next(message);
  }

  // in real app, make this better.
  private announceErrorMessage(content: any): void {
    if (content instanceof HttpErrorResponse || content instanceof Error) {
      this.announceMessage(`Error: ${content.message}`);
    } else {
      this.announceMessage(content);
    }
  }

}
