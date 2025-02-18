import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'bbcode'
})
export class BbcodePipe implements PipeTransform {
  transform(value: string): string {
    if (!value) return '';

    let formattedText = value
      .replace(/\[strong\](.*?)\[\/strong\]/g, '<strong>$1</strong>')
      .replace(/\[i\](.*?)\[\/i\]/g, '<em>$1</em>')
      .replace(/\[u\](.*?)\[\/u\]/g, '<u>$1</u>')
      .replace(/\[code\](.*?)\[\/code\]/gs, '<pre><code>$1</code></pre>') 
      .replace(/\[a=(https?:\/\/[^\]]+)\](.*?)\[\/a\]/g, '<a href="$1" target="_blank" rel="noopener noreferrer">$2</a>') 
      .replace(/\[a\](.*?)\[\/a\]/g, '<a href="#">$1</a>') 
      //.replace(/\n/g, '<br>') // Переноси рядків
      .replace(/ {2}/g, '&nbsp;&nbsp;') 
      .replace(/\t/g, '&nbsp;&nbsp;&nbsp;&nbsp;');

    return formattedText;
  }
}
