import { Component, computed, input, model } from '@angular/core';

/** Previous / next buttons for a paged API list. Two-way bind the page: `[(page)]="page"`. */
@Component({
  selector: 'app-pager',
  template: `
    <nav aria-label="Pagination" class="pager">
      <button type="button" class="secondary" [disabled]="page() <= 1" (click)="page.set(page() - 1)">Previous</button>
      <span aria-live="polite">Page {{ page() }} of {{ totalPages() }} ({{ totalCount() }} {{ itemLabel() }})</span>
      <button type="button" class="secondary" [disabled]="page() >= totalPages()" (click)="page.set(page() + 1)">
        Next
      </button>
    </nav>
  `,
})
export class Pager {
  readonly page = model.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly itemLabel = input('items');

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
}
