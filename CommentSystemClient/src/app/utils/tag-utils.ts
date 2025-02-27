export function validateBBCode(text: string): { isValid: boolean; errors: string[] } {
    const allowedTags = new Set(['a', 'code', 'i', 'strong']);
    const tagRegex = /\[(\/?)(\w+)[^\]]*\]/g;
    const stack: string[] = [];
    const errors: string[] = [];
  
    let match;
    while ((match = tagRegex.exec(text)) !== null) {
      const [, isClosing, tag] = match;
      const lowerTag = tag.toLowerCase();
  
      if (!allowedTags.has(lowerTag)) {
        errors.push(`Invalid tag: [${isClosing ? '/' : ''}${tag}]`);
        continue;
      }
  
      if (isClosing) {
        if (stack.length === 0 || stack.pop() !== lowerTag) {
          errors.push(`Mismatched closing tag: [/${tag}]`);
        }
      } else {
        stack.push(lowerTag);
      }
    }
  
    if (stack.length > 0) {
      errors.push(`Unclosed tags: ${stack.map(tag => `[${tag}]`).join(', ')}`);
    }
  
    return { isValid: errors.length === 0, errors };
  }
   
  export function validateHtml(text: string): { isValid: boolean; errors: string[] } {
    const allowedTags = new Set(['a', 'code', 'i', 'strong']);
    const tagRegex = /<\s*(\/?)\s*([a-zA-Z0-9]+)[^>]*>/g;
    const stack: string[] = [];
    const errors: string[] = [];
  
    let match;
    while ((match = tagRegex.exec(text)) !== null) {
      const [, isClosing, tag] = match;
      const lowerTag = tag.toLowerCase();
  
      if (!allowedTags.has(lowerTag)) {
        errors.push(`Invalid tag: <${isClosing ? '/' : ''}${tag}>`);
        continue;
      }
  
      if (isClosing) {
        if (stack.length === 0 || stack.pop() !== lowerTag) {
          errors.push(`Mismatched closing tag: </${tag}>`);
        }
      } else {
        stack.push(lowerTag);
      }
    }
  
    if (stack.length > 0) {
      errors.push(`Unclosed tags: ${stack.map(tag => `<${tag}>`).join(', ')}`);
    }
  
    return { isValid: errors.length === 0, errors };
  }
  