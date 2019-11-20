allUpdate = ARGV.include?("-all")

Dir.foreach('./TextData') do |item|
  next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".txt") 
  input = File.expand_path(File.dirname(__FILE__), "./TextData/#{item}") 
  output = File.expand_path(File.dirname(__FILE__), "./Corpus/#{item}")
  next if (allUpdate == false) and (File.exist?(output))
  cmd = 'c:/mecab/bin/mecab.exe --node-format=%m\t%f[20]\t%f[0]\n --eos-format=\n --unk-format=%M "' + input + '" > "' + output + '"'
  puts "#{input} to #{output}"
  system(cmd)
end


