export interface User {
    userName: string;
    email: string;
    homePage: string;
    CreatedAt: string;
  }
  
  export interface Comment {
    id: string;
    text: string;
    createdAt: string;
    user: User;
    hasReplies: boolean;
    showReplies?: boolean;
    replies: Comment[];
    fileAttachments: { type: string, url: string }[];
    hasMoreReplies?: boolean;
  }
  