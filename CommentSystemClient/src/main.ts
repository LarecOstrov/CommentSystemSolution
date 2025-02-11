import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

fetch('/assets/environment.json')
  .then((response) => response.json())
  .then((env) => {
    (window as any).env = env;
    bootstrapApplication(AppComponent, appConfig)
      .catch((err) => console.error(err));
  })
  .catch((error) => {
    console.error("Error loading environment.json", error);
  });