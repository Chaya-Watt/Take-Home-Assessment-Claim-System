/**
 * How long ago `since` was, relative to `now`, in the largest whole unit: "3 days", "5 hours", "12 minutes".
 * Both are ISO timestamps from the API (the dashboard passes its `generatedAt` as `now`).
 */
export function timeSince(since: string, now: string): string {
  const minutes = Math.max(0, Math.floor((Date.parse(now) - Date.parse(since)) / 60_000));
  if (minutes < 60) {
    return plural(minutes, 'minute');
  }
  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return plural(hours, 'hour');
  }
  return plural(Math.floor(hours / 24), 'day');
}

function plural(count: number, unit: string): string {
  return `${count} ${unit}${count === 1 ? '' : 's'}`;
}
