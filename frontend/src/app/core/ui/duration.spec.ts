import { timeSince } from './duration';

describe('timeSince', () => {
  const now = '2026-11-25T12:00:00Z';

  it.each([
    ['2026-11-25T12:00:00Z', '0 minutes'],
    ['2026-11-25T11:59:00Z', '1 minute'],
    ['2026-11-25T11:15:00Z', '45 minutes'],
    ['2026-11-25T11:00:00Z', '1 hour'],
    ['2026-11-25T02:30:00Z', '9 hours'],
    ['2026-11-24T12:00:00Z', '1 day'],
    ['2026-11-20T08:00:00Z', '5 days'],
  ])('%s → %s', (since, expected) => {
    expect(timeSince(since, now)).toBe(expected);
  });

  it('never goes negative when clocks disagree slightly', () => {
    expect(timeSince('2026-11-25T12:00:30Z', now)).toBe('0 minutes');
  });
});
