import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TimelineEntry } from '../api/models';
import { StatusBadge } from './status-badge';

/** Every status change of a claim, oldest first, with the time shown in the user's local time zone. */
@Component({
  selector: 'app-claim-timeline',
  imports: [DatePipe, StatusBadge],
  template: `
    <ol class="timeline">
      @for (entry of entries(); track $index) {
        <li>
          <app-status-badge [status]="entry.toStatus" />
          <time [attr.datetime]="entry.changedAt">{{ entry.changedAt | date: 'medium' }}</time>
          @if (entry.note) {
            <p>{{ entry.note }}</p>
          }
        </li>
      }
    </ol>
  `,
})
export class ClaimTimeline {
  readonly entries = input.required<readonly TimelineEntry[]>();
}
