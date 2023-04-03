import {
  ChangeDetectionStrategy,
  Component,
  Input,
  ViewChild,
} from '@angular/core';
import { MessageService } from '../../_services/message.service';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemberMessagesComponent {
  @Input() userName?: string;
  @ViewChild('messageForm') messageForm?: NgForm;

  messageContent = '';

  constructor(public messageService: MessageService) {}

  sendMessage() {
    if (!this.userName) return;

    this.messageService
      .sendMessage(this.userName, this.messageContent)
      .then(() => {
        this.messageForm?.reset();
      });
  }
}
