import { Pipe, PipeTransform } from '@angular/core';
import { IncidentType } from '../api/models';

/** "WaterDamage" → "Water damage". */
export function incidentTypeLabel(type: IncidentType): string {
  const words = type.replace(/([a-z])([A-Z])/g, '$1 $2');
  return words.charAt(0) + words.slice(1).toLowerCase();
}

@Pipe({ name: 'incidentType' })
export class IncidentTypePipe implements PipeTransform {
  transform(type: IncidentType): string {
    return incidentTypeLabel(type);
  }
}

/** Today's date in the user's time zone as "YYYY-MM-DD" (the format of date inputs and of DateOnly in the API). */
export function todayIsoDate(now = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}
