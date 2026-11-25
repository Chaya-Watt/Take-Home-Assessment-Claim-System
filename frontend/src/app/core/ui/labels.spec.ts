import { incidentTypeLabel, todayIsoDate } from './labels';

describe('labels', () => {
  it.each([
    ['Collision', 'Collision'],
    ['WaterDamage', 'Water damage'],
    ['Hospitalization', 'Hospitalization'],
  ] as const)('incidentTypeLabel(%s) = %s', (type, label) => {
    expect(incidentTypeLabel(type)).toBe(label);
  });

  it('todayIsoDate uses the local calendar date in YYYY-MM-DD form', () => {
    expect(todayIsoDate(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });
});
