import { Pipe, PipeTransform } from '@angular/core';

// "5m ago", "3h ago", "2d ago", or a date for older items.
// Usage in a template: {{ article.publishedAt | timeAgo }}
@Pipe({ name: 'timeAgo' })
export class TimeAgoPipe implements PipeTransform {
  transform(isoDate: string): string {
    const date = new Date(isoDate);
    const minutes = Math.floor((Date.now() - date.getTime()) / 60_000);

    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d ago`;
    return date.toLocaleDateString('en-IN', { day: 'numeric', month: 'short' });
  }
}
