import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState, HttpTransportType } from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { Comment } from '../models/comment.model';

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private hubConnection!: HubConnection;
  private newCommentSubject = new BehaviorSubject<Comment | null>(null);
  newComment$ = this.newCommentSubject.asObservable();
  wsUrl = (window as any).env?.getWebSocket || 'http://localhost:5000/ws';

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.wsUrl, { 
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets
       }) 
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000, 60000])
      .configureLogging(LogLevel.None)
      .build();

    this.hubConnection
      .start()
      .then(() => {
        //console.log('WebSocket connected');
      this.startKeepAlive();
      })
      .catch(err => console.error('WebSocket connection failed:', err));

    this.hubConnection.on('ReceiveComment', (comment: Comment) => {
      //console.log('New comment received:', comment);
      this.newCommentSubject.next(comment);
    });

    this.hubConnection.on("KeepAliveAck", () => {
      //console.log("KeepAlive Acknowledged by server");
    });

    this.hubConnection.onclose(error => {
      //console.error('⚠ WebSocket closed:', error);
    });
  }

  private startKeepAlive() {
    setInterval(() => {
      if (this.hubConnection.state === HubConnectionState.Connected) {
        this.hubConnection.send("KeepAlive")
        .catch(err => console.error("KeepAlive error:", err));
      }
    }, 15000); 
  }
}
