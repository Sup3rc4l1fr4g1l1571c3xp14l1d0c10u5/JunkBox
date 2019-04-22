
module ParserCombinator =
    type MaxPosition = (int * string)
    type ParserState<'a> = Success of pos:int * value:'a * max:MaxPosition
                         | Fail    of pos:int * max:MaxPosition

    type Parser<'a> = string -> int -> MaxPosition -> ParserState<'a>

    let succ (pos:int) (value:'a ) (max:MaxPosition) =
        Success (pos, value, max)            

    let fail (pos:int) (msg:string) (max:MaxPosition) =
        let (maxpos,_) = max
        in  if pos > maxpos 
            then Fail (pos, (pos, msg)) 
            else Fail (pos, max) 

    let char (pred : char -> bool) = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            if (str.Length > pos) && (pred str.[pos]) 
            then succ (pos+1) str.[pos] max 
            else fail pos ("char: not match character "+(if (str.Length > pos) then str.[pos].ToString() else "EOS")) max

    let anychar (chs:string) = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            if (str.Length > pos) && (chs.IndexOf(str.[pos]) <> -1 )
            then succ (pos+1) str.[pos] max
            else fail pos ("anychar: not match character  "+(if (str.Length > pos) then str.[pos].ToString() else "EOS")) max

    let submatch (s1:string) (i1:int) (s2:string) =
        if (i1 < 0) || (s1.Length < s2.Length + i1) 
        then false
        else
            let rec loop (i1:int) (i2:int) = 
                if (i2 = 0) 
                then true 
                else
                    let i1, i2 = (i1-1, i2-1)
                    in  if s1.[i1] = s2.[i2]
                        then loop i1 i2
                        else false
            in  loop (i1 + s2.Length) (s2.Length)
        
    let str (s:string) =
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            if submatch str pos s 
            then succ (pos+s.Length) s max
            else fail pos ("str: require is '"+s+"' but get"+(if (str.Length > pos) then str.[pos].ToString() else "EOS")+".") max

    let any () = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            if str.Length > pos 
            then succ (pos+1) str.[pos]  max
            else fail pos "any: require anychar but get EOF." max

    let not (parser:Parser<'a>) = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            match parser str pos max with
            | Fail    _ -> succ pos () max
            | Success _ -> fail pos "not: require rule was fail but success." max

    let select (pred:'a->'b) (parser:Parser<'a>) = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            match parser str pos max with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (pos, value, max2) -> succ pos (pred value) max2

    let asString (parser:Parser<'a list>) = 
        select (fun x -> List.fold (fun s x -> s + x.ToString()) "" x) parser

    let where (pred:'a->bool) (parser:Parser<'a>) = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            match parser str pos max with
            | Fail    (pos, max2) -> Fail (pos, max2)
            | Success (_, value, max2) as f -> if pred value then f else fail pos "where: require rule was fail but success." max2

    let opt (parser:Parser<'a>) = 
        fun (str:string) (pos:int) (max:MaxPosition)  ->       
            match parser str pos max with
            | Fail (pos, max2)  -> succ pos None max2
            | Success (pos, value, max2) -> succ pos (Some value) max2

    let seq (parsers:Parser<'a> list) =
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (max:MaxPosition) (values: 'a list) =
                match parsers with
                | []   -> succ pos (List.rev values) max
                | x::xs -> 
                    match x str pos max with
                    | Fail    (pos, max2) -> Fail (pos, max2)
                    | Success (pos, value, max2) -> loop xs pos max2 (value :: values)
            in loop parsers pos max [];

    let choice(parsers:Parser<'a> list) =
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (max:MaxPosition) =
                match parsers with
                | []   -> fail pos "choice: not match any rules." max
                | x::xs -> 
                    match x str pos max with
                    | Fail (_, max2) -> loop xs pos max2
                    | Success _ as ret -> ret;
            in loop parsers pos max;

    let repeat (parser:Parser<'a>) = 
        fun (str:string) (pos : int) (max:MaxPosition) -> 
            let rec loop pos values max = 
                match parser str pos max with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in loop pos [] max

    let repeat1 (parser:Parser<'a>) = 
        fun (str:string) (pos : int) (max:MaxPosition) -> 
            let rec loop pos values max = 
                match parser str pos max with
                | Fail (pos,max2)  -> succ pos (List.rev values) max2
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in 
                match parser str pos max with
                | Fail    (pos, max2) -> fail pos "repeat1: not match rule" max2
                | Success (pos, value, max2) -> loop pos [value] max2

    let andBoth (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) (max:MaxPosition) -> 
            match lhs str pos max with
            | Fail    (pos, max2) -> fail pos "andBoth: not match left rule" max2
            | Success (pos, value1, max2) -> 
                match rhs str pos max2 with
                | Fail    (pos, max3) -> fail pos "andBoth: not match right rule" max3
                | Success (pos, value2, max3) -> succ pos (value1, value2) max3 

    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) (max:MaxPosition) -> 
            match lhs str pos max with
            | Fail    (pos, max2) -> fail pos "andRight: not match left rule" max2
            | Success (pos, value1, max2) -> 
                match rhs str pos max2 with
                | Fail    (pos, max3) -> fail pos "andRight: not match right rule" max3
                | Success (pos, value2, max3) ->  succ pos value2 max3

    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) (max:MaxPosition)-> 
            match lhs str pos max with
            | Fail    (pos, max2) -> fail pos "andLeft: not match left rule" max2
            | Success (pos, value1, max2) -> 
                match rhs str pos max2 with
                | Fail    (pos, max3) -> fail pos "andLeft: not match left rule" max3
                | Success (pos, value2, max3) -> succ pos value1 max3 

    let lazy_ (p:unit -> Parser<'a>) = 
        fun (str:string) (pos:int) (max:MaxPosition)  ->  (p ()) str pos max

    let success (p:unit->'a) = 
        fun (str:string) (pos:int) (max:MaxPosition)  ->  succ pos (p ()) max

    let failure () = 
        fun (str:string) (pos:int) (max:MaxPosition)  ->  fail pos "fail: fail by rule" max

    type Memoizer = { add: (unit -> unit) -> unit; reset : unit -> unit }
    let memoizer () = 
        let handlers = ref List.empty
        in
            {
                add = fun (handler:unit -> unit) -> handlers := handler :: !handlers;
                reset = fun () -> List.iter (fun h -> h()) !handlers
            }
        
    let memoize (memoizer: Memoizer) (f : Parser<'a>) = 
        let dic = System.Collections.Generic.Dictionary<(string * int * MaxPosition), ParserState<'a>> ()
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
        type binOp = Plus | Minus | Mult | Divi | Lt | Gt | Le | Ge | Eq | Ne
        type exp =
              Var of id
            | ILit of int
            | BLit of bool
            | BinOp of binOp * exp * exp
            | IfExp of exp * exp * exp
            | LetExp of (id * exp) list list * exp
            | FunExp of (id * exp) 
            | AppExp  of (exp * exp)
        type program =
            | ExpStmt of exp
            | LetStmt of (id * exp) list list

    module Parser =
        open ParserCombinator
        open ParserCombinator.OperatorExtension
        open Syntax;

        let isLower ch = 'a' <= ch && ch <= 'z'
        let isUpper ch = 'A' <= ch && ch <= 'Z'
        let isDigit ch = '0' <= ch && ch <= '9'
        let isIdHead ch = isLower(ch)
        let isIdBody ch = isLower(ch) || isDigit(ch) || (ch = '_') || (ch = '\'') 

        let WS = (anychar " \t\r\n").Many()
        let ws x = WS.AndR(x)

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
        let ID = Ident.Where(fun x -> (List.contains x ["true";"false";"if";"then";"else";"let";"in";"and";"fun"]) = false);
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

        let Expr_ = ref (success(fun () -> ILit 0))
        let Expr = lazy_(fun () -> !Expr_)

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
            ]

        let FunExpr =
            FUN.AndR(ID.Many1()).AndL(RARROW).And(Expr).Select(fun (args, e) -> List.foldBack (fun x s -> FunExp (x, s)) args e)


        let AExpr = 
            choice [ 
                FunExpr; 
                INTV.Select(fun x -> ILit x);
                TRUE.Select(fun x -> BLit true);
                FALSE.Select(fun x -> BLit false);
                ID.Select(fun x -> Var x);
                LPAREN.AndR(BinOpExpr).AndL(RPAREN);
                LPAREN.AndR(Expr).AndL(RPAREN)
            ]

        let AppExpr = AExpr.Many1().Select( fun x -> List.reduce (fun s x -> AppExp (s, x) ) x )

        let MExpr = AppExpr.And(choice[MULT;DIV].And(AppExpr).Many()).Select(fun (l,r) -> List.fold (fun l (op,r) -> match op with |"*" -> BinOp (Mult, l, r)|"/" -> BinOp (Divi, l, r)) l r);
        let PExpr = MExpr.And(choice[PLUS;MINUS].And(MExpr).Many()).Select(fun (l,r) -> List.fold (fun l (op,r) -> match op with |"+" -> BinOp (Plus, l, r)|"-" -> BinOp (Minus, l, r)) l r);

        let EqExpr = 
            choice [ 
                PExpr.And(choice[EQ;NE]).And(PExpr).Select(fun ((l,op), r) -> match op with |"=" -> BinOp (Eq, l, r)|"<>" -> BinOp (Ne, l, r) );
                PExpr;
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

        let IfExpr =
            IF.AndR(Expr).AndL(THEN).And(Expr).AndL(ELSE).And(Expr).Select( fun ((c,t),e) -> IfExp (c, t, e) )

        let memor = memoizer()

        let LetPrim =
            (ID.Many1().AndL(EQ).And(Expr).Select(fun (ids,e) -> match ids with | id::[] -> (id,e) | id::args -> (id,List.foldBack (fun x s -> FunExp (x, s)) args e)))
            

        let LetAndExpr =
            LET.AndR(LetPrim).And(AND.AndR(LetPrim).Many()).Select(fun (x,xs) -> (x::xs))
            |> memoize memor

        let LetExpr =
            (LetAndExpr).Many1().AndL(IN).And(Expr).Select(LetExp)

        let _ = Expr_ := choice [IfExpr ; LetExpr; LOrExpr; ]

        let LetStmt =
            (LetAndExpr).Many1().AndL(IN.Not()).Select( fun s -> LetStmt s)

        let ExprStmt =
            Expr.Select( fun x -> ExpStmt x)

        let toplevel = choice[ LetStmt; ExprStmt ].AndL(SEMISEMI)

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
            | ProcV of id * exp * dnval Environment.t
        and dnval = exval

        let pp_val v =
            match v with
            | IntV v -> v.ToString()
            | BoolV v -> if v then "true" else "false"
            | ProcV (id,exp,env) -> "<fun>"

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

            | FunExp (id, exp) -> ProcV (id, exp, env)      
            | AppExp (exp1, exp2) ->
                let funval = eval_exp env exp1 in
                let arg = eval_exp env exp2 in
                    match funval with
                    | ProcV (id, body, env') ->
                        let newenv = Environment.extend id arg env' in
                        eval_exp newenv body
                    | _ -> failwith ("Non-function value is applied")

        let eval_decl env = function
            | ExpStmt e -> 
                let v = eval_exp env e in (["-",v ], env) 
            | LetStmt ss -> 
                //List.fold (fun (ret,env) (id,e) -> let v = eval_exp env e in  ((id, v)::ret,(id, v)::env)) ([],env) s
                List.fold (fun s x -> List.fold (fun (ret,env') (id,e) -> let v = eval_exp env e in  (id, v)::ret,(id, v)::env') s x ) ([],env) ss


    module Repl =
        open Syntax
        open Eval

        let rec read_eval_print env =
            printf "# ";
            match Parser.toplevel (System.Console.ReadLine()) 0 (0, "") with
                | ParserCombinator.Success (p,decl,_) -> 
                    try 
                        printfn "%A" decl;
                        let (rets, newenv) = eval_decl env decl in
                            List.iter (fun (id,v) -> printfn "val %s = %s" id (pp_val v)) rets;
                            read_eval_print newenv
                    with
                        | v -> printfn "%s" v.Message;
                               read_eval_print env

                | ParserCombinator.Fail(p,(i,msg)) ->
                    printfn "Syntax error[%d]: %s" i msg;
                    read_eval_print env

        let initial_env =
            Environment.empty |>
            (Environment.extend "x" (IntV 10)) |> 
            (Environment.extend "v" (IntV  5)) |>
            (Environment.extend "i" (IntV 1))

        let _ = read_eval_print initial_env

[<EntryPoint>]
let main argv = 
    0 // 整数の終了コードを返します

// let makemult = fun maker -> fun x -> if x < 1 then 0 else 4 + maker maker (x + -1) in let times4 = fun x -> makemult makemult x in times4 3
