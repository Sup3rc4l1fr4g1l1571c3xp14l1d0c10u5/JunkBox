#getTextData
require 'nokogiri'
require 'open-uri'
require 'json'

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
  html = open(url, { :proxy => 'http://160.203.98.12:8080/' }) do |f|
    charset = f.charset
    f.read
  end

  doc = Nokogiri::HTML.parse(html, nil, charset)
  contents = doc.css('.module--content > #news_textbody, .module--content > #news_textmore, .module--content > .news_add > div, .content--detail-body > .content--summary').map{|x| x.text.strip.split(//).map{|x| filter(x)}.join('') }.join('').gsub(/。/,"。\n").split("\n")
  for i in 1 .. 1000000 do
    path = sprintf("./TextData/%08d.txt", i)
    if File.exist?(path) then
      next
    end
    File.open(path, "w") do |f|
      f.puts("# #{DateTime.now.strftime("%Y/%m/%d %H:%M:%S")}")
      f.puts("# #{url}")
      contents.each do |content|
        f.puts(content)
      end
    end
    puts "#{url} save to #{path}."
    return true
  end
  return false
end

STDIN.each.each do |line|
  get(line.chomp)
end

