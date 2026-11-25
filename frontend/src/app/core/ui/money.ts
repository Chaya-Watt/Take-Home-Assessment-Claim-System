import { Pipe, PipeTransform } from '@angular/core';

// The API sends and expects amounts as integers in satang (1 THB = 100 satang).

const thbFormat = new Intl.NumberFormat('th-TH', { style: 'currency', currency: 'THB' });

/** Formats satang as Thai baht: 4500000 → "฿45,000.00". */
@Pipe({ name: 'thb' })
export class ThbPipe implements PipeTransform {
  transform(satang: number | null | undefined): string {
    return satang === null || satang === undefined ? '—' : thbFormat.format(satang / 100);
  }
}

/** Converts a baht amount typed by the user into satang for the API (rounded to the nearest satang). */
export function bahtToSatang(baht: number): number {
  return Math.round(baht * 100);
}
