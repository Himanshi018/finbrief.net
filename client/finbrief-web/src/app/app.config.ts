import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      // Route params like :id arrive as component inputs: id = input.required<string>()
      withComponentInputBinding(),
      // Scroll to top on new pages, and back to where you were when pressing Back.
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled' }),
    ),
    // withFetch uses the browser's fetch(), which also works during server-side rendering.
    provideHttpClient(withFetch()),
    // Reuses the server-rendered HTML instead of re-drawing it; clicks made before the app loads are replayed.
    provideClientHydration(withEventReplay()),
  ],
};
