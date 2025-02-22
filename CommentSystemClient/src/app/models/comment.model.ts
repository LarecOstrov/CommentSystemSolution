export interface User {
    userName: string;
    email: string;
    homePage: string;
    CreatedAt: string;
  }
  
  export interface Comment {
    id: string;
    parentId: string;
    text: string;
    createdAt: string;
    user: User;
    hasReplies: boolean;
    showReplies?: boolean;
    replies: Comment[];
    fileAttachments: FileAttachment[];
    hasMoreReplies?: boolean;
  }
  
  export interface FileAttachment {
    id: string;
    commentId: string;
    url: string;
    type: FileType;
    createdAt: string;
}

export enum FileType {
  IMAGE = 'IMAGE',
  TEXT = 'TEXT',
  UNKNOWN = 'UNKNOWN'
}
