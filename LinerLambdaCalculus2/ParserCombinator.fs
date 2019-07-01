namespace ParserCombinator

module Position =
    type t = (int * int * int) 

    let head = (0,1,1)

    let inc_ch ((i,l,c):t) (ch:char) =
        if ch = '\n' then (i + 1, l + 1, 1) else (i + 1, l, c + 1) 

    let inc_str ((i,l,c):t) (str:string) =
        let chars = Array.ofSeq str
        let len = Array.length chars
        let line = Array.fold (fun s x -> if x = '\n' then s + 1 else s) 0 chars
        let col = len - (match Array.tryFindIndexBack (fun x -> x = '\n') chars with | None -> 0 | Some v -> v)
        in  if len > 0 then (i + len, l + line, col + 1) else (i + len, l, c + col) 

module Reader = 
    type t = { reader: System.IO.TextReader; buffer: System.Text.StringBuilder; eof: bool ref }

    let create reader = 
        { reader = reader; buffer = System.Text.StringBuilder(); eof=ref false }

    let Item (reader:t) i = 
        let rec loop () =
            if i < reader.buffer.Length || !reader.eof = true
            then ()
            else 
                let ch = reader.reader.Read()
                in  if ch = -1 || ch = 0xFFFF
                    then reader.eof := true; () 
                    else reader.buffer.Append(char ch) |> ignore; loop ()
        let _ = loop ()
        in  if i < reader.buffer.Length then Some reader.buffer.[i] else None
    
    let submatch (reader:t) (start:int) (str:string) =
        if (start < 0) || (Item reader (str.Length + start - 1) = None) 
        then false
        else
            let rec loop (i1:int) (i2:int) = 
                if (i2 = 0) 
                then true 
                else
                    let i1, i2 = (i1-1, i2-1)
                    in  if reader.buffer.[i1] = str.[i2]
                        then loop i1 i2
                        else false
            in  loop (start + str.Length) (str.Length)

    let trunc (reader:t) (pos:Position.t) =
        let (i,l,c) = pos
        let _ = reader.buffer.Remove(0,i) 
        in  (reader, (0,l,c))

