require 'FileUtils'

FileUtils.mkdir_p('new')
ARGV.each do |argv|
  File.open("new/#{argv}", "w:UTF-8") do |fo| 
    File.open(argv, "r:UTF-8") do |fi| 
      while (line = fi.gets) != nil do
        line.chomp!
        row = 1
        line.split(//).each do |x| 
          if x =~ /^[\x01-\x7F]$/ then
            if x == "\t"
              space = (4 - (row % 4))+1
              fo.print (' '*space)
              row += space
            else
              fo.print x
              row += 1
            end
          else
            fo.print x
            row += 2
          end
        end
        fo.puts ""
      end
    end
  end
end
