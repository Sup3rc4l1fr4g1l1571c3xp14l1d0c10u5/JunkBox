module ParserCombinator =
    type ParserReader = 
         class 
            val reader: System.IO.TextReader; 
            val buffer: System.Text.StringBuilder;
            new (reader) = { reader = reader; buffer = System.Text.StringBuilder(); }
            member x.Item 
                with get i = 
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
            member x.submatch (i1:int) (s2:string) =
                if (i1 < 0) || (x.[s2.Length + i1 - 1] = '\u0000') 
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
         end

    type ErrorPosition = (int * string) 
    type ParserState<'a> = Success of pos:int * value:'a * errPos:ErrorPosition
                         | Fail    of pos:int * errPos:ErrorPosition

    type Parser<'a> = ParserReader -> int -> ErrorPosition -> ParserState<'a>

    let succ (pos:int) (value:'a ) (errPos:ErrorPosition) =
        Success (pos, value, errPos)            

    let fail (pos:int) (msg:string) (errPos:ErrorPosition) =
        let (maxpos,_) = errPos
        in  if pos > maxpos 
            then Fail (pos, (pos, msg)) 
            else Fail (pos, errPos) 
            
    let char (pred : char -> bool) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            if (reader.[pos] <> '\u0000') && (pred reader.[pos]) 
            then succ (pos+1) reader.[pos] errPos 
            else fail pos ("char: not match character "+(if (reader.[pos] <> '\u0000') then reader.[pos].ToString() else "EOS")) errPos

    let anychar (chs:string) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            if (reader.[pos] <> '\u0000') && (chs.IndexOf(reader.[pos]) <> -1 )
            then succ (pos+1) reader.[pos] errPos
            else fail pos ("anychar: not match character  "+(if (reader.[pos] <> '\u0000') then reader.[pos].ToString() else "EOS")) errPos

    let str (s:string) =
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            if reader.submatch pos s 
            then succ (pos+s.Length) s errPos
            else fail pos ("str: require is '"+s+"' but get"+(if (reader.[pos] <> '\u0000') then reader.[pos].ToString() else "EOS")+".") errPos

    let any () = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            if reader.[pos] <> '\u0000' 
            then succ (pos+1) reader.[pos]  errPos
            else fail pos "any: require any but get EOF." errPos

    let not (parser:Parser<'a>) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            match parser reader pos errPos with
            | Fail    _ -> succ pos () errPos
            | Success _ -> fail pos "not: require rule was fail but success." errPos

    let select (pred:'a->'b) (parser:Parser<'a>) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            match parser reader pos errPos with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (pos, value, max2) -> succ pos (pred value) max2

    let asString (parser:Parser<'a list>) = 
        select (fun x -> List.fold (fun s x -> s + x.ToString()) "" x) parser

    let where (pred:'a->bool) (parser:Parser<'a>) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            match parser reader pos errPos with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (_, value, max2) as f -> if pred value then f else fail pos "where: require rule was fail but success." max2

    let opt (parser:Parser<'a>) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  ->       
            match parser reader pos errPos with
            | Fail (pos, max2)  -> succ pos None max2
            | Success (pos, value, max2) -> succ pos (Some value) max2

    let seq (parsers:Parser<'a> list) =
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (errPos:ErrorPosition) (values: 'a list) =
                match parsers with
                | []   -> succ pos (List.rev values) errPos
                | x::xs -> 
                    match x reader pos errPos with
                    | Fail    (pos, max2) -> Fail (pos, max2)
                    | Success (pos, value, max2) -> loop xs pos max2 (value :: values)
            in loop parsers pos errPos [];

    let choice(parsers:Parser<'a> list) =
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (errPos:ErrorPosition) =
                match parsers with
                | []   -> fail pos "choice: not match any rules." errPos
                | x::xs -> 
                    match x reader pos errPos with
                    | Fail (_, max2) -> loop xs pos max2
                    | Success _ as ret -> ret;
            in loop parsers pos errPos;

    let repeat (parser:Parser<'a>) = 
        fun (reader:ParserReader) (pos : int) (errPos:ErrorPosition) -> 
            let rec loop pos values errPos = 
                match parser reader pos errPos with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in loop pos [] errPos

    let repeat1 (parser:Parser<'a>) = 
        fun (reader:ParserReader) (pos : int) (errPos:ErrorPosition) -> 
            let rec loop pos values errPos = 
                match parser reader pos errPos with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in 
                match parser reader pos errPos with
                | Fail    (pos, max2) -> fail pos "repeat1: not match rule" max2
                | Success (pos, value, max2) -> loop pos [value] max2

    let andBoth (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:ParserReader) (pos : int) (errPos:ErrorPosition) -> 
            match lhs reader pos errPos with
            | Fail    (pos1, max2) -> fail pos "andBoth: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andBoth: not match right rule" max3
                | Success (pos2, value2, max3) -> succ pos2 (value1, value2) max3 

    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:ParserReader) (pos : int) (errPos:ErrorPosition) -> 
            match lhs reader pos errPos with
            | Fail    (pos1, max2) -> fail pos "andRight: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andRight: not match right rule" max3
                | Success (pos2, value2, max3) ->  succ pos2 value2 max3

    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (reader:ParserReader) (pos : int) (errPos:ErrorPosition)-> 
            match lhs reader pos errPos with
            | Fail    (pos1, max2) -> fail pos "andLeft: not match left rule" max2
            | Success (pos1, value1, max2) -> 
                match rhs reader pos1 max2 with
                | Fail    (pos2, max3) -> fail pos "andLeft: not match left rule" max3
                | Success (pos2, value2, max3) -> succ pos2 value1 max3 

    let quote (p:unit -> Parser<'a>) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  ->  (p ()) reader pos errPos

    let success (p:unit->'a) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  ->  succ pos (p ()) errPos

    let failure (msg:string) = 
        fun (reader:ParserReader) (pos:int) (errPos:ErrorPosition)  ->  fail pos msg errPos

    type Memoizer = { add: (unit -> unit) -> unit; reset : unit -> unit }
    let memoizer () = 
        let handlers = ref List.empty
        in
            {
                add = fun (handler:unit -> unit) -> handlers := handler :: !handlers;
                reset = fun () -> List.iter (fun h -> h()) !handlers
            }
        
    let memoize (memoizer: Memoizer) (f : Parser<'a>) = 
        let dic = System.Collections.Generic.Dictionary<(ParserReader * int * ErrorPosition), ParserState<'a>> ()
        let _ = memoizer.add (fun () -> dic.Clear() )
        in  fun x y z -> 
                match dic.TryGetValue((x,y,z)) with 
                | true, r -> r
                | _       -> dic.[(x,y,z)] <- f x y z;
                             dic.[(x,y,z)]

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
    module Syntax =
        type id = string

        type binOp = Plus | Minus | Mult | Divi | Lt | Gt | Le | Ge | Eq | Ne | Cons

        type tyvar = int
        type ty = 
              TyInt  
            | TyBool 
            | TyFunc of ty * ty
            | TyList of ty
            | TyVar of tyvar

        let fresh_tyvar = 
            let counter = ref 0
            let body () = let v = !counter in counter := v + 1; v
            in  body

        let freevar_ty ty =
            let rec loop ty ret =
                match ty with
                | TyInt  -> ret
                | TyBool -> ret
                | TyFunc (tyarg,tyret) -> loop tyarg ret |> loop tyret
                | TyList ty -> loop ty ret
                | TyVar id -> Set.add id ret     
            in loop ty Set.empty

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
        let LT = ws(str "<")
        let GT = ws(str ">")
        let LE = ws(str "<=")
        let GE = ws(str ">=")
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

        let Expr_ = ref (success(fun () -> ILit 0))
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

        let PatternExpr_ = ref (success(fun () -> ILitP 0))
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
            (ID.Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | id::[] -> (id,e) | id::args -> (id,List.foldBack (fun x s -> FunExp (x, s)) args e))) |> memoize memor
            

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

        let MExpr = AppExpr.And(choice[MULT;DIV].And(AppExpr).Many()).Select(fun (l,r) -> List.fold (fun l (op,r) -> match op with |"*" -> BinOp (Mult, l, r)|"/" -> BinOp (Divi, l, r)) l r);
        let PExpr = MExpr.And(choice[PLUS;MINUS].And(MExpr).Many()).Select(fun (l,r) -> List.fold (fun l (op,r) -> match op with |"+" -> BinOp (Plus, l, r)|"-" -> BinOp (Minus, l, r)) l r);

        let ConsExpr = 
            PExpr.And(COLCOL.AndR(PExpr).Many()).Select(fun (head, tail) -> List.reduceBack (fun x s -> BinOp(Cons, x, s)) (head::tail) );

        let EqExpr = 
            choice [ 
                ConsExpr.And(choice[EQ;NE]).And(ConsExpr).Select(fun ((l,op), r) -> match op with |"=" -> BinOp (Eq, l, r)|"<>" -> BinOp (Ne, l, r) );
                ConsExpr;
            ]

        let LTExpr = 
            choice [ 
                EqExpr.And(choice[LE;GE;LT;GT]).And(EqExpr).Select(fun ((l,op), r) -> match op with |"<=" -> BinOp (Le, l, r)|">=" -> BinOp (Ge, l, r) |"<" -> BinOp (Lt, l, r)|">" -> BinOp (Gt, l, r));
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

        let toplevel = (success(fun () -> ()).Select(fun _ -> memor.reset() )).AndR(choice[ LetRecStmt; LetStmt; ExprStmt ].AndL(SEMISEMI))

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

    //module Set = 
    //    type 'a t = 'a list

    //    let empty = []

    //    let singleton x = [x]

    //    let to_list x = x

    //    let rec insert x = function
    //        [] -> [x]
    //      | y::rest -> if x = y then y :: rest else y :: insert x rest

    //    let union xs ys = 
    //      List.fold (fun zs x -> insert x zs) ys xs

    //    let rec remove x = function
    //        [] -> []
    //      | y::rest -> if x = y then rest else y :: remove x rest

    //    let diff xs ys =
    //      List.fold (fun zs x -> remove x zs) xs ys

    //    let memq = List.contains
    
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
        type tyenv = ty Environment.t

        type subst = (tyvar * ty) list

        let rec subst_type ss ty = 
            let rec subst_type' s ty = 
                let (v, t) = s
                in  match ty with
                    | TyInt -> TyInt
                    | TyBool -> TyBool
                    | TyFunc (arg, ret)-> TyFunc (subst_type' s arg, subst_type' s ret)
                    | TyList ty -> TyList (subst_type' s ty)
                    | TyVar n -> if v = n then t else ty
            in
                List.fold (fun s x -> subst_type' x s) ty ss

        let eqs_of_subst (s:subst) : (ty*ty) list = 
            List.map (fun (v,t) -> (TyVar v, t)) s

        let subst_eqs (s:subst) (eqs: (ty*ty) list) : (ty*ty) list = 
            List.map (fun (t1,t2) -> (subst_type s t1 , subst_type s t2)) eqs

        let unify (eqs:(ty*ty) list) : subst =
            let rec loop eqs ret =
                match eqs with
                | [] -> ret
                | (ty1,ty2) :: eqs when ty1 = ty2 -> loop eqs ret
                | (TyVar id, ty) :: eqs 
                | (ty, TyVar id) :: eqs ->
                    if Set.contains id (Syntax.freevar_ty ty) 
                    then failwith "error"
                    else 
                        let ret = (id,ty) :: ret
                        let eqs = List.map (fun (ty1,ty2) -> (subst_type ret ty1, subst_type ret ty2)) eqs
                        in  loop eqs ret
                | (TyFunc (tyarg1, tyret1), TyFunc (tyarg2, tyret2)) :: eqs  -> loop ((tyarg1, tyarg2)::(tyret1, tyret2)::eqs) ret
                | _ -> failwith "error"
            in  loop eqs List.empty
            
        let rec pp_ty v =
            match v with
            | TyInt -> "int"
            | TyBool -> "bool"
            | TyFunc (arg, ret)-> sprintf "%s -> %s" (pp_ty arg) (pp_ty ret)
            | TyList ty -> sprintf "%s list" (pp_ty ty)
            | TyVar n -> sprintf "'%c" (char n + 'a')

        let ty_prim op t1 t2 =
            match (op, t1, t2) with
            | Plus, TyInt, TyInt-> ([(t1,TyInt);(t2,TyInt)],TyInt)
            | Plus, _, _ -> failwith ("Both arguments must be integer: +")
            | Minus, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyInt)
            | Minus, _, _ -> failwith ("Both arguments must be integer: -")
            | Mult, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyInt)
            | Mult, _, _ -> failwith ("Both arguments must be integer: *")
            | Divi, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyInt)
            | Divi, _, _ -> failwith ("Both arguments must be integer: /")
            | Lt, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyBool)
            | Lt, _, _ -> failwith ("Both arguments must be integer: <")
            | Le, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyBool)
            | Le, _, _ -> failwith ("Both arguments must be integer: <=")
            | Gt, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyBool)
            | Gt, _, _ -> failwith ("Both arguments must be integer: >")
            | Ge, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyBool)
            | Ge, _, _ -> failwith ("Both arguments must be integer: >=")
            | Eq, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyBool)
            | Eq, TyBool, TyBool -> ([(t1,TyBool);(t2,TyBool)],TyBool)
            | Eq, _, _ -> failwith ("Both arguments must be integer: =")
            | Ne, TyInt, TyInt -> ([(t1,TyInt);(t2,TyInt)],TyBool)
            | Ne, TyBool, TyBool -> ([(t1,TyBool);(t2,TyBool)],TyBool)
            | Ne, _, _ -> failwith ("Both arguments must be integer: <>")
            | Cons, t1, TyList t2 -> ([(t1,t2)],TyList t2)
            | Cons, _, _ -> failwith ("right arguments must be list: ::")

        let rec ty_try_match ty pat env = 
            failwith "not impl"
            //match pat with 
            //| VarP id -> (ty, ((id, ty) :: env))
            //| AnyP -> (ty, env)
            //| ILitP v when ty = TyInt -> (TyInt, env)
            //| BLitP v when ty = TyBool -> (TyBool, env)
            //| LLitP pats -> 
            //    let rec loop p v env =
            //        match p, v with 
            //        | [], ty -> (TyList ty, env)
            //        | (p::ps), ty -> 
            //            let (t, e) = ty_try_match v p env 
            //            in  if ty = TyAny || ty = t then loop ps t e else failwith "list item missmatch"
            //    in  loop pats value env
            //| NilP -> (TyList TyAny, env)
            //| ConsP (x,y) ->
            //    let (t1, e1) = ty_try_match value x env 
            //    let (t2, e2) = ty_try_match value t e1 
            //    in  if value = TyAny || ty = t then loop ps t e else failwith "list item missmatch"


        let rec ty_exp tyenv = function
            | Var x -> 
                try ([], Environment.lookup x tyenv) with 
                    | Environment.Not_bound -> failwithf "Variable not bound: %A" x
            | ILit i -> ([], TyInt)
            | BLit b -> ([], TyBool)
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
                let eqs = [(c,TyBool);(t,e)]@(eqs_of_subst s1)@(eqs_of_subst s2)@(eqs_of_subst s3)
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
                //let newenv = List.fold (fun tyenv (id,e) -> let v = eval_exp tyenv e in  (id, v)::tyenv ) tyenv ss
                let (subst,ty) = ty_exp newtyenv b
                let eqs = eqs_of_subst (newsubst @ subst)
                let s3 = unify eqs
                in (s3, (subst_type s3 ty))

            | LetRecExp (ss,b) ->
                let dummytyenv = ref Environment.empty
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
                //let dummytyenv = ref Environment.empty
                //let newtyenv = List.fold (fun tyenv s -> List.fold (fun tyenv' (id,e) -> (id, e)::tyenv') tyenv s ) tyenv ss
                //in  dummytyenv := newtyenv;
                //    ty_exp newtyenv b

            | FunExp (id, exp) -> 
                failwith "not impl"
                //let ret = ty_exp tyenv exp 
                //in  TyFunc (TyNil, ret)
            | MatchExp (expr, cases) ->
                failwith "not impl"
                //let value = ty_exp tyenv expr
                //let rec loop cases retty =
                //    match cases with
                //    | [] -> retty;
                //    | (pat,body)::xs -> 
                //        let (ty, tyenv) = ty_try_match value pat tyenv 
                //        in  let ty = ty_exp tyenv body 
                //            in  if retty.IsNone || ty = retty.Value then loop xs (Some ty) else failwith "body type missmatch"
                //in 
                //    match loop cases None with
                //    | None -> TyNil
                //    | Some ty -> ty
            | AppExp (exp1, exp2) ->
                failwith "not impl"
                //let funval = ty_exp env exp1 in
                //let arg = ty_exp env exp2 in
                //    match funval with
                //    | TyFunc (arg,ret) when arg = ret -> ret
                //    | _ -> failwith ("Non-function value is applied")
            | LLit v -> 
                //failwith "not impl"
                let ety = TyVar (fresh_tyvar())
                let ty = TyList ety
                let (s,t) = List.foldBack (fun x (s,t) -> let (s',t') = ty_exp tyenv x in ((t',ety)::t,(eqs_of_subst s')@s)) v ([],[])
                let eqs = s@t
                let s3 = unify eqs
                in  (s3, subst_type s3 ty)
        let ty_decl tyenv = function
            | ExpStmt e -> 
                let (s, t) = ty_exp tyenv e in ([("-", t)], tyenv) 
            | LetStmt ss -> 
                //failwith "not impl"
                //List.fold (fun (ret,env) (id,e) -> let v = eval_exp env e in  ((id, v)::ret,(id, v)::env)) ([],env) s
                //List.fold (fun s x -> List.fold (fun (ret,env') (id,e) -> let v = ty_exp env e in  (id, v)::ret,(id, v)::env') s x ) ([],env) ss
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
                //let newenv = List.fold (fun tyenv (id,e) -> let v = eval_exp tyenv e in  (id, v)::tyenv ) tyenv ss
                //let (subst,ty) = ty_exp newtyenv b
                let eqs = eqs_of_subst (newsubst)
                let s3 = unify eqs
                let ret = List.map (fun (id,ty) -> (id, subst_type s3 ty)) ret
                in  (ret, newtyenv)
 
            | LetRecStmt ss -> 
                failwith "not impl"
                //let dummyenv = ref Environment.empty
                //let ret = List.fold (fun env s -> List.fold (fun (ret,env') (id,e) -> (id, e)::ret,(id, e)::env') env s ) ([],env) ss
                //in  dummyenv := snd ret;
                //    ret

    module Repl =
        open Syntax
        open Typing
        open Eval

        let rec read_eval_print env tyenv =
            printf "# ";
            match Parser.toplevel (ParserCombinator.ParserReader System.Console.In) 0 (0, "") with
                | ParserCombinator.Success (p,decl,_) -> 
                    try 
                        let _ = printfn "%A" decl
                        let (tyrets, newtyenv) = Typing.ty_decl tyenv decl
                        let _ = List.iter (fun (id,v) -> printfn "type %s = %s" id (pp_ty v)) tyrets;
                        let (rets, newenv) = eval_decl env decl in
                            List.iter (fun (id,v) -> printfn "val %s = %s" id (pp_val v)) rets;
                            read_eval_print newenv newtyenv
                    with
                        | v -> printfn "%s" v.Message;
                               read_eval_print env tyenv

                | ParserCombinator.Fail(p,(i,msg)) ->
                    printfn "Syntax error[%d]: %s" i msg;
                    read_eval_print env tyenv

        let initial_env =
            Environment.empty |>
            (Environment.extend "x" (IntV 10)) |> 
            (Environment.extend "v" (IntV  5)) |>
            (Environment.extend "i" (IntV 1))

        let initial_tyenv =
            Environment.empty |>
            (Environment.extend "x" TyInt) |> 
            (Environment.extend "v" TyInt) |>
            (Environment.extend "i" TyInt)

        let run () = read_eval_print initial_env initial_tyenv

[<EntryPoint>]
let main argv = 
    let alpha = Interpreter.Syntax.fresh_tyvar () 
    let beta = Interpreter.Syntax.fresh_tyvar () 
    let ans1 = Interpreter.Typing.subst_type [(alpha, Interpreter.Syntax.TyInt)] (Interpreter.Syntax.TyFunc (Interpreter.Syntax.TyVar alpha, Interpreter.Syntax.TyBool))
    let _ = printfn "%A" ans1
    let ans2 = Interpreter.Typing.subst_type [(beta, (Interpreter.Syntax.TyFunc (Interpreter.Syntax.TyVar alpha, Interpreter.Syntax.TyInt))); (alpha, Interpreter.Syntax.TyBool)] (Interpreter.Syntax.TyVar beta)
    let _ = printfn "%A" ans2
    let subst1 = Interpreter.Typing.unify [(Interpreter.Syntax.TyVar alpha, Interpreter.Syntax.TyInt)]
    let _ = printfn "%A" subst1
    let subst2 = Interpreter.Typing.unify [(Interpreter.Syntax.TyFunc(Interpreter.Syntax.TyVar alpha, Interpreter.Syntax.TyBool), Interpreter.Syntax.TyFunc(Interpreter.Syntax.TyFunc(Interpreter.Syntax.TyInt, Interpreter.Syntax.TyVar beta), Interpreter.Syntax.TyVar beta))]
    let _ = printfn "%A" subst2
    in  Interpreter.Repl.run (); 0 // 整数の終了コードを返します

