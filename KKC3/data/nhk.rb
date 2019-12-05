# encoding: UTF-8

# NHKのニュースサイト「NHK NEWS WEB」からニュース文を獲得するツール

require 'open-uri'
require 'json'
require 'fileutils'
require 'nokogiri'
require 'tempfile'

class Nhk
  NEWSLIST_DIR = 'NewsList'
  TEXTDATA_DIR = 'TextData'
  CORPUSDATA_DIR = 'Corpus'
  KYTEA_MODEL = 'C:\kytea\bin\jp-0.4.7-5.mod'
  KYTEA_PATH  = 'C:\kytea\bin\kytea.exe'
  
  def initialize(proxy = nil)
    @proxy = proxy
  end

  def _filter(c)
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

  def _open_url(retry_num, url, &block)
    loop do
      begin
        return open(url, { :proxy => @proxy }, &block)
      rescue Net::ReadTimeout
        if retry_num == 0
          raise
        else
          retry_num -= 1
        end
      end
    end
  end
  
  def get_list()
    puts "Start get news list:"

    i = 1
    FileUtils.mkdir_p(NEWSLIST_DIR)
    loop do
      url = sprintf('https://www3.nhk.or.jp/news/json16/new_%03d.json', i)

      puts "  Download JSON: #{url}"
      json = _open_url(5, url) do |f|
        charset = f.charset
        JSON.load(f.read)
      end
      
      json['channel']['item'].each do |x| 
        path = "./#{NEWSLIST_DIR}/#{x['id']}.json"
        if File.exist?(path) == false
          puts "    News: #{path}"
          data = { id: x['id'], link: "https://www3.nhk.or.jp/news/#{x['link']}", title: x['title'], pubDate: x['pubDate'] }
          File.open(path, "w") do |f|
            JSON.dump(data, f)
          end
        end
      end
      
      if json['channel']['hasNext'] != true then
        break
      else
        i += 1
      end
    end
    puts "Finish."
  end

  def _get_article(json_path)
    json = File.open(json_path, "r") do |f|
      JSON.load(f.read)
    end

    url = json['link']
    id  = json['id'].to_i
    charset = json['charset']
    
    html_path = sprintf("./#{TEXTDATA_DIR}/%08d.html", id)
    text_path = sprintf("./#{TEXTDATA_DIR}/%08d.txt", id)

    overwrite = false
    
    if File.exist?(html_path) == false && File.exist?(text_path) == false then
      puts "  Download: #{url} to #{html_path}"
      html = _open_url(5, url) do |f|
        harset = json['charset'] = f.charset
        f.read
      end
      File.open(html_path, "w") do |f|
        f.write(html)
      end
      File.open(json_path, "w") do |f|
        JSON.dump(json, f)
      end
      overwrite = true
    end

    if overwrite || File.exist?(text_path) == false then
      puts "  Scraping: #{url} to #{text_path}"
      html =  File.open(html_path, "r") do |f|
        f.read
      end
      doc = Nokogiri::HTML.parse(html, nil, charset)
      contents = doc.css('.module--content > #news_textbody, .module--content > #news_textmore, .module--content > .news_add > div, .content--detail-body > .content--summary').map{|x| x.text.strip.split(//).map{|x| _filter(x)}.join('') }.join('').gsub(/。/,"。\n").split("\n")
      File.open(text_path, "w") do |f|
        f.puts("# Date: #{DateTime.now.strftime("%Y/%m/%d %H:%M:%S")}")
        f.puts("# URL: #{url}")
        f.puts("# ID: #{id}")
        contents.each do |content|
          f.puts(content)
        end
      end
    end

  end

  def get_text_data()
    puts "Start get news text:"
    FileUtils.mkdir_p(TEXTDATA_DIR)
    Dir.foreach("./#{NEWSLIST_DIR}") do |item|
      next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".json") 
      _get_article("./#{NEWSLIST_DIR}/#{item}")
    end
    puts "Finish."
  end

  def _create_corpus(input, output)
    puts "  Create Corpus: #{input} to #{output}"
    
    ENV['KYTEA_MODEL'] ||= KYTEA_MODEL

    Tempfile.open("temp") do |fp|
      File.open(input, "r") do |f|
        f.each_line.drop_while{ |x| x.strip.start_with?("#") }.each do |line|
          if (line.chomp != "") then
            fp.puts(line.chomp)
          end
        end
      end
    
      fp.flush

      cmd = KYTEA_PATH+' -wordbound "'+"\t"+'" < "' + fp.path + '"'
      ret = `#{cmd}`
      File.open(output, "w") do |f|
        ret.split("\n").each do |line|
          line.split("\t").each do |entry|
            (word,feat,read) = entry.split("/")
            f.puts([word,read,feat].join("\t"))
          end
          f.puts();
        end
      end
    end
  end

  def make_corpus(allUpdate = false)
    puts "Start make corpus:"
   FileUtils.mkdir_p(CORPUSDATA_DIR)
    Dir.foreach("./#{TEXTDATA_DIR}") do |item|
      next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".txt") 
      input = File.expand_path(File.dirname(__FILE__), "./#{TEXTDATA_DIR}/#{item}") 
      output = File.expand_path(File.dirname(__FILE__), "./#{CORPUSDATA_DIR}/#{item}")
      next if (allUpdate == false) and (File.exist?(output))

      _create_corpus(input, output)
    end
    puts "Finish."
  end

end


nhk = Nhk.new('http://160.203.98.12:8080/')
#nhk.get_list()
nhk.get_text_data()
nhk.make_corpus()

