import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { SignalRService } from './signalr-service';
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { UsersService } from './users.service';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule
  ],
  providers: [
    SignalRService,
    UsersService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
