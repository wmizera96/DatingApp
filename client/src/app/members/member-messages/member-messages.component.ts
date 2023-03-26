import { Component, Input, ViewChild } from '@angular/core';
import { Message } from '../../_models/message';
import { MessageService } from '../../_services/message.service';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css'],
})
export class MemberMessagesComponent {
  @Input() userName?: string;
  @Input() messages: Message[] = [];
  @ViewChild('messageForm') messageForm?: NgForm;

  messageContent = '';

  constructor(private messageService: MessageService) {}

  sendMessage() {
    if (!this.userName) return;

    this.messageService
      .sendMessage(this.userName, this.messageContent)
      .subscribe({
        next: (message) => {
          this.messages.push(message);
          this.messageForm?.reset();
        },
      });
  }
}
