import { provideApollo } from 'apollo-angular';
import { HttpLink } from 'apollo-angular/http';
import { inject } from '@angular/core';
import { ApolloClientOptions, InMemoryCache } from '@apollo/client/core';

export function createApollo(): ApolloClientOptions<any> {
  const httpLink = inject(HttpLink);
  return {
    link: httpLink.create({
      uri: (window as any).env?.getCommentsGraphQL || 'http://localhost:5000/graphql',
    }),
    cache: new InMemoryCache(),
    connectToDevTools: false,
  };
}

export const GraphQLProviders = [
  provideApollo(createApollo),
];
