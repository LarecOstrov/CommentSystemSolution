import { NgModule } from '@angular/core';
import { APOLLO_OPTIONS } from 'apollo-angular';
import { ApolloClient, InMemoryCache, ApolloClientOptions } from '@apollo/client/core';

export function createApollo(): ApolloClientOptions<any> {
  return {
    link: new ApolloClient({
      uri: (window as any).env?.getCommentsGraphQL || 'http://localhost:5000/graphql',
      cache: new InMemoryCache(),
    }).link,
    cache: new InMemoryCache(),
  };
}

@NgModule({
  providers: [
    {
      provide: APOLLO_OPTIONS,
      useFactory: createApollo,
    },
  ],
})
export class GraphQLModule {}
