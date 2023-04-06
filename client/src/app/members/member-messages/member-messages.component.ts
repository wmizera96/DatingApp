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
  loading = false;
  constructor(public messageService: MessageService) {}

  sendMessage() {
    if (!this.userName) return;

    this.loading = true;
    this.messageService
      .sendMessage(this.userName, this.messageContent)
      .then(() => {
        this.messageForm?.reset();
      }).finally(() => (this.loading = false));
  }
}
