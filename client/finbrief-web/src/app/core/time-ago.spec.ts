import { TimeAgoPipe } from './time-ago';

describe('TimeAgoPipe', () => {
  const pipe = new TimeAgoPipe();
  const ago = (ms: number) => new Date(Date.now() - ms).toISOString();

  it('shows minutes, hours and days', () => {
    expect(pipe.transform(ago(10 * 1000))).toBe('just now');
    expect(pipe.transform(ago(5 * 60 * 1000))).toBe('5m ago');
    expect(pipe.transform(ago(3 * 60 * 60 * 1000))).toBe('3h ago');
    expect(pipe.transform(ago(2 * 24 * 60 * 60 * 1000))).toBe('2d ago');
  });
});
