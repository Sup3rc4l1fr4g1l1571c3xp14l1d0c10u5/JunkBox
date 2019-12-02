# makeCorpus
require "tempfile"
require 'fileutils'

allUpdate = ARGV.include?("-all")

Dir.foreach('./TextData') do |item|
  next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".txt") 
  input = File.expand_path(File.dirname(__FILE__), "./TextData/#{item}") 
  Tempfile.open("temp") do |fp|
    File.open(input, "r") do |f|
	  f.each_line.each do |line|
	    if line.strip.start_with?("#") 
          fp.puts(line.chomp)
	    else
          fp.puts(line.chomp.gsub(" ","　"))
		end
	  end
	end
	fp.flush
	FileUtils.cp(fp.path, input)
  end
end
puts "fixed"
FileUtils.mkdir_p("Corpus")
Dir.foreach('./TextData') do |item|
  next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".txt") 
  input = File.expand_path(File.dirname(__FILE__), "./TextData/#{item}") 
  output = File.expand_path(File.dirname(__FILE__), "./Corpus/#{item}")
  next if (allUpdate == false) and (File.exist?(output))
  puts "#{input} to #{output}"
  
  ENV['KYTEA_MODEL'] = 'C:\kytea-master\bin\jp-0.4.7-5.mod'

  
  Tempfile.open("temp") do |fp|
    File.open(input, "r") do |f|
	  f.each_line.drop_while{ |x| x.strip.start_with?("#") }.each do |line|
	    if (line.chomp != "") then
          fp.puts(line.chomp.sub(" ","　"))
		end
	  end
	end
	fp.flush
    #cmd = 'c:/mecab/bin/mecab.exe --node-format=%m\t%f[20]\t%f[0]\n --eos-format=\n --unk-format=%M "' + fp.path + '" > "' + output + '"'
    #system(cmd)
    cmd = 'C:\kytea-master\bin\kytea.exe < "' + fp.path + '"'
    ret = `#{cmd}`
    File.open(output, "w") do |f|
      ret.split("\n").each do |line|
        line.split(" ").each do |entry|
          (word,feat,read) = entry.split('/')
          f.puts([word,read,feat].join("\t"))
        end
        f.puts();
      end
	end
  end
end
