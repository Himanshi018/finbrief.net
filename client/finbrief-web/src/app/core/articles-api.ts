import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ArticlePage } from './models';

export interface ArticleQuery {
  category?: string | null;
  search?: string | null;
  cursor?: string | null;
  limit?: number;
}

// All calls to the FinBrief API go through this service, so the base URL lives in one place.
@Injectable({ providedIn: 'root' })
export class ArticlesApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  // GET /api/articles?category=&search=&cursor=&limit=
  getArticles(query: ArticleQuery): Observable<ArticlePage> {
    let params = new HttpParams();
    if (query.category) params = params.set('category', query.category);
    if (query.search) params = params.set('search', query.search);
    if (query.cursor) params = params.set('cursor', query.cursor);
    if (query.limit) params = params.set('limit', query.limit);

    return this.http.get<ArticlePage>(`${this.baseUrl}/api/articles`, { params });
  }

  // URLs for httpResource(), which loads data reactively from a signal.
  articleUrl(id: string | number): string {
    return `${this.baseUrl}/api/articles/${encodeURIComponent(id)}`;
  }

  categoriesUrl(): string {
    return `${this.baseUrl}/api/categories`;
  }
}
