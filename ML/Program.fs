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
    type t = { reader: System.IO.TextReader; buffer: System.Text.StringBuilder; }

    let create reader = 
        { reader = reader; buffer = System.Text.StringBuilder(); }

    let Item (reader:t) i = 
        let rec loop () =
            if i < reader.buffer.Length 
            then ()
            else 
                let ch = reader.reader.Read()
                in  if ch = -1 
                    then () 
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

module Interpreter =
    module Type = 
        type tyvar = int

        type t = 
              TyInt  
            | TyBool 
            | TyStr
            | TyFunc of t * t
            | TyList of t
            | TyVar of tyvar
            | TyUnit
            | TyTuple of t list

        let rec ToString ty =
            match ty with
            | TyInt -> "int"
            | TyBool -> "bool"
            | TyStr -> "string"
            | TyFunc (arg, ret)-> sprintf "%s -> %s" (ToString arg) (ToString ret)
            | TyList ty -> sprintf "%s list" (ToString ty)
            | TyVar n -> sprintf "'%c" (char n + 'a')
            | TyUnit -> "()"
            | TyTuple tys -> List.map ToString tys |> String.concat " * " |> sprintf "(%s)"

        let fresh_tyvar = 
            let counter = ref 0
            let body () = let v = !counter in counter := v + 1; v
            in  body

        let freevar_ty ty =
            let rec loop ty ret =
                match ty with
                | t.TyInt  -> ret
                | t.TyBool -> ret
                | t.TyStr -> ret
                | t.TyFunc (tyarg,tyret) -> loop tyarg ret |> loop tyret
                | t.TyList ty -> loop ty ret
                | t.TyVar id -> Set.add id ret     
                | t.TyUnit -> ret
                | t.TyTuple tys -> List.fold (fun s x -> loop x s) ret tys
            in loop ty Set.empty

    module Syntax =
        type texp =
              TVar of string
            | TString
            | TInt
            | TBool
            | TFun of texp * texp
            | TList of texp
            | TTuple of texp list
            | TUnit
            | TNamed of string
            | TConstruct of (texp list) * texp

        type id = string

        type binOp = Plus | Minus | Mult | Divi | Lt | Gt | Le | Ge | Eq | Ne | Cons

        type pattern = 
              VarP of id
            | AnyP
            | ILitP of int
            | BLitP of bool
            | SLitP of string
            | LLitP of pattern list
            | TLitP of pattern list
            | NilP
            | ConsP of pattern * pattern
            | UnitP
 
        let rec scanPatV pattern ret  =
            match pattern with
            | VarP name -> name :: ret 
            | AnyP -> ret
            | ILitP _ -> ret
            | BLitP _ -> ret 
            | SLitP _ -> ret 
            | LLitP items -> List.fold (fun s x -> scanPatV x s) ret items
            | TLitP items -> List.fold (fun s x -> scanPatV x s) ret items
            | NilP -> ret
            | UnitP -> ret
            | ConsP (car, cdr) -> scanPatV car ret |> scanPatV cdr

        type lettype =
            | LVar of pattern
            | LFun of id

        type exp =
              Var of id
            | ILit of int
            | BLit of bool
            | SLit of string
            | Unit
            | LLit of exp list
            | TLit of exp list
            | BinOp of binOp * exp * exp
            | IfExp of exp * exp * exp
            | SeqExp of (exp * exp)
            //| LetExp of (id * exp) list list * exp
            | LetExp of (lettype * exp) list list * exp
            //| LetRecExp of (id * exp) list list * exp
            | LetRecExp of (lettype * exp) list list * exp
            //| FunExp of (id * exp) 
            | FunExp of (pattern * exp) 
            | MatchExp of (exp * (pattern * exp) list)
            | AppExp  of (exp * exp)

        type program =
            | ExpStmt of exp
            //| LetStmt of (id * exp) list list
            | LetStmt of (lettype * exp) list list
            //| LetRecStmt of (id * exp) list list
            | LetRecStmt of (lettype * exp) list list

    module Parser =
        open ParserCombinator
        open ParserCombinator.OperatorExtension
        open ParserCombinator.ComputationExpressions
        open Syntax;

        let isLower ch = 'a' <= ch && ch <= 'z'
        let isUpper ch = 'A' <= ch && ch <= 'Z'
        let isDigit ch = '0' <= ch && ch <= '9'
        let isIdHead ch = isLower(ch)
        let isIdBody ch = isLower(ch) || isDigit(ch) || (ch = '_') || (ch = '\'') 

        let escapeString str =
            let rec loop chars ret =
                match chars with
                | [] -> ret |> List.rev |> String.concat ""
                | x::xs -> 
                    let s =
                        match x with
                        | '\a' -> "\\a"
                        | '\b' -> "\\b"
                        | '\f' -> "\\f"
                        | '\n' -> "\\n"
                        | '\r' -> "\\r"
                        | '\t' -> "\\t"
                        | '\v' -> "\\v"
                        | '\\' -> "\\\\"
                        | '"'  -> "\\\""
                        | _ -> x.ToString()
                    in  loop xs (s::ret)
            in  loop (Seq.toList str) List.empty
            
        let unescapeString str =
            let rec loop chars (ret:char list) =
                match chars with
                | [] -> ret |> List.rev |> System.String.Concat
                | _ -> 
                    let (x,xs) =
                        match chars with
                        | '\\'::'a'::xs -> '\a',xs
                        | '\\'::'b'::xs -> '\b',xs
                        | '\\'::'f'::xs -> '\f',xs
                        | '\\'::'n'::xs -> '\n',xs
                        | '\\'::'r'::xs -> '\r',xs
                        | '\\'::'t'::xs -> '\t',xs
                        | '\\'::'v'::xs -> '\v',xs
                        | '\\'::'\\'::xs -> '\\',xs
                        | '\\'::'"'::xs  -> '"',xs
                        | x::xs -> x,xs
                        | [] -> failwith "not reach"
                    in  loop xs (x::ret)
            in  loop (Seq.toList str) List.empty

        let m = memoizer()

        let COMMENT_ = hole()
        let COMMENT = quote (fun () -> !COMMENT_)
        let _ = COMMENT_ := parser {
                                let! _ = str "(*" 
                                let! _ = (choice [
                                            parser { let! _ = look (str "(*") in let! _ = COMMENT in return () };
                                            parser { let! _ = not (str "*)") in let! _ = anyChar () in return () }
                                        ]).Many()
                                let! _ = str "*)"
                                return () 
                            }

        let SPACE = (oneOf " \t\r\n").Select(fun _ -> ())
        let WS = (choice [SPACE;COMMENT]).Many()
        let ws x = WS.AndR(x)  |> memoize m

        //let Ident = ws( (char isIdHead).And((char isIdBody).Many().AsString()).Select(fun (h,b) -> h.ToString() + b) )
        let Ident = ws (parser {
            let! x = charOf isIdHead
            let! xs = (charOf isIdBody).Many()
            return (x::xs) |> System.String.Concat
        })

        let res word = Ident.Where(fun x -> x = word)
        let TRUE = res "true"
        let FALSE = res "false"
        let IF = res "if"
        let THEN = res "then"
        let ELSE = res "else"
        let LET = res "let"
        let IN = res "in"
        let AND = res "and"
        let FUN = res "fun"
        let REC = res "rec"
        let MATCH = res "match"
        let WITH = res "with"
        let INT = res "int"
        let STRING = res "string"
        let BOOL = res "bool"
        let ID = Ident.Where(fun x -> (List.contains x ["true";"false";"if";"then";"else";"let";"in";"and";"fun";"rec";"match";"with"]) = false);
        //let INTV = ws( (char (fun x -> x = '-')).Option().And((char isDigit).Many1().AsString().Select(System.Int32.Parse)).Select(fun (s,v) -> if s.IsSome then (-v) else v))
        let INTV = ws( parser {
            let! sign = (char '-').Option()
            let! digits = (charOf isDigit).Many1().Select(System.String.Concat)
            let v = System.Int32.Parse digits
            return (if sign.IsSome then -v else v) 
        })
        let STRV = 
            let DQuote = char '"'
            let Escaped = (char '\\' ).And(oneOf("abfnrtv\\\"")).Select(fun (x,y) -> sprintf "%c%c" x y)
            let Char = anyChar().Select(fun x -> x.ToString())
            let StrChr = DQuote.Not().AndR(Escaped.Or(Char))
            //in  ws( DQuote.AndR(StrChr.Many()).AndL(DQuote).Select( fun x -> String.concat "" x |> unescapeString ) )
            in  ws( parser { 
                let! _ = DQuote
                let! s = StrChr.Many()
                let! _ = DQuote
                return String.concat "" s |> unescapeString
            })
        let LPAREN = ws(char '(')
        let RPAREN = ws(char ')')
        let MULT = ws(str "*")
        let DIV = ws(str "/")
        let PLUS = ws(str "+")
        let MINUS= ws(str "-")
        let EQ = ws(str "=")
        let NE = ws(str "<>")
        let LE = ws(str "<=")
        let GE = ws(str ">=")
        let LT = ws(str "<")
        let GT = ws(str ">")
        let RARROW = ws(str "->")
        let SEMISEMI = ws(str ";;")
        let ANDAND = ws(str "&&")
        let OROR = ws(str "||")
        let LBRACKET = ws(char '[')
        let RBRACKET = ws(char ']')
        let COLCOL = ws(str "::")
        let BAR = ws(str "|")
        let SEMI = ws(str ";")
        let US = ws(str "_")
        let COMMA = ws(str ",")
        let QUOTE = ws(str "'")
        let COL = ws(str ":")

        let TypeExpr_ = hole()
        let TypeExpr = quote (fun () -> !TypeExpr_)

        let SimpleType = 
            let tcon x id =
                match id,x with
                | "list", x::[] -> TList x
                | "list", _ -> raise (ParseFail "list type need 1 argument")
                | _,[] -> failwith "need 1 arg"
                | _ -> TConstruct(x,TNamed id)
            in
                (choice [
                    QUOTE.AndR(ID).Select(fun x -> TVar x);
                    INT.Select(fun x -> TInt);
                    BOOL.Select(fun x -> TBool);
                    STRING.Select(fun x -> TString);
                    LPAREN.And(RPAREN).Select(fun _ -> TUnit);
                    ID.Select(fun x -> TConstruct ([], TNamed x));
                    LPAREN.AndR(TypeExpr.Many1(COMMA)).AndL(RPAREN).And(ID).Select(fun (xs,id) -> tcon xs id );
                    LPAREN.AndR(TypeExpr).AndR(TypeExpr).AndL(RPAREN);
                ]).And(ID.Many()).Select(fun (x,ys) -> List.fold (fun s x -> tcon [s] x) x ys)

        let TupleType = SimpleType.Many1(MULT).Select(function | x::[] -> x | xs -> TTuple xs)

        let FuncType = TupleType.Many1(RARROW).Select(List.reduceBack (fun x s -> TFun (x,s)))

        let _ = TypeExpr_ := FuncType 

        let Expr_ = hole ()
        let Expr = quote(fun () -> !Expr_)|> memoize m

        let ItemExpr_ = hole ()
        let ItemExpr = quote(fun () -> !ItemExpr_)|> memoize m
        
        let BinOpExpr = 
            let make (tok,op) = tok.Select(fun _ -> FunExp(VarP "@lhs", FunExp(VarP "@rhs", BinOp (op,Var "@lhs",Var "@rhs"))))
            in
                choice (List.map make [ PLUS,Plus;
                                        MINUS,Minus;
                                        MULT,Mult;
                                        DIV,Divi;
                                        EQ,Eq;
                                        NE,Ne;
                                        LT,Lt;
                                        GT,Gt;
                                        LE,Le;
                                        GE,Ge;
                                        COLCOL,Cons
                                    ])

        let Args =
            repeat1(
                choice[
                    parser {
                        let! _ = LPAREN
                        let! id = ID
                        let! ty = opt { let! _ = COL in let! expr = TypeExpr in return expr }
                        let! _ = RPAREN
                        return (id,ty)
                    };
                    parser {
                        let! id = ID
                        let! ty = opt { let! _ = COL in let! expr = TypeExpr in return expr }
                        return (id,ty)
                    };
                ]
            )

        let PatternExpr_ = hole ()
        let PatternExpr = quote(fun () -> !PatternExpr_)|> memoize m

        let PatternAExpr = 
            choice [ 
                INTV.Select(fun x -> ILitP x);
                STRV.Select(fun x -> SLitP x);
                TRUE.Select(fun x -> BLitP true);
                FALSE.Select(fun x -> BLitP false);
                ID.Select(fun x -> VarP x);
                US.Select(fun x -> AnyP );
                LBRACKET.AndR(PatternExpr.Many1(SEMI)).AndL(RBRACKET).Select(LLitP);
                LPAREN.AndR(PatternExpr).AndL(RPAREN);
            ]

        let PatternConsExpr = 
            PatternAExpr.Many1(COLCOL).Select(List.reduceBack (fun x s -> ConsP(x, s)))

        let PatternTupleExpr = 
            PatternConsExpr.Many1(COMMA).Select(function | x::[] -> x | xs -> TLitP xs)

        let _ = PatternExpr_ := PatternTupleExpr

        let FunExpr =
            ////FUN.AndR(ID.Many1()).AndL(RARROW).And(Expr).Select(fun (args, e) -> List.foldBack (fun x s -> FunExp (x, s)) args e)
            //FUN.AndR(LPAREN.AndR(ID).And(COL.AndR(TypeExpr).AndL(RPAREN).Option()).Or(ID.Select(fun x -> (x,None))).Many1()).AndL(RARROW).And(Expr).Select(fun (args, e) -> List.foldBack (fun (x,t) s -> FunExp (x, s)) args e)
            parser {
                let! _ = FUN
                let! args = PatternExpr.Many1()
                let! _ = RARROW
                let! expr = Expr

                return List.foldBack (fun x s -> FunExp (x, s)) args expr
            }

        let Matching =
            let checkPattern pattern =
                let vars = scanPatV pattern []
                let dup = List.countBy (fun x -> x) vars |> List.where (fun (_,cnt) -> cnt > 1) |> List.map (fun (name,cnt) -> name)
                in
                    if List.isEmpty dup 
                    then true
                    else failwith (List.map (fun x -> sprintf "Variable %s is bound several times in this matching." x) dup |> String.concat "\n")
            in
                PatternExpr.Where(checkPattern).AndL(RARROW).And(Expr)

        let Matchings =
            BAR.Option().AndR(Matching.Many1(BAR))

        let MatchExpr =
            MATCH.AndR(Expr).AndL(WITH).And(Matchings).Select(fun (e,b) -> MatchExp (e,b))

        let IfExpr =
            IF.AndR(Expr).AndL(THEN).And(Expr).AndL(ELSE).And(Expr).Select( fun ((c,t),e) -> IfExp (c, t, e) )

        let LetFun =
            ////(ID.Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | [] -> failwith "no entry" | id::[] -> (id,e) | id::args -> (id,List.foldBack (fun x s -> FunExp (x, s)) args e))) |> memoize m
            //(ID.And(COL.AndR(TypeExpr).Option()).Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | [] -> failwith "no entry" | (id,ty)::[] -> (id,e) | (id,ty)::args -> (id,List.foldBack (fun (x,t) s -> FunExp (x, s)) args e))) |> memoize m
            parser {
                let! id = ID
                let! args = PatternExpr.Many1()
                let! _ = EQ
                let! e = Expr
                let ret =
                    match args with 
                    | [] -> failwith "no arg" 
                    | args -> (LFun id,List.foldBack (fun x s -> FunExp (x, s)) args e)
                return ret
            }

        let LetVar =
            ////(ID.Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | [] -> failwith "no entry" | id::[] -> (id,e) | id::args -> (id,List.foldBack (fun x s -> FunExp (x, s)) args e))) |> memoize m
            //(ID.And(COL.AndR(TypeExpr).Option()).Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | [] -> failwith "no entry" | (id,ty)::[] -> (id,e) | (id,ty)::args -> (id,List.foldBack (fun (x,t) s -> FunExp (x, s)) args e))) |> memoize m
            parser {
                //let! (id,ty) = 
                //        choice [
                //            parser {
                //                let! _ = LPAREN
                //                let! id = ID
                //                let! ty = option ( COL.AndR(TypeExpr) )
                //                let! _ = RPAREN
                //                return (id,ty)
                //            };
                //            parser {
                //                let! id = ID
                //                let! ty = option ( COL.AndR(TypeExpr) )
                //                return (id,ty)
                //            };
                //        ]
                //let! _ = EQ
                //let! e = Expr

                //return (id,e)
                let! pat = PatternExpr
                let! _ = EQ
                let! e = Expr

                return (LVar pat,e)
            }

        let LetPrim = (choice [LetFun;LetVar]) |> memoize m

        let LetAndExpr = parser {
            let! _  = LET
            let! xs = LetPrim.Many1(AND)
            return xs
        }

        let LetRecAndExpr = parser {
            let! _  = LET
            let! _  = REC
            let! xs = LetPrim.Many1(AND)
            return xs
        }

        let LetAndExprs = LetAndExpr.Many1() |> memoize m
        let LetRecAndExprs = LetRecAndExpr.Many1() |> memoize m

        let LetExpr = 
            choice [
                LetRecAndExprs.AndL(IN).And(Expr).Select(LetRecExp);
                parser {
                    let! bindings = LetAndExprs
                    let! _ = IN
                    let! expr = Expr
                    return (LetExp (bindings,expr))
                }
            ]

        let AExpr = 
            choice [ 
                FunExpr; 
                INTV.Select(fun x -> ILit x);
                STRV.Select(fun x -> SLit x);
                TRUE.Select(fun _ -> BLit true);
                FALSE.Select(fun _ -> BLit false);
                ID.Select(fun x -> Var x);
                IfExpr; 
                MatchExpr;
                LetExpr; 
                LBRACKET.AndR(ItemExpr.Many(SEMI)).AndL(RBRACKET).Select(LLit);
                LPAREN.And(RPAREN).Select(fun _ -> Unit);
                LPAREN.AndR(BinOpExpr).AndL(RPAREN);
                LPAREN.AndR(Expr).AndL(RPAREN)
            ]

        let AppExpr = parser {
            let! fn = AExpr
            let! args = rep { let! _ = not(LET) in let! arg = AExpr in return arg }
            return (List.fold (fun s x -> AppExp (s, x) ) fn args)
        }

        let MExpr = 
            let op = choice[
                        MULT.Select(fun _ -> Mult);
                        DIV.Select(fun _ -> Divi)
                     ]
            in
                AppExpr.And(op.And(AppExpr).Many()).Select(fun (l,r) -> List.fold (fun l (op,r) -> BinOp (op, l, r)) l r)

        let PExpr = 
            let op = choice[
                        PLUS.Select(fun _ -> Plus);
                        MINUS.Select(fun _ -> Minus)
                     ]
            in
                MExpr.And(op.And(MExpr).Many()).Select(fun (l,r) -> List.fold (fun l (op,r) -> BinOp (op, l, r)) l r)

        let ConsExpr = 
            PExpr.Many1(COLCOL).Select(List.reduceBack (fun x s -> BinOp(Cons, x, s)) )

        let EqExpr = 
            let op = choice[
                        EQ.Select(fun _ -> Eq);
                        NE.Select(fun _ -> Ne)
                    ]
            in
                choice [ 
                    ConsExpr.And(op).And(ConsExpr).Select(fun ((l,op), r) -> BinOp (op, l, r) )
                    ConsExpr;
                ]

        let LTExpr = 
            let op = choice[
                        LE.Select(fun _ -> Le);
                        GE.Select(fun _ -> Ge);
                        LT.Select(fun _ -> Lt);
                        GT.Select(fun _ -> Gt)
                    ]
            in
                choice [ 
                    EqExpr.And(op).And(EqExpr).Select(fun ((l,op), r) -> BinOp (op, l, r) );
                    EqExpr;
                ]

        let LAndExpr = 
            choice [ 
                LTExpr.AndL(ANDAND).And(LTExpr).Select(fun (l,r) -> IfExp (l, r, BLit false));
                LTExpr;
            ]

        let LOrExpr = 
            choice [ 
                LAndExpr.AndL(OROR).And(LAndExpr).Select(fun (l,r) -> IfExp (l, BLit true, r));
                LAndExpr;
            ]

        let TupleExpr = 
                LOrExpr.Many1(COMMA).Select(function | x::[] -> x | xs -> TLit (xs))

        let SeqExpr =
                TupleExpr.Many1(SEMI).Select(List.reduceBack (fun x s -> SeqExp(x,s)))

        let _ = Expr_ := SeqExpr
        let _ = ItemExpr_ := TupleExpr

        let LetStmt = 
            LetAndExprs.AndL(not IN).Select( LetStmt )

        let LetRecStmt =
            LetRecAndExprs.AndL(not IN).Select( LetRecStmt )

        let ExprStmt =
            Expr.Select( fun x -> ExpStmt x)

        let toplevel = (action (fun _ -> m.reset() )).AndR(choice[ LetRecStmt; LetStmt; ExprStmt ].AndL(SEMISEMI))
        
        let errorRecover reader p = 
            let token = ws(choice [
                            Ident;
                            RARROW;
                            SEMISEMI;
                            ANDAND;
                            OROR;
                            LBRACKET.Select(fun x -> x.ToString());
                            RBRACKET.Select(fun x -> x.ToString());
                            COLCOL;
                            BAR;
                            SEMI;
                            US;
                            INTV.Select(fun x -> x.ToString());
                            LPAREN.Select(fun x -> x.ToString());
                            RPAREN.Select(fun x -> x.ToString());
                            MULT;DIV;PLUS;MINUS;EQ;NE;LE;GE;LT;GT;
                            anyChar().Select(fun x -> x.ToString())
                        ])
            let rec loop p lp =
                match token reader p (p,"") with
                | ParserCombinator.Success (p,t,_) -> 
                    match t,lp with 
                    | ("(",_) | ("[",_) -> loop p (t::lp)
                    | (")","("::lp) | ("]","["::lp) -> loop p lp
                    | (";;",[]) -> (reader, p)
                    | _ -> loop p lp
                | ParserCombinator.Fail (p,(fp,msg)) -> failwith "cannot recover from error"
            in  loop p []

    module Environment =

        type 'a t = (Syntax.id * 'a) list
            
        exception Not_bound
        
        let empty = []
        
        let extend x v env = (x,v)::env
        
        let rec lookup x env =
            match List.tryFind (fun (id,v) -> id = x) env with 
            | None -> raise Not_bound
            | Some (id,v) -> v
        
        let rec map f = function
            | [] -> []
            | (id, v)::rest -> (id, f v) :: map f rest

        let rec fold_right f env a =
            match env with
            | [] -> a
            | (_, v)::rest -> f v (fold_right f rest a)

    module Eval =
        open Syntax;

        (* Expressed values *)
        module Value = 
            type t =
                  IntV of int
                | BoolV of bool
                | StringV of string
                | ProcV of pattern * exp * t Environment.t ref
                | ConsV of t * t
                | NilV
                | UnitV
                | TupleV of t list

            let rec ToString v =
                match v with
                | IntV v -> v.ToString()
                | BoolV v -> if v then "true" else "false"
                | StringV v -> Parser.escapeString v |> sprintf "\"%s\"" 
                | ProcV (id,exp,env) -> "<fun>"
                | NilV -> "[]"
                | ConsV _ -> 
                    let rec loop v ret = 
                        match v with
                        | NilV -> List.rev ret
                        | ConsV (x,xs) -> loop xs ((ToString x)::ret)
                        | _ -> failwith "not cons or nil"
                    let items = loop v []
                    in  sprintf "[%s]" (String.concat "; " items)
                | UnitV -> "()"
                | TupleV vs -> List.map ToString vs |> String.concat ", " |> sprintf "(%s)"

        let rec apply_prim op arg1 arg2 = 
            match op, arg1, arg2 with
            | Plus, Value.IntV i1, Value.IntV i2 -> Value.IntV (i1 + i2)
            | Plus, _, _ -> failwith ("Both arguments must be integer: +")
            | Minus, Value.IntV i1, Value.IntV i2 -> Value.IntV (i1 - i2)
            | Minus, _, _ -> failwith ("Both arguments must be integer: -")
            | Mult, Value.IntV i1, Value.IntV i2 -> Value.IntV (i1 * i2)
            | Mult, _, _ -> failwith ("Both arguments must be integer: *")
            | Divi, Value.IntV i1, Value.IntV i2 -> Value.IntV (i1 / i2)
            | Divi, _, _ -> failwith ("Both arguments must be integer: /")
            | Lt, Value.IntV i1, Value.IntV i2 -> Value.BoolV (i1 < i2)
            | Lt, _, _ -> failwith ("Both arguments must be integer: <")
            | Le, Value.IntV i1, Value.IntV i2 -> Value.BoolV (i1 <= i2)
            | Le, _, _ -> failwith ("Both arguments must be integer: <=")
            | Gt, Value.IntV i1, Value.IntV i2 -> Value.BoolV (i1 > i2)
            | Gt, _, _ -> failwith ("Both arguments must be integer: >")
            | Ge, Value.IntV i1, Value.IntV i2 -> Value.BoolV (i1 >= i2)
            | Ge, _, _ -> failwith ("Both arguments must be integer: >=")
            | Eq, Value.IntV i1, Value.IntV i2 -> Value.BoolV (i1 = i2)
            | Eq, _, _ -> failwith ("Both arguments must be integer: =")
            | Ne, Value.IntV i1, Value.IntV i2 -> Value.BoolV (i1 <> i2)
            | Ne, _, _ -> failwith ("Both arguments must be integer: <>")
            | Cons, v1, Value.ConsV _ -> Value.ConsV(v1, arg2)
            | Cons, v1, Value.NilV -> Value.ConsV(v1, arg2)
            | Cons, _, _ -> failwith ("right arguments must be list: ::")

        let rec try_match value pat env =
            match pat with 
            | VarP id -> Some ((id,value) :: env)
            | AnyP -> Some env
            | ILitP v -> if value = Value.IntV v then Some env else None
            | BLitP v -> if value = Value.BoolV v then Some env else None
            | SLitP v -> if value = Value.StringV v then Some env else None
            | LLitP pats -> 
                let rec loop p v env =
                    match p, v with 
                    | [], Value.NilV -> Some env
                    | [], _ -> None
                    | (p::ps), Value.NilV -> None
                    | (p::ps), Value.ConsV(v,vs) -> 
                        match try_match v p env with
                        | Some e -> loop ps vs e
                        | None -> None
                    | _ -> None
                in  loop pats value env
            | TLitP pats -> 
                match value with 
                | Value.TupleV value ->
                    let rec loop p v env =
                        match p, v with 
                        | [], [] -> Some env
                        | [], _ -> None
                        | _, [] -> None
                        | (p::ps), (v::vs) -> 
                            match try_match v p env with
                            | Some e -> loop ps vs e
                            | None -> None
                    in  loop pats value env
                | _ -> None
            | NilP ->
                if value = Value.NilV  then Some env else None
            | ConsP (x,y) ->
                match value with
                | Value.ConsV(a,b) -> 
                    match try_match a x env with
                    | Some e -> try_match b y e
                    | None -> None
                | _ -> None
            | UnitP ->
                if value = Value.UnitV then Some env else None

        let rec eval_exp env = function
            | Var x -> 
                try Environment.lookup x env with 
                    | Environment.Not_bound -> failwithf "Variable not bound: %A" x
            | ILit i -> Value.IntV i
            | BLit b -> Value.BoolV b
            | SLit s -> Value.StringV s
            | Unit -> Value.UnitV
            | BinOp (op, exp1, exp2) ->
                let arg1 = eval_exp env exp1 in
                let arg2 = eval_exp env exp2 in
                    apply_prim op arg1 arg2
            | IfExp (exp1, exp2, exp3) ->
                let test = eval_exp env exp1 in
                    match test with
                        | Value.BoolV true -> eval_exp env exp2
                        | Value.BoolV false -> eval_exp env exp3
                        | _ -> failwith ("Test expression must be boolean: if")
            | SeqExp (exp1, exp2) ->
                let _ = eval_exp env exp1
                in  eval_exp env exp2

            | LetExp (lettype,body) ->
                let proc_binding env binding = 
                    List.fold (fun env' (lt,e) -> 
                        match lt with
                        | LVar pat -> 
                            //let v = eval_exp env e in (id, v)::env'
                            let v = eval_exp env e 
                            let vs = try_match v pat env
                            in  match vs with
                                | None -> failwith "not match"
                                | Some env -> env
                        | LFun id -> let v = eval_exp env e in (id, v)::env'
                    ) env binding
                let newenv = List.fold proc_binding env lettype
                //let newenv = List.fold (fun env (id,e) -> let v = eval_exp env e in  (id, v)::env ) env ss
                in  eval_exp newenv body

            | LetRecExp (pats,body) ->
                let dummyenv = ref Environment.empty
                //let newenv = List.fold (fun env s -> List.fold (fun env' (id,e) -> let v = match e with FunExp(id,exp) -> Value.ProcV (id,exp,dummyenv) | _ -> failwithf "variable cannot " in  (id, v)::env') env s ) env binds
                let newenv = List.fold (fun env s -> List.fold (fun env' (pat,e) -> let v = match e with FunExp(id,exp) -> Value.ProcV (id,exp,dummyenv) in  (id, v)::env' | _ -> failwithf "variable cannot " ) env s ) env pats
                in  dummyenv := newenv;
                    eval_exp newenv body

            | FunExp (pat, exp) -> 
                Value.ProcV (pat, exp, ref env) 
            
            | MatchExp (expr, matchings) ->
                let value = eval_exp env expr
                let rec loop cases =
                    match cases with
                    | [] -> failwith ("Match expr: match failure");
                    | (pattern,expr)::xs -> 
                        match try_match value pattern env with
                        | Some(env) -> eval_exp env expr
                        | None -> loop xs
                in  loop matchings 

            | AppExp (exp1, exp2) ->
                let funval = eval_exp env exp1 in
                let arg = eval_exp env exp2 in
                    match funval with
                    | Value.ProcV (pat, body, env') ->
                        //let newenv = Environment.extend id arg !env' in
                        let newenv = try_match arg pat !env'
                        in
                            match newenv with
                            | None -> failwith "not match"
                            | Some newenv -> eval_exp newenv body
                    | _ -> failwith ("Non-function value is applied")

            | LLit v -> 
                List.foldBack (fun x s -> Value.ConsV(eval_exp env x,s)) v Value.NilV
            | TLit v -> 
                Value.TupleV (List.map (fun x -> eval_exp env x) v)

        let eval_decl env = function
            | ExpStmt e -> 
                let v = eval_exp env e in (["-",v ], env) 
            | LetStmt ss -> 
                //List.fold (fun (ret,env) (id,e) -> let v = eval_exp env e in  ((id, v)::ret,(id, v)::env)) ([],env) s
                let (ret,env) = List.fold (fun (ret,env) x -> List.fold (fun (ret,env') (id,e) -> let v = eval_exp env e in  (id, v)::ret,(id, v)::env') (ret,env) x ) ([],env) ss
                in  (List.rev ret, env)
            | LetRecStmt ss -> 
                let dummyenv = ref Environment.empty
                let ret = List.fold (fun env s -> List.fold (fun (ret,env') (id,e) -> let v = match e with FunExp(id,exp) -> Value.ProcV (id,exp,dummyenv) | _ -> failwithf "variable cannot " in  (id, v)::ret,(id, v)::env') env s ) ([],env) ss
                in  dummyenv := snd ret;
                    ret

    module Typing =
        open Syntax
        type tyenv = Type.t Environment.t
        type subst = (Type.tyvar * Type.t)
        type substs = subst list
        type eqs = (Type.t*Type.t) list

        let rec subst_type (ss:substs) ty = 
            let rec subst_type' (s:subst) ty = 
                let (v, t) = s
                in  match ty with
                    | Type.TyInt -> Type.TyInt
                    | Type.TyBool -> Type.TyBool
                    | Type.TyStr -> Type.TyStr
                    | Type.TyFunc (arg, ret)-> Type.TyFunc (subst_type' s arg, subst_type' s ret)
                    | Type.TyList ty -> Type.TyList (subst_type' s ty)
                    | Type.TyVar n -> if v = n then t else ty
                    | Type.TyUnit -> Type.TyUnit
                    | Type.TyTuple tys -> Type.TyTuple (List.map (fun x -> subst_type' s x) tys)
            in
                List.fold (fun s x -> subst_type' x s) ty ss

        let eqs_of_subst (s:substs) : (Type.t*Type.t) list = 
            List.map (fun (v,t) -> (Type.TyVar v, t)) s

        let subst_eqs (s:substs) (eqs: eqs) : eqs = 
            List.map (fun (t1,t2) -> (subst_type s t1 , subst_type s t2)) eqs

        let unify (eqs:eqs) : substs =
            let rec loop eqs ret =
                match eqs with
                | [] -> ret
                | (ty1,ty2) :: eqs when ty1 = ty2 -> loop eqs ret
                | (Type.TyVar id, ty) :: eqs 
                | (ty, Type.TyVar id) :: eqs ->
                    if Set.contains id (Type.freevar_ty ty) 
                    then failwithf "unification error: type %s is include in %s" (Type.ToString (Type.TyVar id)) (Type.ToString ty)
                    else 
                        let ret = (id,ty) :: ret
                        let eqs = subst_eqs ret eqs
                        in  loop eqs ret
                | (Type.TyFunc (tyarg1, tyret1), Type.TyFunc (tyarg2, tyret2)) :: eqs  -> loop ((tyarg1, tyarg2)::(tyret1, tyret2)::eqs) ret
                | (Type.TyList ty1, Type.TyList ty2) :: eqs  -> loop ((ty1, ty2)::eqs) ret
                | (Type.TyTuple ty1, Type.TyTuple ty2) :: eqs  when List.length ty1 = List.length ty2 -> loop ((List.zip ty1 ty2) @ eqs) ret
                | (ty1, ty2)::eqs -> failwithf "unification error: type %s and type %s cannot unification" (Type.ToString ty1) (Type.ToString ty2)
            in  loop eqs List.empty
            
        let ty_prim op t1 t2 =
            match (op, t1, t2) with
            | Plus, Type.TyInt, Type.TyInt-> ([],Type.TyInt)
            | Plus, Type.TyInt, ty 
            | Plus, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyInt)
            | Plus, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyInt)

            | Minus, Type.TyInt, Type.TyInt-> ([],Type.TyInt)
            | Minus, Type.TyInt, ty 
            | Minus, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyInt)
            | Minus, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyInt)

            | Mult, Type.TyInt, Type.TyInt-> ([],Type.TyInt)
            | Mult, Type.TyInt, ty 
            | Mult, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyInt)
            | Mult, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyInt)

            | Divi, Type.TyInt, Type.TyInt-> ([],Type.TyInt)
            | Divi, Type.TyInt, ty 
            | Divi, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyInt)
            | Divi, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyInt)

            | Lt, Type.TyInt, Type.TyInt-> ([],Type.TyBool)
            | Lt, Type.TyInt, ty 
            | Lt, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyBool)
            | Lt, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyBool)

            | Le, Type.TyInt, Type.TyInt-> ([],Type.TyBool)
            | Le, Type.TyInt, ty 
            | Le, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyBool)
            | Le, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyBool)

            | Gt, Type.TyInt, Type.TyInt-> ([],Type.TyBool)
            | Gt, Type.TyInt, ty 
            | Gt, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyBool)
            | Gt, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyBool)

            | Ge, Type.TyInt, Type.TyInt-> ([],Type.TyBool)
            | Ge, Type.TyInt, ty 
            | Ge, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyBool)
            | Ge, _, _ -> ([(t1,Type.TyInt);(t2,Type.TyInt)],Type.TyBool)

            | Eq, Type.TyInt, Type.TyInt -> ([],Type.TyBool)
            | Eq, Type.TyInt, ty 
            | Eq, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyBool)
            | Eq, Type.TyBool, Type.TyBool -> ([],Type.TyBool)
            | Eq, Type.TyBool, ty 
            | Eq, ty, Type.TyBool -> ([(ty,Type.TyBool)],Type.TyBool)
            | Eq, _, _ -> let t = Type.TyVar (Type.fresh_tyvar()) in ([(t1,t);(t2,t)],Type.TyBool)

            | Ne, Type.TyInt, Type.TyInt -> ([],Type.TyBool)
            | Ne, Type.TyInt, ty 
            | Ne, ty, Type.TyInt -> ([(ty,Type.TyInt)],Type.TyBool)
            | Ne, Type.TyBool, Type.TyBool -> ([],Type.TyBool)
            | Ne, Type.TyBool, ty 
            | Ne, ty, Type.TyBool -> ([(ty,Type.TyBool)],Type.TyBool)
            | Ne, _, _ -> let t = Type.TyVar (Type.fresh_tyvar()) in ([(t1,t);(t2,t)],Type.TyBool)

            | Cons, t1, Type.TyList t2 -> let t = Type.TyVar (Type.fresh_tyvar()) in ([(t1,t);(t2,t)],Type.TyList t)
            | Cons, _, _ -> let t = Type.TyVar (Type.fresh_tyvar()) in ([(t1,t);(t2,Type.TyList t)],Type.TyList t)

        let rec ty_try_match ty pat tyenv = 
            match pat with 
            | VarP id -> (List.empty, [(id, ty)])
            | AnyP -> (List.empty, List.empty)
            | ILitP v -> ([(ty,Type.TyInt)], List.empty)
            | BLitP v -> ([(ty,Type.TyBool)], List.empty)
            | SLitP v -> ([(ty,Type.TyStr)], List.empty)
            | LLitP pats -> 
                let tyitem = Type.TyVar (Type.fresh_tyvar())
                let tylist = Type.TyList tyitem
                let rec loop p substs tyenvs =
                    match p with 
                    | [] -> ((ty,tylist)::(List.fold (fun s x -> x @ s) List.empty substs), List.fold (fun s x -> x @ s) List.empty tyenvs)
                    | (p::ps) -> 
                        let (subst, e) = ty_try_match tyitem p tyenv 
                        in  loop ps (subst::substs) (e::tyenvs)
                in  loop pats List.empty List.empty
            | NilP -> ([(ty,Type.TyVar (Type.fresh_tyvar()))], List.empty)
            | ConsP (x,y) ->
                let tyitem = Type.TyVar (Type.fresh_tyvar())
                let tylist = Type.TyList tyitem
                let (t1, e1) = ty_try_match tyitem x tyenv 
                let (t2, e2) = ty_try_match tylist y tyenv 
                in  ((ty,tylist)::(t1@t2), e1@e2)
            | UnitP -> ([(ty,Type.TyUnit)], List.empty)
            | TLitP pats -> 
                let rec loop p substs tyenvs tyitems =
                    match p with 
                    | [] -> 
                        let tytuple = Type.TyTuple (List.rev tyitems)
                        in  ((ty,tytuple)::(List.fold (fun s x -> x @ s) List.empty substs), List.fold (fun s x -> x @ s) List.empty tyenvs)
                    | (p::ps) -> 
                        let tyitem = Type.TyVar (Type.fresh_tyvar())
                        let (subst, e) = ty_try_match tyitem p tyenv
                        in  loop ps (subst::substs) (e::tyenvs) (tyitem::tyitems)
                in  loop pats List.empty List.empty List.empty


        let rec ty_exp tyenv = function
            | Var x -> 
                try ([], Environment.lookup x tyenv) with 
                    | Environment.Not_bound -> failwithf "Variable not bound: %A" x
            | ILit i -> ([], Type.TyInt)
            | BLit b -> ([], Type.TyBool)
            | SLit b -> ([], Type.TyStr)
            | Unit -> ([], Type.TyUnit)
            | BinOp (op, exp1, exp2) ->
                let (s1, arg1) = ty_exp tyenv exp1
                let (s2, arg2) = ty_exp tyenv exp2
                let (eqs, ret) = ty_prim op arg1 arg2
                let eqs = (eqs_of_subst s1) @ (eqs_of_subst s2) @ eqs 
                let s3 = unify eqs
                in  (s3, subst_type s3 ret)
            | IfExp (exp1, exp2, exp3) ->
                let (s1,c) = ty_exp tyenv exp1
                let (s2,t) = ty_exp tyenv exp2
                let (s3,e) = ty_exp tyenv exp3 
                let eqs = [(c,Type.TyBool);(t,e)]@(eqs_of_subst s1)@(eqs_of_subst s2)@(eqs_of_subst s3)
                let s3 = unify eqs
                in  (s3, subst_type s3 t)
            | LetExp (ss,b) ->
                
                let (newtyenv,newsubst) = 
                    List.fold 
                        (fun (newtyenv,newsubst) s -> 
                            List.fold 
                                (fun (newtyenv',newsubst') (id,e) -> 
                                    let (subst,ty) = ty_exp newtyenv e 
                                    let newtyenv' = (id, ty)::newtyenv' 
                                    let newsubst' = subst@newsubst' 
                                    in (newtyenv', newsubst') )
                                (newtyenv,newsubst)
                                s 
                        ) 
                        (tyenv, List.empty) 
                        ss

                let (subst,ty) = ty_exp newtyenv b
                let eqs = eqs_of_subst (newsubst @ subst)
                let s3 = unify eqs
                in (s3, (subst_type s3 ty))

            | LetRecExp (ss,b) ->
                
                let (dummyenv,newsubst,neweqs,ret) = 
                    List.fold 
                        (fun (dummyenv,newsubst,neweqs,ret) s -> 
                            let items = List.fold (fun s (id,_) -> (id, Type.fresh_tyvar())::s) List.empty s 
                            let dummyenv = (List.map (fun (id,v) -> (id, Type.TyVar v)) items) @ dummyenv
                            let (newsubst,neweqs,ret) = List.fold2
                                                            (fun (newsubst',neweqs',ret') (id,e) (_,v) -> 
                                                                let (subst,ty) = ty_exp dummyenv e 
                                                                let newsubst' = subst@newsubst' 
                                                                let neweqs' = (Type.TyVar v,ty)::neweqs' 
                                                                let newret' = (id,ty)::ret' 
                                                                in (newsubst',neweqs',newret') )
                                                            (newsubst,neweqs,ret)
                                                            s 
                                                            items 
                            in  (dummyenv, newsubst,neweqs, ret)
                        ) 
                        (tyenv, List.empty, List.empty, List.empty) 
                        ss

                let eqs = eqs_of_subst (newsubst)
                let s3 = unify eqs
                let newtyenv = tyenv

                let (ret, newtyenv) = List.fold (fun (ret,newtyenv) (id,ty) -> let ty = subst_type s3 ty in ((id, ty)::ret),(id, ty)::newtyenv) ([],newtyenv) ret

                let (newsubst,ty) = ty_exp newtyenv b
                let eqs = eqs_of_subst (newsubst)@eqs
                let s3 = unify eqs

                in  (s3, (subst_type s3 ty))

            | FunExp (id, exp) -> 
                let tyarg = Type.TyVar (Type.fresh_tyvar ())
                let (subst,tyret) = ty_exp ((id,tyarg)::tyenv) exp
                let ty = Type.TyFunc (tyarg, tyret)
                let eqs = eqs_of_subst subst
                let s3 = unify eqs
                in  (s3, (subst_type s3 ty))

            | MatchExp (expr, cases) ->
                let domv = Type.fresh_tyvar()
                let domty = Type.TyVar domv
                let (st,ty) = ty_exp tyenv expr
                let eqs = eqs_of_subst st
                let eqs = List.fold (fun s (pt,ex) -> let (eqs1,binds1) = ty_try_match ty pt tyenv in let env1 = binds1 @ tyenv in let (se, tye) = ty_exp env1 ex in (domty,tye)::eqs1@(eqs_of_subst se)@s) eqs cases
                let s3 = unify(eqs)
                in  (s3, (subst_type s3 domty))

            | AppExp (exp1, exp2) ->
                let (subst1,tyexp1) = ty_exp tyenv exp1
                let (subst2,tyexp2) = ty_exp tyenv exp2

                let tyvret = Type.fresh_tyvar ()    

                let eqs = (tyexp1, Type.TyFunc (tyexp2, Type.TyVar tyvret))::eqs_of_subst(subst1)@eqs_of_subst(subst2)
                let s3 = unify eqs
                in  (s3, (subst_type s3 (Type.TyVar tyvret)))

            | SeqExp (exp1,exp2) ->
                let (s1,c) = ty_exp tyenv exp1
                let (s2,t) = ty_exp tyenv exp2
                let eqs = [(c,Type.TyUnit)]@(eqs_of_subst s1)@(eqs_of_subst s2)
                let s3 = unify eqs
                in  (s3, subst_type s3 t)

            | LLit v -> 
                let ety = Type.TyVar (Type.fresh_tyvar())
                let ty = Type.TyList ety
                let (s,t) = List.foldBack (fun x (s,t) -> let (s',t') = ty_exp tyenv x in ((t',ety)::t,(eqs_of_subst s')@s)) v ([],[])
                let eqs = s@t
                let s3 = unify eqs
                in  (s3, subst_type s3 ty)

            | TLit v -> 
                let (s,t,m) = List.foldBack (fun x (s,t,m) -> let (s',t') = ty_exp tyenv x in let ety = Type.TyVar (Type.fresh_tyvar()) in ((t',ety)::t,(eqs_of_subst s')@s,ety::m)) v ([],[],[])
                let ty = Type.TyTuple m
                let eqs = s@t
                let s3 = unify eqs
                in  (s3, subst_type s3 ty)

        let ty_decl tyenv = function
            | ExpStmt e -> 
                let (s, t) = ty_exp tyenv e in ([("-", t)], tyenv) 
            | LetStmt ss -> 
                let (newtyenv,newsubst,ret) = 
                    List.fold 
                        (fun (newtyenv,newsubst,ret) s -> 
                            List.fold 
                                (fun (newtyenv',newsubst',ret') (id,e) -> 
                                    let (subst,ty) = ty_exp newtyenv e 
                                    let newtyenv' = (id, ty)::newtyenv' 
                                    let newsubst' = subst@newsubst' 
                                    let newret' = (id,ty)::ret' 
                                    in (newtyenv', newsubst', newret') )
                                (newtyenv,newsubst,ret)
                                s 
                        ) 
                        (tyenv, List.empty, List.empty) 
                        ss
                let eqs = eqs_of_subst (newsubst)
                let s3 = unify eqs
                let ret = List.map (fun (id,ty) -> (id, subst_type s3 ty)) ret
                in  (ret, newtyenv)
 
            | LetRecStmt ss -> 
                let (dummyenv,newsubst,neweqs,ret) = 
                    List.fold 
                        (fun (dummyenv,newsubst,neweqs,ret) s -> 
                            let items = List.fold (fun s (id,_) -> (id, Type.fresh_tyvar())::s) List.empty s 
                            let dummyenv = (List.map (fun (id,v) -> (id, Type.TyVar v)) items) @ dummyenv
                            let (newsubst,neweqs,ret) = List.fold2
                                                            (fun (newsubst',neweqs',ret') (id,e) (_,v) -> 
                                                                let (subst,ty) = ty_exp dummyenv e 
                                                                let newsubst' = subst@newsubst' 
                                                                let neweqs' = (Type.TyVar v,ty)::neweqs' 
                                                                let newret' = (id,ty)::ret' 
                                                                in (newsubst',neweqs',newret') )
                                                            (newsubst,neweqs,ret)
                                                            s 
                                                            items 
                            in  (dummyenv, newsubst,neweqs, ret)
                        ) 
                        (tyenv, List.empty, List.empty, List.empty) 
                        ss

                let eqs = eqs_of_subst (newsubst)
                let s3 = unify eqs
                let newtyenv = tyenv

                let (ret, newtyenv) = List.fold (fun (ret,newtyenv) (id,ty) -> let ty = subst_type s3 ty in ((id, ty)::ret),(id, ty)::newtyenv) ([],newtyenv) ret
                in  (List.rev ret, newtyenv)


    module Repl =
        open Syntax
        open Typing
        open Eval
        
        let rec read_eval_print env tyenv reader position =
            printf "# ";
            match Parser.toplevel reader position (position, "")  with
                | ParserCombinator.Success (p,decl,_) -> 
                    try 
                        let (i,l,c) = p
                        let _ = printfn "%A" decl
                        let (tyrets, newtyenv) = Typing.ty_decl tyenv decl
                        let (rets, newenv) = eval_decl env decl
                        let ziped = List.zip rets tyrets 
                        let _ = let rec loop xs = match xs with | [] -> [] | ((id,v),(_,t))::xs -> (printfn "val %s : %s = %s" id (Type.ToString t) (Value.ToString v)); loop xs in loop ziped
                        let (reader,p) = Reader.trunc reader p
                        in
                            read_eval_print newenv newtyenv reader p
                    with
                        | v -> 
                            let (reader,p) = Reader.trunc reader p
                            in
                                printfn "%s" v.Message;
                                read_eval_print env tyenv reader p

                | ParserCombinator.Fail(p,((i,l,c),msg)) ->
                    let (reader, p) = Parser.errorRecover reader p
                    let (reader, p) = Reader.trunc reader p
                    in  printfn "SyntaxError (%d, %d) : %s" l c msg;
                        read_eval_print env tyenv reader p

        let initial_env =
            Environment.empty |>
            (Environment.extend "x" (Value.IntV 10)) |> 
            (Environment.extend "v" (Value.IntV  5)) |>
            (Environment.extend "i" (Value.IntV 1))

        let initial_tyenv =
            Environment.empty |>
            (Environment.extend "x" Type.TyInt) |> 
            (Environment.extend "v" Type.TyInt) |>
            (Environment.extend "i" Type.TyInt)

        let run () = read_eval_print initial_env initial_tyenv (Reader.create System.Console.In) Position.head



[<EntryPoint>]
let main argv = 
//    let ret = Interpreter.Parser.LetAndExpr (Reader.create System.Console.In) Position.head (Position.head,"")
//    let _ = printfn "%A" ret in  
    Interpreter.Repl.run (); 0 // 整数の終了コードを返します

