require 'open-uri'
require 'json'
require 'fileutils'

def get_list()
  i = 1
  list = {}
  FileUtils.mkdir_p("NewsList")
  loop do
    url = sprintf('https://www3.nhk.or.jp/news/json16/new_%03d.json', i)
    json = open(url) do |f|
      charset = f.charset
      JSON.load(f.read)
    end
    json['channel']['item'].each do |x| 
      path = "./NewsList/#{x['id']}.json"
      if File.exist?(path) == false
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
  return list
end

get_list()

