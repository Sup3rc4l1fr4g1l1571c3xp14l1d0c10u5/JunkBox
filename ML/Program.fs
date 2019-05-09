module Position =
    type t = (int * int * int) 

    let start = (0,1,1)

    let incc ((i,l,c):t) (ch:char) =
        if ch = '\n' then (i + 1, l + 1, 1) 
                     else (i + 1, l, c + 1) 

    let inc ((i,l,c):t) (str:string) =
        let chars = Array.ofSeq str
        let len = Array.length chars
        let line = Array.fold (fun s x -> if x = '\n' then s + 1 else s) 0 chars
        let col = len - (match Array.tryFindIndexBack (fun x -> x = '\n') chars with | None -> 0 | Some v -> v)
        in  if len > 0 then (i + len, l + line, col + 1) 
                       else (i + len, l, c + col) 

module Reader = 
    type t = { reader: System.IO.TextReader; buffer: System.Text.StringBuilder; }
    let create reader = { reader = reader; buffer = System.Text.StringBuilder(); }
    let Item (x:t) i = 
        let rec loop () =
            if i < x.buffer.Length 
            then ()
            else 
                let ch = x.reader.Read()
                in  if ch = -1 
                    then () 
                    else x.buffer.Append(char ch) |> ignore; loop ()
        let _ = loop ()
        in  if i < x.buffer.Length then x.buffer.[i] else '\u0000'
    
    let submatch (x:t) (i1:int) (s2:string) =
        if (i1 < 0) || (Item x (s2.Length + i1 - 1) = '\u0000') 
        then false
        else
            let rec loop (i1:int) (i2:int) = 
                if (i2 = 0) 
                then true 
                else
                    let i1, i2 = (i1-1, i2-1)
                    in  if x.buffer.[i1] = s2.[i2]
                        then loop i1 i2
                        else false
            in  loop (i1 + s2.Length) (s2.Length)

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
            if (ch <> '\u0000') && (pred ch) 
            then succ (Position.incc pos ch) ch failInfo 
            else fail pos ("char: not match character "+(if (ch <> '\u0000') then ch.ToString() else "EOS")) failInfo

    let anychar (chs:string) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if (ch <> '\u0000') && (chs.IndexOf(ch) <> -1 )
            then succ (Position.incc pos ch) ch failInfo
            else fail pos ("anychar: not match character  "+(if (ch <> '\u0000') then ch.ToString() else "EOS")) failInfo

    let str (s:string) =
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if Reader.submatch reader i s 
            then succ (Position.inc pos s) s failInfo
            else fail pos ("str: require is '"+s+"' but get"+(if (ch <> '\u0000') then ch.ToString() else "EOS")+".") failInfo

    let any () = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  -> 
            let (i,l,c) = pos
            let ch = Reader.Item reader i in
            if ch <> '\u0000' 
            then succ (Position.incc pos ch) ch  failInfo
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

    let quote (p:unit -> Parser<'a>) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  (p ()) reader pos failInfo

    let hole () = 
        ref (fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  failwith "hole syntax")

    let failure (msg:string) = 
        fun (reader:Reader.t) (pos:Position.t) (failInfo:FailInformation)  ->  fail pos msg failInfo

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
                let (i,l,c) = y in
                match dic.TryGetValue((x,i,z)) with 
                | true, r -> r
                | _       -> dic.[(x,i,z)] <- f x y z;
                             dic.[(x,i,z)]

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
    module Ty = 
        type tyvar = int

        type t = 
              TyInt  
            | TyBool 
            | TyFunc of t * t
            | TyList of t
            | TyVar of tyvar

        let rec ToString ty =
            match ty with
            | TyInt -> "int"
            | TyBool -> "bool"
            | TyFunc (arg, ret)-> sprintf "%s -> %s" (ToString arg) (ToString ret)
            | TyList ty -> sprintf "%s list" (ToString ty)
            | TyVar n -> sprintf "'%c" (char n + 'a')

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
            | NilP
            | ConsP of pattern * pattern

        type exp =
              Var of id
            | ILit of int
            | BLit of bool
            | LLit of exp list
            | BinOp of binOp * exp * exp
            | IfExp of exp * exp * exp
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

        let memor = memoizer()

        let WS = (anychar " \t\r\n").Many()
        let ws x = WS.AndR(x)  |> memoize memor


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

        let Expr_ = hole ()
        let Expr = quote(fun () -> !Expr_)|> memoize memor

        let BinOpExpr = 
            choice[
                PLUS.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Plus,Var "@lhs",Var "@rhs"))));
                MINUS.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Minus,Var "@lhs",Var "@rhs"))));
                MULT.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Mult,Var "@lhs",Var "@rhs"))));
                DIV.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Divi,Var "@lhs",Var "@rhs"))));
                EQ.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Eq,Var "@lhs",Var "@rhs"))));
                NE.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Ne,Var "@lhs",Var "@rhs"))));
                LT.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Lt,Var "@lhs",Var "@rhs"))));
                GT.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Gt,Var "@lhs",Var "@rhs"))));
                LE.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Le,Var "@lhs",Var "@rhs"))));
                GE.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Ge,Var "@lhs",Var "@rhs"))))
                COLCOL.Select(fun _ -> FunExp("@lhs", FunExp("@rhs", BinOp (Cons,Var "@lhs",Var "@rhs"))))
            ]

        let FunExpr =
            FUN.AndR(ID.Many1()).AndL(RARROW).And(Expr).Select(fun (args, e) -> List.foldBack (fun x s -> FunExp (x, s)) args e)

        let PatternExpr_ = hole ()
        let PatternExpr = quote(fun () -> !PatternExpr_)|> memoize memor

        let PatternAExpr = 
            choice [ 
                INTV.Select(fun x -> ILitP x);
                TRUE.Select(fun x -> BLitP true);
                FALSE.Select(fun x -> BLitP false);
                ID.Select(fun x -> VarP x);
                US.Select(fun x -> AnyP );
                LBRACKET.AndR(PatternExpr.And((SEMI.AndR(PatternExpr)).Many()).Option().Select( function | Some (x,xs) -> (x::xs) | None -> [] )).AndL(RBRACKET).Select(LLitP);
            ]

        let PatternConsExpr = 
            PatternAExpr.And(COLCOL.AndR(PatternAExpr).Many()).Select(fun (head, tail) -> List.reduceBack (fun x s -> ConsP(x, s)) (head::tail) );

        let _ = PatternExpr_ := PatternConsExpr

        let checkPattern pattern =
            let rec enum_varp pattern ret =
                match pattern with
                | VarP id -> pattern :: ret 
                | AnyP -> ret
                | ILitP v -> ret
                | BLitP v -> ret 
                | LLitP pats -> 
                    let rec loop pats varset =
                        match pats with
                        | [] -> ret
                        | p::ps -> enum_varp p ret |> loop ps 
                    in loop pats ret
                | NilP -> ret
                | ConsP (x,y) ->
                    enum_varp x ret |> enum_varp y
            let varps = enum_varp pattern []
            let rec check varps set ret =
                match varps with
                | [] -> ret
                | x::xs -> if Set.contains x set then check xs set (x::ret) else check xs (Set.add x set) ret
            let dup = check varps Set.empty []
            let var_name x = match x with | VarP x -> x | _ -> ""
            in
                if List.isEmpty dup 
                then true
                else failwith (List.map (fun x -> sprintf " Variable %s is bound several times in this matching." (var_name x)) dup |> String.concat "\n")

        let MatchEntry =
            PatternExpr.Where(checkPattern).AndL(RARROW).And(Expr)

        let MatchEntries =
            BAR.Option().AndR(MatchEntry).And(BAR.AndR(MatchEntry).Many()).Select(fun (x,xs) -> x::xs)

        let MatchExpr =
            MATCH.AndR(Expr).AndL(WITH).And(MatchEntries).Select(fun (e,b) -> MatchExp (e,b))

        let IfExpr =
            IF.AndR(Expr).AndL(THEN).And(Expr).AndL(ELSE).And(Expr).Select( fun ((c,t),e) -> IfExp (c, t, e) )

        let LetPrim =
            (ID.Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | [] -> failwith "no entry" | id::[] -> (id,e) | id::args -> (id,List.foldBack (fun x s -> FunExp (x, s)) args e))) |> memoize memor
            
        let LetAndExpr =
            LET.AndR(LetPrim).And(AND.AndR(LetPrim).Many()).Select(fun (x,xs) -> (x::xs))

        let LetAndExprs = (LetAndExpr).Many1() |> memoize memor

        let LetRecAndExpr =
            LET.AndR(REC).AndR(LetPrim).And(AND.AndR(LetPrim).Many()).Select(fun (x,xs) -> (x::xs))

        let LetRecAndExprs = (LetRecAndExpr).Many1() |> memoize memor

        let LetExpr = 
            choice [
                LetRecAndExprs.AndL(IN).And(Expr).Select(LetRecExp);
                LetAndExprs.AndL(IN).And(Expr).Select(LetExp)
            ]

        let AExpr = 
            choice [ 
                FunExpr; 
                MatchExpr;
                IfExpr; 
                LetExpr; 
                INTV.Select(fun x -> ILit x);
                TRUE.Select(fun x -> BLit true);
                FALSE.Select(fun x -> BLit false);
                ID.Select(fun x -> Var x);
                LBRACKET.AndR(Expr.And((SEMI.AndR(Expr)).Many()).Option().Select( function | Some (x,xs) -> (x::xs) | None -> [] )).AndL(RBRACKET).Select(LLit);
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

        let _ = Expr_ := LOrExpr

        let LetStmt =
            LetAndExprs.AndL(IN.Not()).Select( fun s -> LetStmt s)

        let LetRecStmt =
            LetRecAndExprs.AndL(IN.Not()).Select( fun s -> LetRecStmt s)

        let ExprStmt =
            Expr.Select( fun x -> ExpStmt x)

        let toplevel = (action (fun _ -> memor.reset() )).AndR(choice[ LetRecStmt; LetStmt; ExprStmt ].AndL(SEMISEMI))
        
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
        type exval =
              IntV of int
            | BoolV of bool
            | ProcV of id * exp * dnval Environment.t ref
            | ConsV of exval * exval
            | NilV
        and dnval = exval

        let rec pp_val v =
            match v with
            | IntV v -> v.ToString()
            | BoolV v -> if v then "true" else "false"
            | ProcV (id,exp,env) -> "<fun>"
            | NilV -> "[]"
            | ConsV _ -> 
                let rec loop v ret = 
                    match v with
                    | NilV -> List.rev ret
                    | ConsV (x,xs) -> loop xs ((pp_val x)::ret)
                    | _ -> failwith "not cons or nil"
                let items = loop v []
                in  sprintf "[%s]" (String.concat "; " items)

        let rec apply_prim op arg1 arg2 = 
            match op, arg1, arg2 with
            | Plus, IntV i1, IntV i2 -> IntV (i1 + i2)
            | Plus, _, _ -> failwith ("Both arguments must be integer: +")
            | Minus, IntV i1, IntV i2 -> IntV (i1 - i2)
            | Minus, _, _ -> failwith ("Both arguments must be integer: -")
            | Mult, IntV i1, IntV i2 -> IntV (i1 * i2)
            | Mult, _, _ -> failwith ("Both arguments must be integer: *")
            | Divi, IntV i1, IntV i2 -> IntV (i1 / i2)
            | Divi, _, _ -> failwith ("Both arguments must be integer: /")
            | Lt, IntV i1, IntV i2 -> BoolV (i1 < i2)
            | Lt, _, _ -> failwith ("Both arguments must be integer: <")
            | Le, IntV i1, IntV i2 -> BoolV (i1 <= i2)
            | Le, _, _ -> failwith ("Both arguments must be integer: <=")
            | Gt, IntV i1, IntV i2 -> BoolV (i1 > i2)
            | Gt, _, _ -> failwith ("Both arguments must be integer: >")
            | Ge, IntV i1, IntV i2 -> BoolV (i1 >= i2)
            | Ge, _, _ -> failwith ("Both arguments must be integer: >=")
            | Eq, IntV i1, IntV i2 -> BoolV (i1 = i2)
            | Eq, _, _ -> failwith ("Both arguments must be integer: =")
            | Ne, IntV i1, IntV i2 -> BoolV (i1 <> i2)
            | Ne, _, _ -> failwith ("Both arguments must be integer: <>")
            | Cons, v1, ConsV _ -> ConsV(v1, arg2)
            | Cons, v1, NilV -> ConsV(v1, arg2)
            | Cons, _, _ -> failwith ("right arguments must be list: ::")

        let rec try_match value pat env =
            match pat with 
            | VarP id -> Some ((id,value) :: env)
            | AnyP -> Some env
            | ILitP v -> if value = IntV v then Some env else None
            | BLitP v -> if value = BoolV v then Some env else None
            | LLitP pats -> 
                let rec loop p v env =
                    match p, v with 
                    | [], NilV -> Some env
                    | [], _ -> None
                    | (p::ps), NilV -> None
                    | (p::ps), ConsV(v,vs) -> 
                        match try_match v p env with
                        | Some e -> loop ps vs e
                        | None -> None
                    | _ -> None
                in  loop pats value env
            | NilP ->
                if value = NilV  then Some env else None
            | ConsP (x,y) ->
                match value with
                | ConsV(a,b) -> 
                    match try_match a x env with
                    | Some e -> try_match b y e
                    | None -> None
                | _ -> None

        let rec eval_exp env = function
            | Var x -> 
                try Environment.lookup x env with 
                    | Environment.Not_bound -> failwithf "Variable not bound: %A" x
            | ILit i -> IntV i
            | BLit b -> BoolV b
            | BinOp (op, exp1, exp2) ->
                let arg1 = eval_exp env exp1 in
                let arg2 = eval_exp env exp2 in
                    apply_prim op arg1 arg2
            | IfExp (exp1, exp2, exp3) ->
                let test = eval_exp env exp1 in
                    match test with
                        | BoolV true -> eval_exp env exp2
                        | BoolV false -> eval_exp env exp3
                        | _ -> failwith ("Test expression must be boolean: if")
            | LetExp (ss,b) ->
                let newenv = List.fold (fun env s -> List.fold (fun env' (id,e) -> let v = eval_exp env e in  (id, v)::env') env s ) env ss
                //let newenv = List.fold (fun env (id,e) -> let v = eval_exp env e in  (id, v)::env ) env ss
                in  eval_exp newenv b

            | LetRecExp (ss,b) ->
                let dummyenv = ref Environment.empty
                let newenv = List.fold (fun env s -> List.fold (fun env' (id,e) -> let v = match e with FunExp(id,exp) -> ProcV (id,exp,dummyenv) | _ -> failwithf "variable cannot " in  (id, v)::env') env s ) env ss
                in  dummyenv := newenv;
                    eval_exp newenv b

            | FunExp (id, exp) -> ProcV (id, exp, ref env)      
            | MatchExp (expr, cases) ->
                let value = eval_exp env expr
                let rec loop cases =
                    match cases with
                    | [] -> failwith ("not match");
                    | (pat,body)::xs -> 
                        match try_match value pat env with
                        | Some(env) -> eval_exp env body
                        | None -> loop xs
                in loop cases 
            | AppExp (exp1, exp2) ->
                let funval = eval_exp env exp1 in
                let arg = eval_exp env exp2 in
                    match funval with
                    | ProcV (id, body, env') ->
                        let newenv = Environment.extend id arg !env' in
                        eval_exp newenv body
                    | _ -> failwith ("Non-function value is applied")
            | LLit v -> List.foldBack (fun x s -> ConsV(eval_exp env x,s)) v NilV

        let eval_decl env = function
            | ExpStmt e -> 
                let v = eval_exp env e in (["-",v ], env) 
            | LetStmt ss -> 
                //List.fold (fun (ret,env) (id,e) -> let v = eval_exp env e in  ((id, v)::ret,(id, v)::env)) ([],env) s
                List.fold (fun s x -> List.fold (fun (ret,env') (id,e) -> let v = eval_exp env e in  (id, v)::ret,(id, v)::env') s x ) ([],env) ss
            | LetRecStmt ss -> 
                let dummyenv = ref Environment.empty
                let ret = List.fold (fun env s -> List.fold (fun (ret,env') (id,e) -> let v = match e with FunExp(id,exp) -> ProcV (id,exp,dummyenv) | _ -> failwithf "variable cannot " in  (id, v)::ret,(id, v)::env') env s ) ([],env) ss
                in  dummyenv := snd ret;
                    ret

    module Typing =
        open Syntax
        type tyenv = Ty.t Environment.t

        type subst = (Ty.tyvar * Ty.t) list

        let rec subst_type ss ty = 
            let rec subst_type' s ty = 
                let (v, t) = s
                in  match ty with
                    | Ty.TyInt -> Ty.TyInt
                    | Ty.TyBool -> Ty.TyBool
                    | Ty.TyFunc (arg, ret)-> Ty.TyFunc (subst_type' s arg, subst_type' s ret)
                    | Ty.TyList ty -> Ty.TyList (subst_type' s ty)
                    | Ty.TyVar n -> if v = n then t else ty
            in
                List.fold (fun s x -> subst_type' x s) ty ss

        let eqs_of_subst (s:subst) : (Ty.t*Ty.t) list = 
            List.map (fun (v,t) -> (Ty.TyVar v, t)) s

        let subst_eqs (s:subst) (eqs: (Ty.t*Ty.t) list) : (Ty.t*Ty.t) list = 
            List.map (fun (t1,t2) -> (subst_type s t1 , subst_type s t2)) eqs

        let unify (eqs:(Ty.t*Ty.t) list) : subst =
            let rec loop eqs ret =
                match eqs with
                | [] -> ret
                | (ty1,ty2) :: eqs when ty1 = ty2 -> loop eqs ret
                | (Ty.TyVar id, ty) :: eqs 
                | (ty, Ty.TyVar id) :: eqs ->
                    if Set.contains id (Ty.freevar_ty ty) 
                    then failwith "unification error"
                    else 
                        let ret = (id,ty) :: ret
                        let eqs = List.map (fun (ty1,ty2) -> (subst_type ret ty1, subst_type ret ty2)) eqs
                        in  loop eqs ret
                | (Ty.TyFunc (tyarg1, tyret1), Ty.TyFunc (tyarg2, tyret2)) :: eqs  -> loop ((tyarg1, tyarg2)::(tyret1, tyret2)::eqs) ret
                | (Ty.TyList ty1, Ty.TyList ty2) :: eqs  -> loop ((ty1, ty2)::eqs) ret
                | _ -> failwith "unification error"
            in  loop eqs List.empty
            
        let ty_prim op t1 t2 =
            match (op, t1, t2) with
            | Plus, Ty.TyInt, Ty.TyInt-> ([],Ty.TyInt)
            | Plus, Ty.TyInt, ty 
            | Plus, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyInt)
            | Plus, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyInt)

            | Minus, Ty.TyInt, Ty.TyInt-> ([],Ty.TyInt)
            | Minus, Ty.TyInt, ty 
            | Minus, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyInt)
            | Minus, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyInt)

            | Mult, Ty.TyInt, Ty.TyInt-> ([],Ty.TyInt)
            | Mult, Ty.TyInt, ty 
            | Mult, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyInt)
            | Mult, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyInt)

            | Divi, Ty.TyInt, Ty.TyInt-> ([],Ty.TyInt)
            | Divi, Ty.TyInt, ty 
            | Divi, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyInt)
            | Divi, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyInt)

            | Lt, Ty.TyInt, Ty.TyInt-> ([],Ty.TyBool)
            | Lt, Ty.TyInt, ty 
            | Lt, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyBool)
            | Lt, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyBool)

            | Le, Ty.TyInt, Ty.TyInt-> ([],Ty.TyBool)
            | Le, Ty.TyInt, ty 
            | Le, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyBool)
            | Le, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyBool)

            | Gt, Ty.TyInt, Ty.TyInt-> ([],Ty.TyBool)
            | Gt, Ty.TyInt, ty 
            | Gt, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyBool)
            | Gt, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyBool)

            | Ge, Ty.TyInt, Ty.TyInt-> ([],Ty.TyBool)
            | Ge, Ty.TyInt, ty 
            | Ge, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyBool)
            | Ge, _, _ -> ([(t1,Ty.TyInt);(t2,Ty.TyInt)],Ty.TyBool)

            | Eq, Ty.TyInt, Ty.TyInt -> ([],Ty.TyBool)
            | Eq, Ty.TyInt, ty 
            | Eq, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyBool)
            | Eq, Ty.TyBool, Ty.TyBool -> ([],Ty.TyBool)
            | Eq, Ty.TyBool, ty 
            | Eq, ty, Ty.TyBool -> ([(ty,Ty.TyBool)],Ty.TyBool)
            | Eq, _, _ -> let t = Ty.TyVar (Ty.fresh_tyvar()) in ([(t1,t);(t2,t)],Ty.TyBool)

            | Ne, Ty.TyInt, Ty.TyInt -> ([],Ty.TyBool)
            | Ne, Ty.TyInt, ty 
            | Ne, ty, Ty.TyInt -> ([(ty,Ty.TyInt)],Ty.TyBool)
            | Ne, Ty.TyBool, Ty.TyBool -> ([],Ty.TyBool)
            | Ne, Ty.TyBool, ty 
            | Ne, ty, Ty.TyBool -> ([(ty,Ty.TyBool)],Ty.TyBool)
            | Ne, _, _ -> let t = Ty.TyVar (Ty.fresh_tyvar()) in ([(t1,t);(t2,t)],Ty.TyBool)

            | Cons, t1, Ty.TyList t2 -> let t = Ty.TyVar (Ty.fresh_tyvar()) in ([(t1,t);(t2,t)],Ty.TyList t)
            | Cons, _, _ -> let t = Ty.TyVar (Ty.fresh_tyvar()) in ([(t1,t);(t2,Ty.TyList t)],Ty.TyList t)

        let rec ty_try_match ty pat tyenv = 
            match pat with 
            | VarP id -> (List.empty, [(id, ty)])
            | AnyP -> (List.empty, List.empty)
            | ILitP v -> ([(ty,Ty.TyInt)], List.empty)
            | BLitP v -> ([(ty,Ty.TyBool)], List.empty)
            | LLitP pats -> 
                let tyitem = Ty.TyVar (Ty.fresh_tyvar())
                let tylist = Ty.TyList tyitem
                let rec loop p substs tyenvs =
                    match p with 
                    | [] -> ((ty,tylist)::(List.fold (fun s x -> x @ s) List.empty substs), List.fold (fun s x -> x @ s) List.empty tyenvs)
                    | (p::ps) -> 
                        let (subst, e) = ty_try_match tyitem p tyenv 
                        in  loop ps (subst::substs) (e::tyenvs)
                in  loop pats List.empty List.empty
            | NilP -> ([(ty,Ty.TyVar (Ty.fresh_tyvar()))], List.empty)
            | ConsP (x,y) ->
                let tyitem = Ty.TyVar (Ty.fresh_tyvar())
                let tylist = Ty.TyList tyitem
                let (t1, e1) = ty_try_match tyitem x tyenv 
                let (t2, e2) = ty_try_match tylist y tyenv 
                in  ((ty,tylist)::(t1@t2), e1@e2)


        let rec ty_exp tyenv = function
            | Var x -> 
                try ([], Environment.lookup x tyenv) with 
                    | Environment.Not_bound -> failwithf "Variable not bound: %A" x
            | ILit i -> ([], Ty.TyInt)
            | BLit b -> ([], Ty.TyBool)
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
                let eqs = [(c,Ty.TyBool);(t,e)]@(eqs_of_subst s1)@(eqs_of_subst s2)@(eqs_of_subst s3)
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
                            let items = List.fold (fun s (id,_) -> (id, Ty.fresh_tyvar())::s) List.empty s 
                            let dummyenv = (List.map (fun (id,v) -> (id, Ty.TyVar v)) items) @ dummyenv
                            let (newsubst,neweqs,ret) = List.fold2
                                                            (fun (newsubst',neweqs',ret') (id,e) (_,v) -> 
                                                                let (subst,ty) = ty_exp dummyenv e 
                                                                let newsubst' = subst@newsubst' 
                                                                let neweqs' = (Ty.TyVar v,ty)::neweqs' 
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
                let tyarg = Ty.TyVar (Ty.fresh_tyvar ())
                let (subst,tyret) = ty_exp ((id,tyarg)::tyenv) exp
                let ty = Ty.TyFunc (tyarg, tyret)
                let eqs = eqs_of_subst subst
                let s3 = unify eqs
                in (s3, (subst_type s3 ty))
            | MatchExp (expr, cases) ->
                let domv = Ty.fresh_tyvar()
                let domty = Ty.TyVar domv
                let (st,ty) = ty_exp tyenv expr
                let eqs = eqs_of_subst st
                let eqs = List.fold (fun s (pt,ex) -> let (eqs1,binds1) = ty_try_match ty pt tyenv in let env1 = binds1 @ tyenv in let (se, tye) = ty_exp env1 ex in (domty,tye)::eqs1@(eqs_of_subst se)@s) eqs cases
                let s3 = unify(eqs)
                in  (s3, (subst_type s3 domty))

            | AppExp (exp1, exp2) ->
                let (subst1,tyexp1) = ty_exp tyenv exp1
                let (subst2,tyexp2) = ty_exp tyenv exp2

                let tyvret = Ty.fresh_tyvar ()

                let eqs = (tyexp1, Ty.TyFunc (tyexp2, Ty.TyVar tyvret))::eqs_of_subst(subst1)@eqs_of_subst(subst2)
                let s3 = unify eqs
                in (s3, (subst_type s3 (Ty.TyVar tyvret)))

            | LLit v -> 
                //failwith "not impl"
                let ety = Ty.TyVar (Ty.fresh_tyvar())
                let ty = Ty.TyList ety
                let (s,t) = List.foldBack (fun x (s,t) -> let (s',t') = ty_exp tyenv x in ((t',ety)::t,(eqs_of_subst s')@s)) v ([],[])
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
                            let items = List.fold (fun s (id,_) -> (id, Ty.fresh_tyvar())::s) List.empty s 
                            let dummyenv = (List.map (fun (id,v) -> (id, Ty.TyVar v)) items) @ dummyenv
                            let (newsubst,neweqs,ret) = List.fold2
                                                            (fun (newsubst',neweqs',ret') (id,e) (_,v) -> 
                                                                let (subst,ty) = ty_exp dummyenv e 
                                                                let newsubst' = subst@newsubst' 
                                                                let neweqs' = (Ty.TyVar v,ty)::neweqs' 
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
                        //let (tyrets, newtyenv) = Typing.ty_decl tyenv decl
                        //let _ = List.iter (fun (id,v) -> printfn "type %s = %s" id (Ty.ToString v)) tyrets;
                        //let (rets, newenv) = eval_decl env decl
                        //let _ = List.iter (fun (id,v) -> printfn "val %s = %s" id (pp_val v)) rets
                        let (tyrets, newtyenv) = Typing.ty_decl tyenv decl
                        //let _ = List.iter (fun (id,v) -> printfn "type %s = %s" id ()) tyrets;
                        let (rets, newenv) = eval_decl env decl
                        let ziped = List.zip rets tyrets 
                        let _ = List.iter (fun ((id,v),(id,t)) -> printfn "val %s : %s = %s" id (Ty.ToString t) (pp_val v)) ziped
                        let (reader,p) = Reader.trunc reader p
                        in
                            read_eval_print newenv newtyenv reader p
                    with
                        | v -> printfn "%s" v.Message;
                               read_eval_print env tyenv reader p

                | ParserCombinator.Fail(p,((i,l,c),msg)) ->
                    let (reader, p) = Parser.errorRecover reader p
                    in  printfn "SyntaxError (%d, %d) : %s" l c msg;
                        read_eval_print env tyenv reader p

        let initial_env =
            Environment.empty |>
            (Environment.extend "x" (IntV 10)) |> 
            (Environment.extend "v" (IntV  5)) |>
            (Environment.extend "i" (IntV 1))

        let initial_tyenv =
            Environment.empty |>
            (Environment.extend "x" Ty.TyInt) |> 
            (Environment.extend "v" Ty.TyInt) |>
            (Environment.extend "i" Ty.TyInt)

        let run () = read_eval_print initial_env initial_tyenv (Reader.create System.Console.In) Position.start

[<EntryPoint>]
let main argv = 
    let alpha = Interpreter.Ty.fresh_tyvar () 
    let beta = Interpreter.Ty.fresh_tyvar () 
    let ans1 = Interpreter.Typing.subst_type [(alpha, Interpreter.Ty.TyInt)] (Interpreter.Ty.TyFunc (Interpreter.Ty.TyVar alpha, Interpreter.Ty.TyBool))
    let _ = printfn "%A" ans1
    let ans2 = Interpreter.Typing.subst_type [(beta, (Interpreter.Ty.TyFunc (Interpreter.Ty.TyVar alpha, Interpreter.Ty.TyInt))); (alpha, Interpreter.Ty.TyBool)] (Interpreter.Ty.TyVar beta)
    let _ = printfn "%A" ans2
    let subst1 = Interpreter.Typing.unify [(Interpreter.Ty.TyVar alpha, Interpreter.Ty.TyInt)]
    let _ = printfn "%A" subst1
    let subst2 = Interpreter.Typing.unify [(Interpreter.Ty.TyFunc(Interpreter.Ty.TyVar alpha, Interpreter.Ty.TyBool), Interpreter.Ty.TyFunc(Interpreter.Ty.TyFunc(Interpreter.Ty.TyInt, Interpreter.Ty.TyVar beta), Interpreter.Ty.TyVar beta))]
    let _ = printfn "%A" subst2
    in  Interpreter.Repl.run (); 0 // 整数の終了コードを返します

