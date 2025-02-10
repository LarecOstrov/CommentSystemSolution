import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { GraphQLModule } from './graphql.module';
import { ReactiveFormsModule } from '@angular/forms';

@NgModule({
  imports: [
    BrowserModule,
    GraphQLModule, // import GraphQL
    ReactiveFormsModule,
  ],
  providers: [],
})
export class AppModule {}
