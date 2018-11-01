require "minitest"

include Minitest::Assertions

class << self
  attr_accessor :assertions
end
self.assertions = 0

TYPEDATA = {
	:char   => { :size=>1, :align=>1 },
	:short  => { :size=>2, :align=>2 },
	:int    => { :size=>4, :align=>4 },
	:long   => { :size=>4, :align=>4 },
	:uchar  => { :size=>1, :align=>1 },
	:ushort => { :size=>2, :align=>2 },
	:uint   => { :size=>4, :align=>4 },
	:ulong  => { :size=>4, :align=>4 },
}

def sizeof(type)
  return TYPEDATA[type][:size]
end

def alignof(type)
  return TYPEDATA[type][:align]
end

def calc_layout(members, pack = nil) 
  currentbit = 0;
  ret = []
  members.each do |(ctype, name, bitwidth)|
    bitsize = bitwidth != nil ? bitwidth : sizeof(ctype) * 8
    align = alignof(ctype) * 8

    if bitsize == 0
      # packの有無に関係なく、幅0のビットフィールド指定は指定した型のアライメントにオフセット強制的にそろえるので溢れビットを算出しておく
      overflowbitSize = (currentbit) % align
      paddingName = name
    elsif pack != nil
      # packありの場合はとにかく詰めるので溢れビットは不要
      overflowbitSize = 0
      paddingName = nil
    else
      # pack無し

      # 境界を跨ぐか判定
      headAlign = ((currentbit            ) / align).to_i
      tailAlign = ((currentbit+bitsize - 1) / align).to_i

      if (headAlign != tailAlign )
        # 境界を跨ぐので溢れビットを算出しておく
        overflowbitSize = (currentbit) % align
      else
        # パディング挿入なし
        overflowbitSize = 0
      end
      paddingName = nil
    end

    # 溢れビットがあるならアライメントにそろえるためのパディングを挿入
    if overflowbitSize > 0
      paddingbitsize = align - overflowbitSize
      ret << [ctype , paddingName, (currentbit / 8).to_i, (currentbit % 8), paddingbitsize]
      currentbit += paddingbitsize
    end
    
    # フィールドがサイズを持つなら挿入
    if bitsize > 0
      ret << [ctype, name, (currentbit / 8).to_i, (currentbit % 8), bitsize]
      currentbit += bitsize
    end

  end
  
  if pack != nil
    # packあり
    paddingbitsize = (currentbit) % (pack*8)
    if paddingbitsize > 0
      ret << [:int, nil, (currentbit / 8).to_i, (currentbit % 8), (pack*8) - paddingbitsize]
    end
  else
    # pack無し
    alignType = members.max {|(ctype1, _, _), (ctype2, _, _)| TYPEDATA[ctype1][:align] <=> TYPEDATA[ctype2][:align] }[0]
    align = TYPEDATA[alignType][:align] * 8
    paddingbitsize = currentbit % align
    if paddingbitsize > 0
      ret << [alignType, nil, (currentbit / 8).to_i, (currentbit % 8), align - paddingbitsize]
    end
  end
  
  return ret
end

struct = [ 
	[:int  , 'x', 7],
	[:short, 'y', 7],
	[:char , 'z', 7],
	[:short, 'w', 15],
]


pack1_layout = calc_layout(struct,1)
assert_equal pack1_layout, [ 
	[:int  , 'x',  0, 0,  7],
	[:short, 'y',  0, 7,  7],
	[:char , 'z',  1, 6,  7],
	[:short, 'w',  2, 5, 15],
	[:int  , nil,  4, 4,  4],
]

pack2_layout = calc_layout(struct,2)
assert_equal pack2_layout, [ 
	[:int  , 'x',  0, 0,  7],
	[:short, 'y',  0, 7,  7],
	[:char , 'z',  1, 6,  7],
	[:short, 'w',  2, 5, 15],
	[:int  , nil,  4, 4, 12],
]

pack4_layout = calc_layout(struct,4)
assert_equal pack4_layout, [ 
	[:int  , 'x',  0, 0,  7],
	[:short, 'y',  0, 7,  7],
	[:char , 'z',  1, 6,  7],
	[:short, 'w',  2, 5, 15],
	[:int  , nil,  4, 4, 28],
]

pack8_layout = calc_layout(struct,8)
assert_equal pack8_layout, [ 
	[:int  , 'x',  0, 0,  7],
	[:short, 'y',  0, 7,  7],
	[:char , 'z',  1, 6,  7],
	[:short, 'w',  2, 5, 15],
	[:int  , nil,  4, 4, 28],
]

unpack_layout = calc_layout(struct)
assert_equal unpack_layout, [ 
	[:int  , 'x',  0, 0,  7],	# ４バイトアライメントを跨がないのでそのまま入れる
	[:short, 'y',  0, 7,  7],	# ２バイトアライメントを跨がないのでそのまま入れる
	[:char , nil,  1, 6,  2],	
	[:char , 'z',  2, 0,  7],	# １バイトアライメントを跨ぐので直前に隙間を入れる
	[:short, nil,  2, 7,  9],	
	[:short, 'w',  4, 0, 15],	# ２バイトアライメントを跨ぐので直前に隙間を入れる
	[:int  , nil,  5, 7, 17],	# メンバ中最大のアライメントにそろえる
]

assert_equal calc_layout([
	[:int, 'v', 31],
	[:int, 's', 1],
]), [
	[:int, "v", 0, 0, 31], 
	[:int, "s", 3, 7,  1]
]

assert_equal calc_layout([
	[:short, 'x', 4],
	[:char, 'y', 1],
	[:short, nil, 0],
	[:char, 'w', 4],
]), [
	[:short, "x", 0, 0, 4], 
	[:char, "y", 0, 4, 1], 
	[:short, nil, 0, 5, 11], 
	[:char, "w", 2, 0, 4], 
	[:short, nil, 2, 4, 12], 
]

assert_equal calc_layout([
	[:char , 'data1'],
	[:short, 'data2'],
	[:int  , 'data3'],
	[:char , 'data4'],
]), [
	[:char, "data1", 0, 0, 8], 
	[:short, nil, 1, 0, 8], 
	[:short, "data2", 2, 0, 16], 
	[:int, "data3", 4, 0, 32], 
	[:char, "data4", 8, 0, 8], 
	[:int, nil, 9, 0, 24]
]
