import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserItem } from './user-item';
import { TokenItem } from './token-item';

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly baseUrl: string = 'http://localhost:7071/api/';

  constructor(private httpClient: HttpClient) { }

  public seedDatabase(): Observable<Array<UserItem>> {
    const requestUrl = `${this.baseUrl}SeedDatabase`;
    return this.httpClient.get<Array<UserItem>>(requestUrl);
  }

  public login(userName: string, password: string): Observable<TokenItem> {
    const requestUrl = `${this.baseUrl}Login`;
    const body = {
      userName: userName,
      password: password
    };
    return this.httpClient.post<TokenItem>(requestUrl, body);
  }
}
