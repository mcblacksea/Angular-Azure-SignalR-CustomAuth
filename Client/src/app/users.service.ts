import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserItem } from './UserItem';
import { TokenItem } from './TokenItem';

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly _baseUrl: string = 'http://localhost:7071/api/';

  constructor(private _httpClient: HttpClient) { }

  seedDatabase(): Observable<Array<UserItem>> {
    const requestUrl = `${this._baseUrl}SeedDatabase`;
    return this._httpClient.get<Array<UserItem>>(requestUrl);
  }

  login(userName: string, password: string): Observable<TokenItem> {
    const requestUrl = `${this._baseUrl}Login`;
    const body = {
      UserName: userName,
      Password: password
    };
    return this._httpClient.post<TokenItem>(requestUrl, body);
  }
}
