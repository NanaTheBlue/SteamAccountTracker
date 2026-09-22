import { describe, it, expect } from 'vitest';
import { escapeHtml, chunk } from './index';

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
