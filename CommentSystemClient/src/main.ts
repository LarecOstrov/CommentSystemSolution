import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { enableProdMode } from '@angular/core';

fetch('/assets/environment.json')
  .then((response) => response.json())
  .then((env) => {
    (window as any).env = env;
    bootstrapApplication(AppComponent, appConfig)
      .catch((err) => console.error(err));
      if (env.production) {
        enableProdMode();
      }
  })
  .catch((error) => {
    console.error("Error loading environment.json", error);
  });