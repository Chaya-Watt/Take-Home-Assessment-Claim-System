import { bahtToSatang, ThbPipe } from './money';

describe('ThbPipe', () => {
  const pipe = new ThbPipe();

  it('formats satang as Thai baht', () => {
    expect(pipe.transform(4_500_000)).toBe('฿45,000.00');
    expect(pipe.transform(5)).toBe('฿0.05');
  });

  it('shows a dash when there is no amount yet', () => {
    expect(pipe.transform(null)).toBe('—');
    expect(pipe.transform(undefined)).toBe('—');
  });
});

describe('bahtToSatang', () => {
  it('converts baht to whole satang', () => {
    expect(bahtToSatang(45_000)).toBe(4_500_000);
    expect(bahtToSatang(1234.56)).toBe(123_456);
  });

  it('rounds floating-point noise to the nearest satang', () => {
    expect(bahtToSatang(0.1 + 0.2)).toBe(30); // 0.30000000000000004 baht
    expect(bahtToSatang(19.99)).toBe(1999); // 1998.9999999999998 before rounding
  });
});
