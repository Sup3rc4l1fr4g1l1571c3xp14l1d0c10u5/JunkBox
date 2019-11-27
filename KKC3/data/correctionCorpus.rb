require "tempfile"

def yomi0(moji)
	case moji
		when '０' 
			return 'ゼロ'
		when '１'
			return 'イチ'
		when '２'
			return 'ニ'
		when '３'
			return 'サン'
		when '４'
			return 'ヨン'
		when '５'
			return 'ゴ'
		when '６'
			return 'ロク'
		when '７'
			return 'ナナ'
		when '８'
			return 'ハチ'
		when '９'
			return 'キュウ'
		else raise "bad moji #{moji}"
	end
end

def yomi1(moji,keta)
	case moji
		when '０' 
			return ''
		when '１'
			case keta
				when 0 then
					return 'イチ'
				when 1 then
					return 'ジュウ'
				when 2 then
					return 'ヒャク'
				when 3 then
					return 'セン'
				else raise "bad keta"
			end
		when '２'
			case keta
				when 0 then
					return 'ニ'
				when 1 then
					return 'ニジュウ'
				when 2 then
					return 'ニヒャク'
				when 3 then
					return 'ニセン'
				else raise "bad keta"
			end
		when '３'
			case keta
				when 0 then
					return 'サン'
				when 1 then
					return 'サンジュウ'
				when 2 then
					return 'サンビャク'
				when 3 then
					return 'サンゼン'
				else raise "bad keta"
			end
		when '４'
			case keta
				when 0 then
					return 'ヨン'
				when 1 then
					return 'ヨンジュウ'
				when 2 then
					return 'ヨンヒャク'
				when 3 then
					return 'ヨンセン'
				else raise "bad keta"
			end
		when '５'
			case keta
				when 0 then
					return 'ゴ'
				when 1 then
					return 'ゴジュウ'
				when 2 then
					return 'ゴヒャク'
				when 3 then
					return 'ゴセン'
				else raise "bad keta"
			end
		when '６'
			case keta
				when 0 then
					return 'ロク'
				when 1 then
					return 'ロクジュウ'
				when 2 then
					return 'ロッピャク'
				when 3 then
					return 'ロクセン'
				else raise "bad keta"
			end
		when '７'
			case keta
				when 0 then
					return 'ナナ'
				when 1 then
					return 'ナナジュウ'
				when 2 then
					return 'ナナヒャク'
				when 3 then
					return 'ナナセン'
				else raise "bad keta"
			end
		when '８'
			case keta
				when 0 then
					return 'ハチ'
				when 1 then
					return 'ハチジュウ'
				when 2 then
					return 'ハッピャク'
				when 3 then
					return 'ハッセン'
				else raise "bad keta"
			end
		when '９'
			case keta
				when 0 then
					return 'キュウ'
				when 1 then
					return 'キュウジュウ'
				when 2 then
					return 'キュウヒャク'
				when 3 then
					return 'キュウセン'
				else raise "bad keta"
			end
		else raise "bad moji #{moji}"
	end
end

def yomi2(moji,keta)
	yomi = moji[0..3].split(//).each.with_index.map {|x,i| [x, yomi1(x,moji.length-i-1)] } 
	i = 1
	while yomi.length > i
		if yomi[i][1] == ""
			yomi[i-1][0] += yomi[i][0]
			yomi[i-1][1] += yomi[i][1]
			x = yomi.delete_at(i)
		else
			i += 1
		end
	end
	idx = yomi.find_index {|x| x[1] != "" }
	if idx != nil
		yomi[idx][1] += keta
		fixmap = [
			['イチチョウ', 'イッチョウ'], 
			['ハチチョウ', 'ハッチョウ'], 
			['ジュウチョウ', 'ジュッチョウ'], 
			['イチケイ', 'イッケイ'], 
			['ハチケイ', 'ハッケイ'], 
			['ジュウケイ', 'ジュッケイ'], 
			['ヒャクケイ', 'ヒャッケイ'], 
		]
		yomi[idx][1] = fixmap.reduce (yomi[idx][1]) {|s,x| s.gsub(x[0],x[1]) }
	end
	return yomi
end

def yomi3(moji)
	range = moji.length % 4
	keta = (moji.length-1) / 4
	keta_yomi = ['','マン','オク','チョウ','ケイ','ガイ']
	s = 0
	ret = []
	if range > 0
		ret += yomi2(moji[0...range], keta_yomi[keta])
		s = range
		keta -= 1
	end
	while (keta >= 0)
		ret += yomi2(moji[s...s+4], keta_yomi[keta])
		s = s+4
		keta -= 1
	end
	return ret
end

YOMI = {
'０'=>'レイ',
'１'=>'イチ',
'２'=>'ニ',
'３'=>'サン',
'４'=>'ヨン',
'５'=>'ゴ',
'６'=>'ロク',
'７'=>'ナナ',
'８'=>'ハチ',
'９'=>'キュウ',
}

def correction(words)
	output = []
	tokens = []
	dotted = false
	words.push(["\t"])
	words.each do |word|
		if word[0] == '．' && dotted == false && tokens.length > 0
			tokens.push(word[0])
			dotted = true
		elsif word[0] =~ /^[０１２３４５６７８９]$/ && (tokens.length > 0 || YOMI[word[0]] == word[1])
			tokens.push(word[0])
		else
			if tokens.length != 0
				if (dotted &&tokens.last == '．')
					s = tokens[0..-2].join('')
					output += yomi3(s).map{|x| [x[0],x[1],'数詞']}
					output.push(['．','．','補助記号'])
				elsif dotted
					idx = tokens.index('．')
					s = tokens[0...idx].join('')
					e = tokens[idx+1..-1].map{|x| yomi0(x)}.join('')
					output += yomi3(s).map{|x| [x[0],x[1],'数詞']}
					output.push(['．','．','補助記号'])
					tokens[idx+1..-1].each do |t|
						output.push([t,yomi0(t),'数詞'])
					end
				else
					s = tokens.join('')
					output += yomi3(s).map{|x| [x[0],x[1],'数詞']}
				end
				tokens.clear
				dotted = false
			end
			output.push(word)
		end
	end
	return output[0..-2]
end

#p correction ("かまくら ばくふ は １ １ ９ ２ ねん".split(/ /).map{|x| [x,x,'名詞']})

#=begin

allUpdate = ARGV.include?("-all")

Dir.foreach('./Corpus') do |item|
	next if (item == '.') or (item == '..') or (File.extname(item).downcase != ".txt") 
	input = "./Corpus/#{item}"
	Tempfile.open("temp") do |fp|
		File.open(input, "r") do |f|
			items = []
			f.each_line do |line|
				if (line.chomp == "") then
					items = correction(items)
					items.each do |item|
						fp.puts(item.join("\t"))
					end
					fp.puts("")
					items.clear
				else
					items.push(line.split(/\t/))
				end
			end
		end
		fp.flush
		FileUtils.copy(fp.path, input)
	end
end

#=end

