import { Component, OnInit } from '@angular/core';
import { Member } from '../../_modules/member';
import { MembersService } from '../../_services/members.service';
import { ActivatedRoute } from '@angular/router';
import {
  NgxGalleryAnimation,
  NgxGalleryImage,
  NgxGalleryOptions,
} from '@kolkov/ngx-gallery';

@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css'],
})
export class MemberDetailComponent implements OnInit {
  member: Member | undefined;
  galleryOptions: NgxGalleryOptions[] = [];
  galleryImages: NgxGalleryImage[] = [];

  constructor(
    private memberService: MembersService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loadMember();

    this.galleryOptions = [
      {
        width: '500px',
        height: '500px',
        imagePercent: 100,
        thumbnailsColumns: 4,
        imageAnimation: NgxGalleryAnimation.Slide,
        preview: false,
      },
    ];
  }

  getImages(): NgxGalleryImage[] {
    const imageUrls: NgxGalleryImage[] = [];
    if (this.member) {
      for (const photo of this.member.photos) {
        imageUrls.push({
          small: photo.url,
          medium: photo.url,
          big: photo.url,
        });
      }
    }

    return imageUrls;
  }

  loadMember() {
    const userName = this.route.snapshot.paramMap.get('userName');
    if (userName) {
      this.memberService.getMember(userName).subscribe({
        next: (member) => {
          this.member = member;
          this.galleryImages = this.getImages();
        },
      });
    }
  }
}
