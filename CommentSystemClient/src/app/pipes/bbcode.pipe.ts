import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({
  name: 'bbcode'
})
export class BbcodePipe implements PipeTransform {

  constructor(private sanitizer: DomSanitizer) {}

  transform(value: string): SafeHtml {
    if (!value) return '';

    let formattedText = this.parseBBCode(value);

    return this.sanitizer.bypassSecurityTrustHtml(formattedText);
  }

  private parseBBCode(text: string): string {
    const tagMap: { [key: string]: string } = {
      'strong': 'strong',
      'i': 'em',
      'u': 'u'
    };

    text = text.replace(/\[a=(https?:\/\/[^\]]+)\](.*?)\[\/a\]/g, '<a href="$1" target="_blank" rel="noopener noreferrer">$2</a>');
    text = text.replace(/\[a\](.*?)\[\/a\]/g, '<a href="#">$1</a>');

    const regex = /\[([a-z]+)]([\s\S]*?)\[\/\1]/gi;
    let prevText: string;

    do {
      prevText = text;
      text = text.replace(regex, (match, tag, content) => {
        const htmlTag = tagMap[tag.toLowerCase()];
        if (htmlTag) return `<${htmlTag}>${content}</${htmlTag}>`;
        if (tag.toLowerCase() === 'code') return `<code>${this.escapeHtml(content)}</code>`;
        return match;
      });
    } while (prevText !== text); 

    text = text
      .replace(/  /g, '&nbsp;&nbsp;')
      .replace(/\t/g, '&nbsp;&nbsp;&nbsp;&nbsp;'); 

    return text;
  }

  private escapeHtml(text: string): string {
    return text.replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
}
