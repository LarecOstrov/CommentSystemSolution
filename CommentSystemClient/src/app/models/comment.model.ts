export interface User {
    userName: string;
  }
  
  export interface Comment {
    id: string;
    text: string;
    createdAt: string;
    user: User;
    hasReplies: boolean;
    showReplies?: boolean;
    replies: Comment[];
    hasMoreReplies?: boolean;
  }
  