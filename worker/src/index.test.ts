import { describe, it, expect } from 'vitest';
import { escapeHtml, chunk, shouldStop } from './index';

describe('escapeHtml', () => {
  it('escapes HTML special characters correctly', () => {
    const input = '<script>alert("XSS & fun")</script>\'test\'';
    const expected = '&lt;script&gt;alert(&quot;XSS &amp; fun&quot;)&lt;/script&gt;&#039;test&#039;';
    expect(escapeHtml(input)).toBe(expected);
  });

  it('leaves safe strings unmodified', () => {
    expect(escapeHtml('CheaterWatchUser123')).toBe('CheaterWatchUser123');
  });

  it('handles empty strings', () => {
    expect(escapeHtml('')).toBe('');
  });
});

describe('chunk', () => {
  it('splits array into evenly sized chunks', () => {
    const data = [1, 2, 3, 4, 5, 6];
    expect(chunk(data, 2)).toEqual([
      [1, 2],
      [3, 4],
      [5, 6],
    ]);
  });

  it('handles array with trailing elements in last chunk', () => {
    const data = [1, 2, 3, 4, 5];
    expect(chunk(data, 2)).toEqual([
      [1, 2],
      [3, 4],
      [5],
    ]);
  });

  it('handles empty array', () => {
    expect(chunk([], 10)).toEqual([]);
  });
});

describe('shouldStop', () => {
  it('returns true when request count reaches maxRequests', () => {
    const startTime = Date.now();
    expect(shouldStop(40, startTime, 40)).toBe(true);
    expect(shouldStop(41, startTime, 40)).toBe(true);
  });

  it('returns true when time exceeds 13 minutes', () => {
    const oldStartTime = Date.now() - (13 * 60 * 1000 + 1000); // 13m 1s ago
    expect(shouldStop(5, oldStartTime, 40)).toBe(true);
  });

  it('returns false when within request limit and time budget', () => {
    const recentStartTime = Date.now() - 5000;
    expect(shouldStop(10, recentStartTime, 40)).toBe(false);
  });
});

