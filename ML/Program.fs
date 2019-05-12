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

    let EoS = '\u0000'

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
        in  if i < reader.buffer.Length then reader.buffer.[i] else EoS
    
    let submatch (reader:t) (start:int) (str:string) =
        if (start < 0) || (Item reader (str.Length + start - 1) = EoS) 
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

    let char (pred : char -> bool) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if (ch <> Reader.EoS) && (pred ch) 
            then succ (Position.inc_ch pos ch) ch failInfo 
            else fail pos ("char: not match character "+(if (ch <> Reader.EoS) then ch.ToString() else "EOS")) failInfo

    let anychar (chs:string) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if (ch <> Reader.EoS) && (chs.IndexOf(ch) <> -1 )
            then succ (Position.inc_ch pos ch) ch failInfo
            else fail pos ("anychar: not match character  "+(if (ch <> Reader.EoS) then ch.ToString() else "EOS")) failInfo

    let str (s:string) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if Reader.submatch reader i s 
            then succ (Position.inc_str pos s) s failInfo
            else fail pos ("str: require is '"+s+"' but get"+(if (ch <> Reader.EoS) then ch.ToString() else "EOS")+".") failInfo

    let any () = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if ch <> Reader.EoS 
            then succ (Position.inc_ch pos ch) ch  failInfo
            else fail pos "any: require any but get EOF." failInfo

    let not (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    _ -> succ pos () failInfo
            | Success _ -> fail pos "not: require rule was fail but success." failInfo

    let select (pred:'a->'b) (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (pos, value, max2) -> succ pos (pred value) max2

    let asString (parser:Parser<'a list>) = 
        select (fun x -> List.fold (fun s x -> s + x.ToString()) "" x) parser

    let where (pred:'a->bool) (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            match parser reader pos failInfo with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (_, value, max2) as f -> if pred value then f else fail pos "where: require rule was fail but success." max2

    let opt (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->       
            match parser reader pos failInfo with
            | Fail (pos, max2)  -> succ pos None max2
            | Success (pos, value, max2) -> succ pos (Some value) max2

    let seq (parsers:Parser<'a> list) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:Position.t) (failInfo:FailInformation) (values: 'a list) =
                match parsers with
                | []   -> succ pos (List.rev values) failInfo
                | x::xs -> 
                    match x reader pos failInfo with
                    | Fail    (pos, max2) -> Fail (pos, max2)
                    | Success (pos, value, max2) -> loop xs pos max2 (value :: values)
            in loop parsers pos failInfo [];

    let choice(parsers:Parser<'a> list) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:Position.t) (failInfo:FailInformation) =
                match parsers with
                | []   -> fail pos "choice: not match any rules." failInfo
                | x::xs -> 
                    match x reader pos failInfo with
                    | Fail (_, max2) -> loop xs pos max2
                    | Success _ as ret -> ret;
            in loop parsers pos failInfo;

    let repeat (parser:Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            let rec loop pos values failInfo = 
                match parser reader pos failInfo with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in loop pos [] failInfo

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

    let andBoth (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            match lhs reader pos failInfo with
            | Fail    (pos1, max2) -> fail pos "andBoth: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andBoth: not match right rule" max3
                | Success (pos2, value2, max3) -> succ pos2 (value1, value2) max3 

    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation) -> 
            match lhs reader pos failInfo with
            | Fail    (pos1, max2) -> fail pos "andRight: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andRight: not match right rule" max3
                | Success (pos2, value2, max3) ->  succ pos2 value2 max3

    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)-> 
            match lhs reader pos failInfo with
            | Fail    (pos1, max2) -> fail pos "andLeft: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andLeft: not match left rule" max3
                | Success (pos2, value2, max3) -> succ pos2 value1 max3 

    let quote (parser:unit -> Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  (parser ()) reader pos failInfo

    let hole () = 
        ref (fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  failwith "hole syntax")

    type Memoizer = { add: (unit -> unit) -> unit; reset : unit -> unit }
    let memoizer () = 
        let handlers = ref List.empty
        in
            {
                add = fun (handler:unit -> unit) -> handlers := handler :: !handlers;
                reset = fun () -> List.iter (fun h -> h()) !handlers
            }
        
    let memoize (memoizer: Memoizer) (f : Parser<'a>) = 
        let dic = System.Collections.Generic.Dictionary<(Reader.t * int * FailInformation), ParserState<'a>> ()
        let _ = memoizer.add (fun () -> dic.Clear() )
        in  fun x y z -> 
                let (i,l,c) = y 
                let key = (x,i,z) 
                in
                    match dic.TryGetValue(key) with 
                    | true, r -> r
                    | _       -> dic.[key] <- f x y z;
                                 dic.[key]

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
            static member inline Many1(self: Parser<'T>) = repeat1 self 
            [<Extension>]
            static member inline Option(self: Parser<'T>) = opt self 
            [<Extension>]
            static member inline Select(self: Parser<'T1>, selector:'T1 -> 'T2) = select selector self 
            [<Extension>]
            static member inline Where(self: Parser<'T1>, selector:'T1 -> bool) = where selector self 
            [<Extension>]
            static member inline AsString(self: Parser<'T list>) = asString self 

module Interpreter =
    module Type = 
        type tyvar = int

        type t = 
              TyInt  
            | TyBool 
            | TyFunc of t * t
            | TyList of t
            | TyVar of tyvar
            | TyUnit
            | TyTuple of t list

        let rec ToString ty =
            match ty with
            | TyInt -> "int"
            | TyBool -> "bool"
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
                | t.TyFunc (tyarg,tyret) -> loop tyarg ret |> loop tyret
                | t.TyList ty -> loop ty ret
                | t.TyVar id -> Set.add id ret     
                | t.TyUnit -> ret
                | t.TyTuple tys -> List.fold (fun s x -> loop x s) ret tys
            in loop ty Set.empty

    module Syntax =
        type id = string

        type binOp = Plus | Minus | Mult | Divi | Lt | Gt | Le | Ge | Eq | Ne | Cons

        type pattern = 
              VarP of id
            | AnyP
            | ILitP of int
            | BLitP of bool
            | LLitP of pattern list
            | TLitP of pattern list
            | NilP
            | ConsP of pattern * pattern
            | UnitP
 
        type exp =
              Var of id
            | ILit of int
            | BLit of bool
            | Unit
            | LLit of exp list
            | TLit of exp list
            | BinOp of binOp * exp * exp
            | IfExp of exp * exp * exp
            | SeqExp of (exp * exp)
            | LetExp of (id * exp) list list * exp
            | LetRecExp of (id * exp) list list * exp
            | FunExp of (id * exp) 
            | MatchExp of (exp * (pattern * exp) list)
            | AppExp  of (exp * exp)

        type program =
            | ExpStmt of exp
            | LetStmt of (id * exp) list list
            | LetRecStmt of (id * exp) list list

    module Parser =
        open ParserCombinator
        open ParserCombinator.OperatorExtension
        open Syntax;

        let isLower ch = 'a' <= ch && ch <= 'z'
        let isUpper ch = 'A' <= ch && ch <= 'Z'
        let isDigit ch = '0' <= ch && ch <= '9'
        let isIdHead ch = isLower(ch)
        let isIdBody ch = isLower(ch) || isDigit(ch) || (ch = '_') || (ch = '\'') 

        let memo = memoizer()

        let WS = (anychar " \t\r\n").Many()
        let ws x = WS.AndR(x)  |> memoize memo

        let Ident = ws( (char isIdHead).And((char isIdBody).Many().AsString()).Select(fun (h,b) -> h.ToString() + b) )
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
        let ID = Ident.Where(fun x -> (List.contains x ["true";"false";"if";"then";"else";"let";"in";"and";"fun";"rec";"match";"with"]) = false);
        let INTV = ws( (char (fun x -> x = '-')).Option().And((char isDigit).Many1().AsString().Select(System.Int32.Parse)).Select(fun (s,v) -> if s.IsSome then (-v) else v))
        let LPAREN = ws(char (fun x -> x = '(' ))
        let RPAREN = ws(char (fun x -> x = ')' ))
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
        let LBRACKET = ws(char (fun x -> x = '[' ))
        let RBRACKET = ws(char (fun x -> x = ']' ))
        let COLCOL = ws(str "::")
        let BAR = ws(str "|")
        let SEMI = ws(str ";")
        let US = ws(str "_")
        let COMMA = ws(str ",")

        let Expr_ = hole ()
        let Expr = quote(fun () -> !Expr_)|> memoize memo

        let BinOpExpr = 
            let make (tok,op) = tok.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (op,Var "@lhs",Var "@rhs"))))
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

        let FunExpr =
            FUN.AndR(ID.Many1()).AndL(RARROW).And(Expr).Select(fun (args, e) -> List.foldBack (fun x s -> FunExp (x, s)) args e)

        let PatternExpr_ = hole ()
        let PatternExpr = quote(fun () -> !PatternExpr_)|> memoize memo

        let PatternAExpr = 
            choice [ 
                INTV.Select(fun x -> ILitP x);
                TRUE.Select(fun x -> BLitP true);
                FALSE.Select(fun x -> BLitP false);
                ID.Select(fun x -> VarP x);
                US.Select(fun x -> AnyP );
                LBRACKET.AndR(PatternExpr.And((SEMI.AndR(PatternExpr)).Many()).Option().Select( function | Some (x,xs) -> (x::xs) | None -> [] )).AndL(RBRACKET).Select(LLitP);
                LPAREN.AndR(PatternExpr).AndL(RPAREN);
            ]

        let PatternConsExpr = 
            PatternAExpr.And(COLCOL.AndR(PatternAExpr).Many()).Select(fun (head, tail) -> List.reduceBack (fun x s -> ConsP(x, s)) (head::tail) );

        let PatternTupleExpr = 
            PatternConsExpr.And(COMMA.AndR(PatternConsExpr).Many()).Select(fun (head, tail) -> match tail with [] -> head | _ -> TLitP (head::tail) );

        let _ = PatternExpr_ := PatternTupleExpr

        let Matching =
            let checkPattern pattern =
                let vars = 
                    let rec scan pattern ret  =
                        match pattern with
                        | VarP name -> name :: ret 
                        | AnyP -> ret
                        | ILitP _ -> ret
                        | BLitP _ -> ret 
                        | LLitP items -> List.fold (fun s x -> scan x s) ret items
                        | TLitP items -> List.fold (fun s x -> scan x s) ret items
                        | NilP -> ret
                        | UnitP -> ret
                        | ConsP (car, cdr) -> scan car ret |> scan cdr
                    in
                        scan pattern []
                let dup = List.countBy (fun x -> x) vars |> List.where (fun (_,cnt) -> cnt > 1) |> List.map (fun (name,cnt) -> name)
                in
                    if List.isEmpty dup 
                    then true
                    else failwith (List.map (fun x -> sprintf "Variable %s is bound several times in this matching." x) dup |> String.concat "\n")
            in
                PatternExpr.Where(checkPattern).AndL(RARROW).And(Expr)

        let Matchings =
            BAR.Option().AndR(Matching).And(BAR.AndR(Matching).Many()).Select(fun (x,xs) -> x::xs)

        let MatchExpr =
            MATCH.AndR(Expr).AndL(WITH).And(Matchings).Select(fun (e,b) -> MatchExp (e,b))

        let IfExpr =
            IF.AndR(Expr).AndL(THEN).And(Expr).AndL(ELSE).And(Expr).Select( fun ((c,t),e) -> IfExp (c, t, e) )

        let LetPrim =
            (ID.Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | [] -> failwith "no entry" | id::[] -> (id,e) | id::args -> (id,List.foldBack (fun x s -> FunExp (x, s)) args e))) |> memoize memo
            
        let LetAndExpr =
            LET.AndR(LetPrim).And(AND.AndR(LetPrim).Many()).Select(fun (x,xs) -> (x::xs))

        let LetAndExprs = (LetAndExpr).Many1() |> memoize memo

        let LetRecAndExpr =
            LET.AndR(REC).AndR(LetPrim).And(AND.AndR(LetPrim).Many()).Select(fun (x,xs) -> (x::xs))

        let LetRecAndExprs = (LetRecAndExpr).Many1() |> memoize memo

        let LetExpr = 
            choice [
                LetRecAndExprs.AndL(IN).And(Expr).Select(LetRecExp);
                LetAndExprs.AndL(IN).And(Expr).Select(LetExp)
            ]

        let AExpr = 
            choice [ 
                FunExpr; 
                INTV.Select(fun x -> ILit x);
                TRUE.Select(fun _ -> BLit true);
                FALSE.Select(fun _ -> BLit false);
                ID.Select(fun x -> Var x);
                IfExpr; 
                MatchExpr;
                LetExpr; 
                LBRACKET.AndR(Expr.And((SEMI.AndR(Expr)).Many()).Option().Select( function | Some (x,xs) -> (x::xs) | None -> [] )).AndL(RBRACKET).Select(LLit);
                LPAREN.And(RPAREN).Select(fun _ -> Unit);
                LPAREN.AndR(BinOpExpr).AndL(RPAREN);
                LPAREN.AndR(Expr).AndL(RPAREN)
            ]

        let AppExpr = AExpr.Many1().Select( fun x -> List.reduce (fun s x -> AppExp (s, x) ) x )

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
            PExpr.And(COLCOL.AndR(PExpr).Many()).Select(fun (head, tail) -> List.reduceBack (fun x s -> BinOp(Cons, x, s)) (head::tail) )

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
                LOrExpr.And(COMMA.AndR(LOrExpr).Many()).Select(fun (x,xs) -> match xs with | [] -> x | xs -> TLit (x::xs));

        let SeqExpr =
                TupleExpr.And(SEMI.AndR(TupleExpr).Many()).Select(fun (x,xs) -> List.reduceBack (fun x s -> SeqExp(x,s)) (x::xs));

        let _ = Expr_ := SeqExpr

        let LetStmt =
            LetAndExprs.AndL(IN.Not()).Select( fun s -> LetStmt s)

        let LetRecStmt =
            LetRecAndExprs.AndL(IN.Not()).Select( fun s -> LetRecStmt s)

        let ExprStmt =
            Expr.Select( fun x -> ExpStmt x)

        let toplevel = (action (fun _ -> memo.reset() )).AndR(choice[ LetRecStmt; LetStmt; ExprStmt ].AndL(SEMISEMI))
        
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
                            any().Select(fun x -> x.ToString())
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
                | ProcV of id * exp * t Environment.t ref
                | ConsV of t * t
                | NilV
                | UnitV
                | TupleV of t list
            
            let rec ToString v =
                match v with
                | IntV v -> v.ToString()
                | BoolV v -> if v then "true" else "false"
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
            | LetExp (ss,b) ->
                let newenv = List.fold (fun env s -> List.fold (fun env' (id,e) -> let v = eval_exp env e in  (id, v)::env') env s ) env ss
                //let newenv = List.fold (fun env (id,e) -> let v = eval_exp env e in  (id, v)::env ) env ss
                in  eval_exp newenv b

            | LetRecExp (ss,b) ->
                let dummyenv = ref Environment.empty
                let newenv = List.fold (fun env s -> List.fold (fun env' (id,e) -> let v = match e with FunExp(id,exp) -> Value.ProcV (id,exp,dummyenv) | _ -> failwithf "variable cannot " in  (id, v)::env') env s ) env ss
                in  dummyenv := newenv;
                    eval_exp newenv b

            | FunExp (id, exp) -> 
                Value.ProcV (id, exp, ref env) 
            
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
                    | Value.ProcV (id, body, env') ->
                        let newenv = Environment.extend id arg !env' in
                        eval_exp newenv body
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
                List.fold (fun s x -> List.fold (fun (ret,env') (id,e) -> let v = eval_exp env e in  (id, v)::ret,(id, v)::env') s x ) ([],env) ss
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
                    then failwith "unification error"
                    else 
                        let ret = (id,ty) :: ret
                        let eqs = subst_eqs ret eqs
                        in  loop eqs ret
                | (Type.TyFunc (tyarg1, tyret1), Type.TyFunc (tyarg2, tyret2)) :: eqs  -> loop ((tyarg1, tyarg2)::(tyret1, tyret2)::eqs) ret
                | (Type.TyList ty1, Type.TyList ty2) :: eqs  -> loop ((ty1, ty2)::eqs) ret
                | (Type.TyTuple ty1, Type.TyTuple ty2) :: eqs  when List.length ty1 = List.length ty2 -> loop ((List.zip ty1 ty2) @ eqs) ret
                | _ -> failwith "unification error"
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
                        let _ = List.iter (fun ((id,v),(id,t)) -> printfn "val %s : %s = %s" id (Type.ToString t) (Value.ToString v)) ziped
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
    let alpha = Interpreter.Type.fresh_tyvar () 
    let beta = Interpreter.Type.fresh_tyvar () 
    let ans1 = Interpreter.Typing.subst_type [(alpha, Interpreter.Type.TyInt)] (Interpreter.Type.TyFunc (Interpreter.Type.TyVar alpha, Interpreter.Type.TyBool))
    let _ = printfn "%A" ans1
    let ans2 = Interpreter.Typing.subst_type [(beta, (Interpreter.Type.TyFunc (Interpreter.Type.TyVar alpha, Interpreter.Type.TyInt))); (alpha, Interpreter.Type.TyBool)] (Interpreter.Type.TyVar beta)
    let _ = printfn "%A" ans2
    let subst1 = Interpreter.Typing.unify [(Interpreter.Type.TyVar alpha, Interpreter.Type.TyInt)]
    let _ = printfn "%A" subst1
    let subst2 = Interpreter.Typing.unify [(Interpreter.Type.TyFunc(Interpreter.Type.TyVar alpha, Interpreter.Type.TyBool), Interpreter.Type.TyFunc(Interpreter.Type.TyFunc(Interpreter.Type.TyInt, Interpreter.Type.TyVar beta), Interpreter.Type.TyVar beta))]
    let _ = printfn "%A" subst2
    in  Interpreter.Repl.run (); 0 // 整数の終了コードを返します