module ParserCombinator =
    type FailInformation = (Position.t * string)
    type ParserState<'a> = Success of pos:Position.t * value:'a * failInfo:FailInformation
                         | Fail    of pos:Position.t            * failInfo:FailInformation

    type Parser<'a> = Reader.t -> Position.t -> FailInformation -> ParserState<'a>

    let succ (pos:Position.t) (value:'a ) (failInfo:FailInformation) =
        Success (pos, value, failInfo)            

    let fail (pos:Position.t) (msg:string) (failInfo:FailInformation) =
        let (i,l,c) = pos
        let ((fi,fl,fc),_) = failInfo
        in  if i > fi 
            then Fail (pos, (pos, msg)) 
            else Fail (pos, failInfo) 

    let action (act: ((Reader.t * Position.t * FailInformation) -> 'a)) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let v = act (reader,pos,failInfo)
            in  succ pos v failInfo 

    // 文字 ch を受理するパーサを作る
    let char ch = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            in
                match Reader.Item reader i with
                | Some c -> if c = ch then succ (Position.inc_ch pos c) ch failInfo 
                                        else fail pos (sprintf "char: not match character %c." c) failInfo
                | None -> fail pos "char: Reached end of file while parsing." failInfo

    // 述語 pred が真を返す文字を受理するパーサを作る
    let charOf (pred : char -> bool) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            in
                match Reader.Item reader i with
                | Some ch -> if pred ch then succ (Position.inc_ch pos ch) ch failInfo 
                                        else fail pos (sprintf "char: not match character %c." ch) failInfo
                | None -> fail pos "char: Reached end of file while parsing." failInfo

    // 文字列 str に含まれる文字を受理するパーサを作る
    let oneOf (str:string) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            in
                match Reader.Item reader i with
                | Some ch -> if str.IndexOf(ch) <> -1 then succ (Position.inc_ch pos ch) ch failInfo 
                                                      else fail pos (sprintf "oneOf: not match character %c." ch) failInfo
                | None -> fail pos "oneOf: Reached end of file while parsing." failInfo

    // 文字列 str を受理するパーサを作る
    let str (str:string) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i 
            in
                if Reader.submatch reader i str 
                then succ (Position.inc_str pos str) str failInfo
                else fail pos (sprintf "str: require is '%s' but not exists." str) failInfo

    // EOF以外の任意の位置文字を受理するパーサを作る
    let anyChar () = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            in
                match Reader.Item reader i with
                | Some ch -> succ (Position.inc_ch pos ch) ch failInfo 
                | None -> fail pos "anyChar: Reached end of file while parsing." failInfo

    // EOFを受理するパーサを作る
    let eof () = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            in
                match Reader.Item reader i with
                | Some ch -> fail pos (sprintf "eof: Request end of file but got %c" ch) failInfo
                | None -> succ pos () failInfo 


    // パーサ parser が失敗することを期待するパーサを作る
    // 入力は消費しない
    let not (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    _ -> succ pos () failInfo
            | Success _ -> fail pos "not: require rule was fail but success." failInfo

    // パーサ parser が成功するか先読みを行うパーサを作る
    // 入力は消費しない
    let look (parser:Parser<'a>) = not (not (parser))

    // パーサ parser の結果に述語 pred を適用して結果を射影するパーサを作る
    let select (pred:'a->'b) (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (pos, value, max2) -> succ pos (pred value) max2

    // パーサ parser の結果に述語 pred を適用して真ならば継続するパーサを作る
    let where (pred:'a->bool) (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (_, value, max2) as f -> if pred value then f else fail pos "where: require rule was fail but success." max2

    // パーサ parser の結果に述語 pred を適用してSomeならば継続するパーサを作る
    let where_select (pred:'a->'b option) (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (pos', value, max2) as f -> 
                match pred value with
                | None   -> fail pos "where_select: require rule was fail but success." max2
                | Some v -> succ pos' v max2 

    // パーサ parser を省略可能なパーサを作る
    let option (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->       
            match parser reader pos failInfo with
            | Fail (pos, max2)  -> succ pos None max2
            | Success (pos, value, max2) -> succ pos (Some value) max2

    // パーサ列 parsers を順に試し、最初に成功したパーサの結果か、すべて成功しなかった場合は失敗となるパーサを作る
    let choice(parsers:Parser<'a> list) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:Position.t) (failInfo:FailInformation) =
                match parsers with
                | []   -> fail pos "choice: not match anyChar rules." failInfo
                | x::xs -> 
                    match x reader pos failInfo with
                    | Fail (_, max2) -> loop xs pos max2
                    | Success _ as ret -> ret;
            in loop parsers pos failInfo;

    // パーサ parser の 0 回以上の繰り返しに合致するパーサを作る
    let repeat (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            let rec loop pos values failInfo = 
                match parser reader pos failInfo with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in loop pos [] failInfo

    // パーサ parser の 1 回以上の繰り返しに合致するパーサを作る
    let repeat1 (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            let rec loop pos values failInfo = 
                match parser reader pos failInfo with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in 
                match parser reader pos failInfo with
                | Fail    (pos, max2) -> fail pos "repeat1: not match rule" max2
                | Success (pos, value, max2) -> loop pos [value] max2

    // パーサ lhs と パーサ rhs を連結したパーサを作る
    let andBoth (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            match lhs reader pos failInfo with
            | Fail    (pos1, max2) -> fail pos "andBoth: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andBoth: not match right rule" max3
                | Success (pos2, value2, max3) -> succ pos2 (value1, value2) max3 

    // パーサ lhs と パーサ rhs を連結したパーサを作る
    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            match lhs reader pos failInfo with
            | Fail    (pos1, max2) -> fail pos "andRight: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andRight: not match right rule" max3
                | Success (pos2, value2, max3) ->  succ pos2 value2 max3

    // パーサ lhs と パーサ rhs を連結したパーサを作る
    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)-> 
            match lhs reader pos failInfo with
            | Fail    (pos1, max2) -> fail pos "andLeft: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andLeft: not match left rule" max3
                | Success (pos2, value2, max3) -> succ pos2 value1 max3 

    // パーサ sep が区切りとして出現するパーサ parser の1回以上の繰り返しに合致するパーサを作る
    let repeatSep1 (sep:Parser<'b>) (parser:Parser<'a>) = 
        (andBoth (repeat (andRight parser sep)) parser) |> select (fun (x,xs) -> x::xs) 

    // パーサ sep が区切りとして出現するパーサ parser の0回以上の繰り返しに合致するパーサを作る
    let repeatSep (sep:Parser<'b>) (parser:Parser<'a>) = 
        (repeatSep1 sep parser) |> option |> select (function | None -> [] | Some xs -> xs) 
    
    // 遅延評価が必要となるパーサを作る
    let quote (parser:unit -> Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  (parser ()) reader pos failInfo

    // 書き換え可能なパーサを作る
    let hole () = 
        ref (fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  failwith "hole syntax")

    // パーサのメモ化制御オブジェクト型
    type Memoizer = { 
        add: (unit -> unit) -> unit;    // メモ化のリセット時に呼び出される関数を登録する
        reset : unit -> unit            // メモ化をリセットする
    }

    // パーサのメモ化制御オブジェクトを作る
    let memoizer () = 
        let handlers = ref List.empty
        let add (handler:unit -> unit) = handlers := handler :: !handlers
        let reset () = List.iter (fun h -> h()) !handlers
        in  { add = add; reset = reset }

    // パーサ parser をメモ化したパーサを作る
    let memoize (memoizer: Memoizer) (parser : Parser<'a>) = 
        let dic = System.Collections.Generic.Dictionary<(Reader.t * int * FailInformation), ParserState<'a>> ()
        let _ = memoizer.add (fun () -> dic.Clear() )
        in  fun reader pos failInfo -> 
                let (i,l,c) = pos 
                let key = (reader,i,failInfo) 
                in  match dic.TryGetValue(key) with 
                    | true, r -> r
                    | _       -> dic.[key] <- parser reader pos failInfo;
                                 dic.[key]

    module ComputationExpressions =
        exception ParseFail of (string)

        // Parser (ﾟ∀ﾟ) Monad!
        type ParserBuilder() =
            member __.Bind(m, f) = 
                fun (reader:Reader.t) (pos:Position.t) (fail:FailInformation) -> 
                    match m reader pos fail with 
                    | Fail    (pos',       fail') -> Fail (pos, fail) 
                    | Success (pos',value',fail') -> f value' reader pos' fail'
            member __.Return(x)  = 
                fun (reader:Reader.t) (pos:Position.t) (fail':FailInformation) -> 
                    try succ pos x fail' with 
                    | ParseFail msg -> fail pos msg fail'

        let parser = ParserBuilder()

        // Repeat (ﾟ∀ﾟ) Monad!
        type RepeatBuilder() = 
            inherit ParserBuilder() 
            member __.Run(m) = repeat m
            
        let rep = RepeatBuilder()

        // Option (ﾟ∀ﾟ) Monad!
        type OptionBuilder() = 
            inherit ParserBuilder() 
            member __.Run(m) = option m
            
        let opt = OptionBuilder()

        type DelayBuilder() =
            inherit ParserBuilder() 
            member __.Run(m) = quote(fun () -> m)

        let delay = DelayBuilder()
        

    module OperatorExtension =
        open System.Runtime.CompilerServices;
        [<Extension>]
        type IEnumerableExtensions() =  
            [<Extension>]
            static member inline Not(self: Parser<'T>) = not self
            [<Extension>]
            static member inline And(self: Parser<'T1>, rhs:Parser<'T2>) = andBoth rhs self
            [<Extension>]
            static member inline AndL(self: Parser<'T1>, rhs:Parser<'T2>) = andLeft rhs self
            [<Extension>]
            static member inline AndR(self: Parser<'T1>, rhs:Parser<'T2>) = andRight rhs self
            [<Extension>]
            static member inline Or(self: Parser<'T>, rhs:Parser<'T>) = choice [self; rhs]
            [<Extension>]
            static member inline Many(self: Parser<'T>) = repeat self 
            [<Extension>]
            static member inline Many(self: Parser<'T>, sep:Parser<'U>) = repeatSep sep self 
            [<Extension>]
            static member inline Many1(self: Parser<'T>) = repeat1 self 
            [<Extension>]
            static member inline Many1(self: Parser<'T>, sep:Parser<'U>) = repeatSep1 sep self 
            [<Extension>]
            static member inline Option(self: Parser<'T>) = option self 
            [<Extension>]
            static member inline Select(self: Parser<'T1>, selector:'T1 -> 'T2) = select selector self 
            [<Extension>]
            static member inline Where(self: Parser<'T1>, selector:'T1 -> bool) = where selector self 
            [<Extension>]
            static member inline WhereSelect(self: Parser<'T1>, selector:'T1 -> 'T2 option) = (where_select selector self)
