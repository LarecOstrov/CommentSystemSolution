export type SortingField = 
  { createdAt?: string } 
  | { user?: { userName?: string } } 
  | { user?: { email?: string } };
