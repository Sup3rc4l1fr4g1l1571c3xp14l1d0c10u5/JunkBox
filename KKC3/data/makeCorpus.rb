# makeCorpus
require "tempfile"

allUpdate = ARGV.include?("-all")

Dir.foreach('./TextData') do |item|
  next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".txt") 
  input = File.expand_path(File.dirname(__FILE__), "./TextData/#{item}") 
  output = File.expand_path(File.dirname(__FILE__), "./Corpus/#{item}")
  next if (allUpdate == false) and (File.exist?(output))
  puts "#{input} to #{output}"
  
  Tempfile.open("temp") do |fp|
    File.open(input, "r") do |f|
	  f.each_line.drop_while{ |x| x.strip.start_with?("#") }.each do |line|
	    if (line.chomp != "") then
          fp.puts(line.chomp)
		end
	  end
	end
	fp.flush
    cmd = 'c:/mecab/bin/mecab.exe --node-format=%m\t%f[20]\t%f[0]\n --eos-format=\n --unk-format=%M "' + fp.path + '" > "' + output + '"'
    system(cmd)
  end
end
