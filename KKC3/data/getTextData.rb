#getTextData
require 'nokogiri'
require 'open-uri'
require 'json'
require 'fileutils'

def filter(c)
  case c.ord
    when " ".ord          then return '　'
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

def get(json)
  charset = nil
  url = json['link']
  id  = json['id'].to_i
  path = sprintf("./TextData/%08d.txt", id)

  if File.exist?(path) then
    return 
  end

  puts "Download: #{url}"

  retry_num = 5
  html = ""
  loop do
    begin
      #html = open(url, { :proxy => 'http://160.203.98.12:8080/' }) do |f|
      html = open(url) do |f|
        charset = f.charset
        f.read
      end
      break
    rescue Net::ReadTimeout
      puts "-> time out"
      if retry_num == 0
        raise
      else
        puts "  -> retry"
        retry_num -= 1
      end
    end
  end
  
  doc = Nokogiri::HTML.parse(html, nil, charset)
  contents = doc.css('.module--content > #news_textbody, .module--content > #news_textmore, .module--content > .news_add > div, .content--detail-body > .content--summary').map{|x| x.text.strip.split(//).map{|x| filter(x)}.join('') }.join('').gsub(/。/,"。\n").split("\n")
  File.open(path, "w") do |f|
    f.puts("# Date: #{DateTime.now.strftime("%Y/%m/%d %H:%M:%S")}")
    f.puts("# URL: #{url}")
    f.puts("# ID: #{id}")
    contents.each do |content|
      f.puts(content)
    end
  end
  return
end

FileUtils.mkdir_p("TextData")
Dir.foreach('./NewsList') do |item|
  next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".json") 
  json = File.open("./NewsList/#{item}", "r") do |f|
    JSON.load(f.read)
  end
  get(json)
end


