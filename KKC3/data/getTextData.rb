require 'nokogiri'
require 'open-uri'


def filter(c)
  case c.ord
    when "\r".ord         then return ''
    when "\n".ord         then return ''
    when '!'.ord          then return '！'
    when '"'.ord          then return '”'
    when '#'.ord          then return '＃'
    when '$'.ord          then return '＄'
    when '%'.ord          then return '％'
    when '&'.ord          then return '＆'
    when "'".ord          then return '’'
    when '('.ord          then return '（'
    when ')'.ord          then return '）'
    when '*'.ord          then return '＊'
    when '+'.ord          then return '＋'
    when ','.ord          then return '，'
    when '-'.ord          then return '－'
    when '.'.ord          then return '．'
    when '/'.ord          then return '／'
    when '0'.ord..'9'.ord then return ['０'.ord + (c.ord - '0'.ord)].pack("U")
    when ':'.ord          then return '：'
    when ';'.ord          then return '；'
    when '<'.ord          then return '＜'
    when '='.ord          then return '＝'
    when '>'.ord          then return '＞'
    when '?'.ord          then return '？'
    when '@'.ord          then return '＠'
    when 'A'.ord..'Z'.ord then return ['Ａ'.ord + (c.ord - 'A'.ord)].pack("U")
    when '['.ord          then return '［'
    when '\\'.ord         then return '￥'
    when ']'.ord          then return '］'
    when '^'.ord          then return '＾'
    when '_'.ord          then return '＿'
    when '`'.ord          then return '‘'
    when 'a'.ord..'z'.ord then return ['ａ'.ord + (c.ord - 'a'.ord)].pack("U")
    when '{'.ord          then return '｛'
    when '|'.ord          then return '｜'
    when '}'.ord          then return '｝'
    when '~'.ord          then return '～'
    else                       return c
  end
end

def get(url)
  charset = nil
  html = open(url) do |f|
    charset = f.charset
    f.read
  end

  doc = Nokogiri::HTML.parse(html, nil, charset)
  lines = doc.css('.module--content > #news_textbody, .module--content > #news_textmore, .module--content > .news_add > div, .content--detail-body > .content--summary').map{|x| x.text.strip.split(//).map{|x| filter(x)}.join('') }.join('').gsub(/。/,"。\n")
  for i in 1 .. 1000000 do
    path = sprintf("./TextData/%08d.txt", i)
    if File.exist?(path) then
      next
    end
    IO.write(path, lines)
    puts "#{url} save to #{path}."
    return true
  end
  return false
end

ARGV.each { |arg| get(arg) }

