import { FileType } from "../models/comment.model";

export function mapFileType(type: number): FileType {
    switch (type) {
      case 0: return FileType.IMAGE;
      case 1: return FileType.TEXT;
      default: return FileType.UNKNOWN;
    }
}

export function areFilesValid(selectedFiles: { file: File, name: string }[]): boolean {
  const allowedFileTypes = ['image/png', 'image/jpeg', 'image/gif', 'text/plain'];
  
  return selectedFiles.every(({ file }) => allowedFileTypes.includes(file.type));
}