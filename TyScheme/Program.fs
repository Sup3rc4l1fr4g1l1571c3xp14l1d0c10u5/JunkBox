open System
open System.Linq.Expressions
open System.Text.RegularExpressions;

(* Value Type *)

type NumberV =
      ComplexV of real:NumberV * imaginary:NumberV
    | FractionV of numerator:int * denominator:int
    | IntV of value : int
    | RealV of value : double

module NumberVOp =
    let toComplex v =
        match v with
        | ComplexV _ -> v
        | FractionV _ -> ComplexV (v, IntV 0)
        | IntV _ -> ComplexV (v, IntV 0)
        | RealV _  -> ComplexV (v, IntV 0)

    let toFraction v =
        match v with
        | ComplexV _ -> failwith "cannt convert"
        | FractionV _ -> v
        | IntV v -> FractionV (v, 1)
        | RealV v  -> FractionV (int (v * 10000.0), 10000)

    let toInt v =
        match v with
        | ComplexV _ -> failwith "cannt convert"
        | FractionV _ -> failwith "cannt convert"
        | IntV _ -> v
        | RealV _  -> failwith "cannt convert"

    let toReal v =
        match v with
        | ComplexV _ -> failwith "cannt convert"
        | FractionV _ -> v
        | IntV v -> RealV ( float v)
        | RealV _  -> v

    let rec add lhs rhs =
        match (lhs, rhs) with
        | ComplexV (r1,i1), ComplexV (r2,i2)  -> ComplexV (add r1 r2, add i1 i2)
        | ComplexV _, _  -> add lhs (toComplex rhs)
        | _, ComplexV _  -> add (toComplex lhs) rhs

        | FractionV (n1,d1), FractionV (n2,d2) -> FractionV (n1*d2+n2*d1,d1*d2)
        | FractionV _,_ -> add lhs (toFraction rhs)
        | _, FractionV _ -> add (toFraction lhs) rhs

        | RealV v1, RealV v2   -> RealV (v1 + v2)
        | RealV _,_ -> add lhs (toReal rhs)
        | _, RealV _ -> add (toReal lhs) rhs

        | IntV v1, IntV v2 -> IntV (v1 + v2)
        | IntV _ ,_        -> add lhs (toInt rhs)
        |       _, IntV _  -> add (toInt lhs) rhs

        | _ -> failwith "cannot add"

    let neg v =
        match v with
        | ComplexV _ -> failwith "cannot neg" 
        | FractionV (n,d) -> FractionV (-n,d)
        | IntV v -> IntV (-v)
        | RealV v -> RealV (-v)

type Value  = 
      Symbol of value : string
    | Nil
    | Array of value : Value list
    | Cell of car: Value * cdr: Value
    | String of value : string
    | Char of value : char
    | Number of value : NumberV
    | Boolean of value : bool
    | Primitive of value : (Value -> Value)
    | Closure of Inst list * Value list
    | Error of subtype: string * message : string 

and Inst =
      Ld   of i:int * j:int
    | Ldc  of Value
    | Ldg  of Value
    | Ldf  of Inst list
    | Join
    | Rtn
    | App
    | Pop
    | Sel  of t_clause:(Inst list) * f_clause:(Inst list)
    | Def  of Value
    | Args of int
    | Stop


let escapeString (str:string) =
    let rec loop (str:char list) (result:string list) =
        match str with
            | [] -> result |> List.rev |> List.toSeq |> String.concat ""
            | '\r' :: xs -> loop xs ("\\r"::result)
            | '\t' :: xs -> loop xs ("\\t"::result)
            | '\n' :: xs -> loop xs ("\\n"::result)
            | '\f' :: xs -> loop xs ("\\f"::result)
            | '\b' :: xs -> loop xs ("\\b"::result)
            | '\\' :: xs -> loop xs ("\\"::result)
            |    x :: xs -> loop xs (string(x)::result)
    in loop (str.ToCharArray() |> Array.toList) []

let toString (value:Value) = 
    let rec loop (value:Value) (isCdr : bool) =
        match value with 
        | Symbol value -> value
        | Nil -> "()"
        | Array v -> "#(" + String.Join(", ", (List.map (fun x -> loop x false) v)) + ")"
        | Cell (car, cdr) -> 
            match isCdr with
            | false -> "(" + (loop car false) + " " + (loop cdr true) + ")"
            | true  -> (loop car false) + " " + (loop cdr true)
        | Char value -> 
            match value with
            | '\n' -> "#\\newline"
            | ' ' -> "#\\space"
            | _ -> "#\\" + value.ToString()
        | String value -> escapeString value
        | Number value -> value.ToString ()
        | Boolean false -> "#f"
        | Boolean true  -> "#t"
        | Primitive value -> sprintf "<Primitive: %s>" (value.ToString ())
        | Closure (value, env) -> sprintf "<Closure: %s>" (value.ToString ())
        | Error (subtype, message) -> sprintf "<%s: %s>" subtype message
    in loop value false

(* Scheme's primitive functions *)

let car (x: Value): Value =
    match x with
    | Cell (car, cdr) -> car
    | _               -> Error ("runtime error", (sprintf "Attempt to apply car on %s" (toString x)))

let cdr (x: Value): Value =
    match x with
    | Cell (car, cdr) -> cdr
    | _               -> Error ("runtime error", (sprintf "Attempt to apply cdr on %s" (toString x)))

let cadr = car << cdr

let isnull x =
    match x with
    | Nil -> true
    | _   -> false

let issymbol x = 
    match x with
    | Symbol _ -> true
    | _        -> false

let isnumber x = 
    match x with
    | Number _ -> true
    | _        -> false

let ispair x = 
    match x with
    | Nil  _
    | Cell _ -> true
    | _      -> false

let iserror x = 
    match x with
    | Error _ -> true
    | _       -> false

let list (values : Value list) : Value =
    List.foldBack (fun x y -> Cell (x,y)) values Nil

let length (x:Value) : int = 
    let rec loop (x:Value) (n:int) =
        match x with
        | Cell (a, d) -> loop d (n + 1)
        | Nil -> n
        | _ -> failwith "not pair"
    in  loop x 0

let eq (x:Value) (y:Value) : bool =
    match (x,y) with
    | (Symbol   v1, Symbol   v2) -> (v1 = v2)//(Object.Equals(x,y))
    | (Cell      _, Cell      _) -> (Object.Equals(x,y))
    | (Array     _, Array     _) -> (Object.Equals(x,y))
    | (Number   v1, Number   v2) -> (v1 = v2)
    | (Char     v1, Char     v2) -> (v1 = v2)
    | (String   v1, String   v2) -> (v1 = v2)
    | (Boolean  v1, Boolean  v2) -> (v1 = v2)
    | (Primitive _, Primitive _) -> (Object.Equals(x,y))
    | (Nil        , Nil        ) -> true
    | (          _,           _) -> false

let rec equal (x:Value) (y:Value) : bool =
    match (x,y) with
    | (Symbol        v1, Symbol        v2) -> (v1 = v2)//(Object.Equals(x,y))
    | (Cell     (a1,d1), Cell     (a2,d2)) -> (equal a1 a2) && (equal d1 d2)
    | (Array         v1, Array         v2) -> (List.forall2 equal v1 v2)
    | (Number        v1, Number        v2) -> (v1 = v2)
    | (Char     v1, Char     v2) -> (v1 = v2)
    | (String        v1, String        v2) -> (v1 = v2)
    | (Boolean       v1, Boolean       v2) -> (v1 = v2)
    | (Primitive      _, Primitive      _) -> (Object.Equals(x,y))
    | (Nil             , Nil             ) -> true
    | (               _,                _) -> false

(* compiler *)
module Compiler =
    let compile (expr:Value) : Inst list =
        let position_var (sym:Value) (ls:Value) : int option =
            let rec loop (ls:Value) (i:int) =
                match ls with
                | Nil            -> None
                | Symbol(_) as y -> if (eq sym y) then Some (-(i + 1)) else None
                | Cell(a, d)     -> if (eq sym a) then Some i else loop d (i + 1)
                | _              -> failwith "bad variable table"
            in loop ls 0

        let location (sym:Value) (ls:Value list) : (int * int) option  =
            let rec loop (ls:Value list) (i:int) =
                match ls with
                | [] -> None
                | a::d ->
                    match position_var sym a with
                    | Some j -> Some(i, j)
                    | None   -> loop d (i + 1)
            in loop ls 0

        let is_self_evaluation (expr:Value) : bool =
            ((ispair expr)) = false && ((issymbol expr) = false)

        let rec comp (expr: Value) (env: Value list) (code: Inst list): Inst list =
            if is_self_evaluation expr 
            then Ldc(expr) :: code
            else 
                match expr with
                | Symbol _ ->
                    match location expr env with
                    | Some (i, j) -> Ld(i, j) :: code
                    | None -> Ldg(expr) :: code
                | Cell(Symbol("quote"), Cell(v, Nil)) -> 
                    Ldc(v) :: code
                | Cell(Symbol("if"), Cell(cond, Cell(t, Nil))) -> 
                    let t_clause = comp t env [Join]
                    let f_clause = [Ldc (Symbol "*undef"); Join] 
                    in  comp cond env (Sel (t_clause, f_clause) :: code)
                | Cell(Symbol("if"), Cell(cond, Cell(t, Cell(e, Nil)))) -> 
                    let t_clause = comp t env [Join]
                    let f_clause = comp e env [Join]
                    in  comp cond env (Sel (t_clause, f_clause) :: code)
                | Cell((Symbol "lambda"), Cell(name, body)) ->
                    let body = comp_body body (name :: env) [Rtn]
                    in  Ldf(body) :: code
                | Cell((Symbol "define"), Cell((Symbol _) as sym, Cell(body, Nil))) -> 
                    comp body env (Def(sym)::code)
                | Cell(fn, args) -> 
                    complis args env ((Args (length args))::(comp fn env (App :: code)))
                | _ -> failwith "syntax error"
            
        and comp_body (body: Value) (env: Value list) (code: Inst list) : Inst list =
            match body with
            | Cell(x, Nil) -> comp x env code
            | Cell(x, xs) -> comp x env (Pop :: (comp_body xs env code))
            | _ -> failwith "syntax error"
        and complis (expr: Value) (env: Value list) (code: Inst list): Inst list =
            match (expr) with 
            | Nil -> code 
            | Cell(a,d) -> comp a env (complis d env code)
            | _ -> failwith "syntax error"
        in
            if iserror(expr)
            then [Ldc expr; Stop]
            else comp expr [] [Stop]

(* secd virtual machine *)
module VM =
    let get_lvar (e: Value list) (i: int) (j: int) : Value =
        let rec list_ref (x:Value) (n:int) : Value = 
            match x with
                | Nil -> failwith "out of range"
                | Cell(a,d) -> 
                    if n < 0 then failwith "out of range"
                    else if n = 0 then a
                    else list_ref d (n - 1)
                | _  -> failwith "not cell"
        let rec drop (ls: Value) (n: int): Value =
            if n <= 0 
            then ls
            else 
                match ls with
                | Cell (a, d) -> drop d (n - 1)
                | _ -> failwith "not cell"
        in
            if 0 <= j
            then list_ref (List.item i e) j
            else drop     (List.item i e) -(j + 1)
  
    let sum (x:Value) : Value = 
        match x with
        | Cell (Number l, xs) -> 
            let rec loop (x:Value) (n:NumberV) =
                match x with
                | Nil                 -> Number n
                | Cell (Number l, xs) -> loop xs (NumberVOp.add n l)
                | _ -> failwith "bad argument"
            in  loop xs l
        | _ -> failwith "bad argument"

    let global_environment: ((Value * Value) list) ref =
        ref [
                (Symbol "#t",    Boolean true);
                (Symbol "#f",    Boolean false);

                (Symbol "car",   Primitive (fun xs -> car (car xs)));
                (Symbol "cdr",   Primitive (fun xs -> cdr (car xs)));
                (Symbol "cons",  Primitive (fun xs -> Cell (car xs, cadr xs)));
                (Symbol "eq?",   Primitive (fun xs -> Boolean (eq (car xs) (cadr xs))));
                (Symbol "pair?", Primitive (fun xs -> Boolean (ispair (car xs))));
                (Symbol "+",     Primitive sum);
            ]

    let assoc (sym: Value) (dic: ((Value*Value) list) ref) : (Value * Value) option =
        let rec loop (dic: (Value*Value) list) =
            match dic with
            | [] -> None
            | ((key, value) as entry) :: d -> if (eq key sym) then Some entry else loop d
        in loop !dic        
        
    let get_gvar (sym:Value) : Value =
        match assoc sym global_environment with
        | Some (key, value) -> value
        | _ -> Error ("runtime error", String.Format("unbound variable:{0}",sym))

    type Context = { s: Value list; e: Value list; c: (Inst list); d: (Value list * Value list * (Inst list)) list; halt: bool }

    let vm (context:Context) : Context =
        if context.halt 
        then context
        else 
            match (context.c, context.s, context.e, context.d) with
            | (Ld(i,j)  ::c',                         s , e,                 d ) -> { context with s = (get_lvar e i j) :: s; e = e; c = c'; d = d }
            | (Ldc(v)   ::c',                         s , e,                 d ) -> { context with s = v::s; e = e; c = c'; d = d }
            | (Ldg(v)   ::c',                         s , e,                 d ) -> { context with s = (get_gvar v) :: s; e = e; c = c'; d = d }
            | (Ldf(v)   ::c',                         s , e,                 d ) -> { context with s = Closure (v, e)::s; e = e; c = c'; d = d }
            | (App      ::c', Primitive(f)  :: arg :: s', e,                 d ) -> { context with s = (f arg) :: s'; e = e; c = c'; d = d }
            | (App      ::c', Closure(f,e') :: arg :: s', e,                 d ) -> { context with s = []; e = arg :: e'; c = f; d = (s', e, c') :: d }
            | (Rtn      ::_ ,                     s1::[], e, (s2, e', c') :: d') -> { context with s = s1 :: s2; e = e'; c = c'; d = d' }
            | (Sel(t, f)::c',          (Boolean v) :: s', e,                 d ) -> { context with s = s'; e = e; c = (if v then t else f); d = ([],[],c') :: d }
            | (Join     ::_ ,                         s , e,   (_, _, c') :: d') -> { context with s = s; e = e; c = c'; d = d' }
            | (Pop      ::c',                    _ :: s', e,                 d ) -> { context with s = s'; e = e; c = c'; d = d }
            | (Args(v)  ::c',                         s , e,                 d ) -> 
                let rec loop a s n =
                    match (s,n) with
                    | (         _, 0) -> { context with s = a :: s; e = e; c = c'; d = d }
                    | (car :: cdr, _) -> loop (Cell(car, a)) cdr (n - 1)
                    | _               -> failwith "bad args"
                in loop Nil s v
            | (Def(sym):: c', body :: s', e, d) -> 
                global_environment := (sym, body) :: !global_environment;
                { context with s = sym :: s'; e = e; c = c'; d = d };
            | (Stop::c', s, e, d) -> { context with halt= true }
            | _ -> failwith "bad context"

(* parser combinator *)
module ParserCombinator =
    type ParserState<'a> = { pos:int; value:'a }
    type Parser<'a> = string -> int -> ParserState<'a> option

    let char (pred : char -> bool) = 
        fun (str:string) (pos:int) -> 
            if (str.Length > pos) && (pred str.[pos]) 
            then Some({ pos=pos+1; value=str.[pos]}) 
            else None

    let anychar (chs:string) = 
        fun (str:string) (pos:int) -> 
            if (str.Length > pos) && (chs.IndexOf(str.[pos]) <> -1 )
            then Some({ pos=pos+1; value=str.[pos]}) 
            else None

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
        fun (str:string) (pos:int) -> 
            if submatch str pos s 
            then Some({ pos=pos+s.Length; value=s}) 
            else None

    let any () = 
        fun (str:string) (pos:int) -> 
            if str.Length > pos 
            then Some({ pos=pos+1; value=str.[pos]}) 
            else None

    let not (parser:Parser<'a>) = 
        fun (str:string) (pos:int) -> 
            match parser str pos with
            | None   -> Some { pos=pos; value=() }
            | Some _ -> None

    let select (pred:'a->'b) (parser:Parser<'a>) = 
        fun (str:string) (pos:int) -> 
            match parser str pos  with
            | None   -> None
            | Some {pos=pos; value=value} -> Some {pos=pos; value=pred value}

    let where (pred:'a->bool) (parser:Parser<'a>) = 
        fun (str:string) (pos:int) -> 
            match parser str pos with
            | None   -> None
            | Some {pos=pos; value=value} as ret -> if pred value then ret else None

    let opt (parser:Parser<'a>) = 
        fun (str:string) (pos:int) ->       
            match parser str pos with
            | None   -> Some {pos=pos; value=None}
            | Some {pos=pos; value=value} -> Some {pos=pos; value=Some(value)} 

    let seq (parsers:Parser<'a> list) =
        fun (str:string) (pos:int) -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (values: 'a list) =
                match parsers with
                | []   -> Some {pos=pos; value=List.rev values}
                | x::xs -> 
                    match x str pos with
                    | None -> None
                    | Some {pos=pos; value=value} -> loop xs pos (value :: values)
            in loop parsers pos [];
    
    let choice(parsers:Parser<'a> list) =
        fun (str:string) (pos:int) -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) =
                match parsers with
                | []   -> None
                | x::xs -> 
                    match x str pos with
                    | None -> loop xs pos
                    | Some _ as ret -> ret;
            in loop parsers pos;

    let repeat (parser:Parser<'a>) = 
        fun (str:string) (pos : int) -> 
            let rec loop pos values = 
                match parser str pos with
                | None -> Some {pos=pos;value=List.rev values}
                | Some {pos=pos; value=value} -> loop pos (value :: values)
            in loop pos []

    let repeat1 (parser:Parser<'a>) = 
        fun (str:string) (pos : int) -> 
            let rec loop pos values = 
                match parser str pos with
                | None -> Some {pos=pos;value=List.rev values}
                | Some {pos=pos; value=value} -> loop pos (value :: values)
            in 
                match parser str pos with
                | None -> None
                | Some {pos=pos; value=value} -> loop pos [value]


    let andBoth (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) -> 
            match lhs str pos with
            | None -> None
            | Some {pos=pos; value=value1} -> 
                match rhs str pos with
                | None -> None
                | Some {pos=pos; value=value2} -> 
                    Some {pos=pos; value=(value1, value2)} 

    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) -> 
            match lhs str pos with
            | None -> None
            | Some {pos=pos; value=value1} -> 
                match rhs str pos with
                | None -> None
                | Some {pos=pos; value=value2} -> 
                    Some {pos=pos; value=value2} 

    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) -> 
            match lhs str pos with
            | None -> None
            | Some {pos=pos; value=value1} -> 
                match rhs str pos with
                | None -> None
                | Some {pos=pos; value=value2} -> 
                    Some {pos=pos; value=value1} 

    let lazy_ (p:unit -> Parser<'a>) = 
        fun (str:string) (pos:int) ->  (p ()) str pos

module ParserCombinatorExt =
    open ParserCombinator
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

module Parser =
    open ParserCombinator
    open ParserCombinatorExt
    
    let isdigit  (ch:char) = '0' <= ch && ch <= '9'
    let isalpha  (ch:char) = ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z')
    let issphead (ch:char) = "!$%&*/:<=>?^_~".IndexOf(ch) <> -1
    let issptail (ch:char) = "!$%&*/:<=>?^_~+-.@".IndexOf(ch) <> -1
    let ishead   (ch:char) = issphead(ch) || isalpha(ch)
    let istail   (ch:char) = issptail(ch) || isalpha(ch) || isdigit(ch)
    let isspace  (ch:char) = " \t\r\n".IndexOf(ch) <> -1

    let rec whitespace = (ParserCombinator.char isspace).Many()

    and start = whitespace.AndR(expr).AndL(whitespace)

    and expr = lazy_(fun () -> whitespace.AndR(quoted.Or(list_).Or(vector).Or(atom)))
    
    and char = 
        let space     = (str "#\\space").Select(fun _ -> ' ')
        let newline   = (str "#\\newline").Select(fun _ -> '\n')
        let character = (str "#\\").AndL(any()).Select(fun x -> x.[0])
        in  whitespace.AndR(space.Or(newline).Or(character)).Select(Char)

    and bool = 
        let trueV  = (str "#t").Select(fun _ -> true)
        let falseV = (str "#f").Select(fun _ -> false)
        in  whitespace.AndR(trueV.Or(falseV)).Select(Boolean)

    and string = 
        let ch = 
            let dquote    = (str "\\\"").Select(fun _ -> '"')
            let cr        = (str "\\r" ).Select(fun _ -> '\r')
            let lf        = (str "\\n" ).Select(fun _ -> '\n')
            let tab       = (str "\\t" ).Select(fun _ -> '\t')
            let formfeed  = (str "\\f" ).Select(fun _ -> '\f')
            let notescape = ParserCombinator.char (fun x -> x <> '\\')
            in  dquote.Or(cr).Or(lf).Or(tab).Or(formfeed).Or(notescape)
        in whitespace.AndR(ParserCombinator.char (fun x -> x = '"')).AndR(ch.Many()).AndL(ParserCombinator.char (fun x -> x = '"')).Select(fun x -> List.fold (fun s x -> s + x.ToString()) "" x |> String)

    and  number = whitespace.AndR(digit)

    and  digit = 
         let digit_prefix = (str "#d").Option()
         let accuracy_prefix = (str "#i").Or(str "#e").Option()
         let prefix = digit_prefix.AndR(accuracy_prefix).Or(accuracy_prefix.AndL(digit_prefix))
         let sign = (str "+").Or(str "-").Option()
         let digit_num = ParserCombinator.char isdigit
         let digit_usint = digit_num.Many1().AndL((str "#").Many()).Select(fun x -> List.fold (fun s x -> s + x.ToString()) "" x |> int)
         let digit_decimals = 
            let d1 = (str ".").AndR(digit_num).AndL((str "#").Many()).Select(fun d -> ("0."+d.ToString()) |> double |> RealV )
            let d2 = (digit_num.Many1()).AndL(str ".").And(digit_num).AndL((str "#").Many()).Select(fun (i,d) -> (i.ToString()+"."+d.ToString()) |> double |> RealV )
            let d3 = (digit_num.Many1()).AndL((str "#").Many1()).AndL(str ".").AndL((str "#").Many()).Select(fun i -> i.ToString() |> double |> RealV )
            let d4 = (digit_num.Many1()).Select(fun i -> i.ToString() |> int |> IntV )
            in  d1.Or(d2).Or(d3).Or(d4) 
         let digit_usreal = 
            let faction = digit_usint.AndL(whitespace.And(str "/").And(whitespace)).And(digit_usint).Select(fun v -> FractionV v)
            in  faction.Or(digit_decimals).Or(digit_usint.Select(IntV))
         let digit_real = sign.And(digit_usreal).Select(fun (s,v) -> match s with Some("+") -> v | Some("-") -> NumberVOp.neg v | _ -> v)
         let digit_complex = 
            // digit_real.AndL(str "@").AndR(digit_real) <- ?
            let p1 = digit_real.AndL(str "+i").Select(fun real -> ComplexV (real, RealV 1.0))
            let p2 = digit_real.AndL(str "+" ).And(digit_usreal).AndL(str "i").Select(fun (real,imaginary) -> ComplexV (real, imaginary))
            let p3 = digit_real.AndL(str "-i").Select(fun real -> ComplexV (real, RealV -1.0))
            let p4 = digit_real.AndL(str "-" ).And(digit_usreal).AndL(str "i").Select(fun (real,imaginary) -> ComplexV (real, NumberVOp.neg imaginary))
            in p1.Or(p2).Or(p3).Or(p4)
         in accuracy_prefix.And(digit_complex).Select(fun (a,v) -> Number v )
    and  ident = 
         let normal = 
            let head = ParserCombinator.char ishead
            let tail = ParserCombinator.char istail
            in head.And(tail.Many()).Select(fun (x,xs) -> List.fold (fun s x -> s + x.ToString()) (x.ToString()) xs)
         let plus  = str "+"
         let minus = str "-"
         let dots  = str "..."
         in whitespace.AndR(normal.Or(plus).Or(minus).Or(dots)).Select(Symbol)
    and atom = whitespace.AndR(string.Or(char).Or(bool).Or(number).Or(ident))
    and quoted = 
        let quote = (str "'").AndR(expr).Select(fun x -> list [Symbol "quote"; x])
        let quasiquote = (str "`").AndR(expr).Select(fun x -> list [Symbol "quasiquote"; x])
        let unquote_splicing = (str ".@").AndR(expr).Select(fun x -> list [Symbol "unquote-splicing"; x])
        let unquote = (str ",").AndR(expr).Select(fun x -> list [Symbol "unquote"; x])
        in whitespace.AndR(quote.Or(quasiquote).Or(unquote_splicing).Or(unquote))
    and list_ = 
        let h    = (str "(").And(whitespace)
        let body = 
            let a = expr.Many1()
            let d = whitespace.AndR(str ".").AndR(expr).Option().Select(fun x -> match x with Some v -> v | None -> Nil)
            in a.And(d).Select( fun (a,d) -> List.foldBack (fun x s -> Cell(x,s)) a d)
        let t    = whitespace.And(str ")")
        in whitespace.And(h).AndR(body.Option()).AndL(t).Select(fun x -> match x with Some v -> v | None -> Nil)
    and vector = 
        let h    = (str "#(").And(whitespace)
        let body = expr.Many1()
        let t    = whitespace.And(str ")")
        in whitespace.And(h).AndR(body.Option()).AndL(t).Select(fun x -> match x with Some v -> Array v | None -> Nil)


let run (context:VM.Context) :VM.Context =
    let rec loop context =
        let context = VM.vm context
        in  if context.halt then context else loop context
    in loop context

let create_context () : VM.Context =
    { s=[]; e=[]; c=[Stop]; d=[]; halt = true }

let update_context(context: VM.Context) (expr:Inst list) : VM.Context =
    { context with c=expr; halt = false }

[<EntryPoint>]
let main argv =
    let test (s:string) (expr:Value) (ret:Value) (ctx:VM.Context): VM.Context = 
        printfn "Test:";
        printfn "  Input Code: %s" s;
        match Parser.start s 0 with
        | None -> failwith "syntax error"
        | Some {value=v} -> 
            printfn "  Parsed   AST: %A" v;
            printfn "  Expected AST: %A" expr;
            if (equal v expr) = false 
            then (printfn "  -> AST Not Match"); ctx
            else
                let code = v |> Compiler.compile
                let ctx' = update_context ctx code
                let result = run ctx'
                in  
                    result.s.[0] |> toString |> printfn "  Execute  Result: %s";
                    ret          |> toString |> printfn "  Expected Result: %s";
                    (if equal ret result.s.[0] then printfn "  All Match" else printfn "  -> Result Not Match");
                    ctx'
    
    in
        create_context () |>
        test "(quote a)" (list [Symbol("quote"); Symbol("a")]) (Symbol "a")|>
        test "(if #t 'a 'b)" (list [Symbol("if"); Boolean  true; list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]]) (Symbol "a")|>
        test "(if #f 'a 'b)" (list [Symbol("if"); Boolean false; list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]]) (Symbol "b")|>

        test "(car '(a b c))" (list [Symbol("car"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]]) (Symbol "a")|>
        test "(cdr '(a b c))" (list [Symbol("cdr"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]]) (list[Symbol("b");Symbol("c")])|>
        test "(cons 'a 'b)" (list [Symbol("cons"); list [Symbol("quote");Symbol("a")]; list [Symbol("quote");Symbol("b")]]) (Cell (Symbol "a", Symbol "b"))|>
        test "(eq? 'a 'a)" (list [Symbol("eq?"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("a")]]) (Boolean true)|>
        test "(eq? 'a 'b)" (list [Symbol("eq?"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]]) (Boolean false)|>
        test "(pair? '(a b c))" (list [Symbol("pair?"); list [Symbol("quote"); list [Symbol("a"); Symbol("b"); Symbol("c")]]]) (Boolean true)|>
        test "(pair? 'a)" (list [Symbol("pair?"); list [Symbol("quote"); Symbol("a")]]) (Boolean false)|>

        test "(define a 'b)" (list [Symbol("define");Symbol("a");list[Symbol("quote");Symbol("b")]]) (Symbol "a") |>
        test "a" (Symbol "a") (Symbol "b")|>
        test "(lambda (x) x)" (list [Symbol "lambda"; list [Symbol "x"]; Symbol "x"]) (Closure ([Ld (0, 0); Rtn], [])) |>
        test "((lambda (x) x) 'a)" (list [list [Symbol "lambda"; list [Symbol "x"]; Symbol "x"]; list[Symbol "quote"; Symbol "a"]]) (Symbol "a") |>
        test "(define list (lambda x x))" (list [Symbol "define"; Symbol "list"; list [Symbol "lambda"; Symbol "x"; Symbol "x"]]) (Symbol "list") |>
        test "(list 'a 'b 'c 'd 'e)" (list [Symbol "list"; list [Symbol "quote";Symbol "a"]; list [Symbol "quote";Symbol "b"]; list [Symbol "quote";Symbol "c"]; list [Symbol "quote";Symbol "d"]; list [Symbol "quote";Symbol "e"]]) (list [Symbol "a";Symbol "b";Symbol "c";Symbol "d";Symbol "e"]) |> 
        
        test "(define x 'a)" (list [Symbol("define"); Symbol("x"); list [Symbol("quote"); Symbol("a")]]) (Symbol "x")|>
        test "x" (Symbol "x") (Symbol "a")|>
        test "(define foo (lambda () x))" (list [Symbol("define"); Symbol("foo"); list [Symbol("lambda"); Nil; Symbol("x") ]]) (Symbol "foo")|>
        test "(foo)" (list [Symbol("foo")]) (Symbol "a")|>
        test "(define bar (lambda (x) (foo)))"  (list [Symbol("define"); Symbol("bar"); list [Symbol("lambda"); list[Symbol("x")]; list[Symbol("foo")] ]]) (Symbol "bar")|>
        test "(bar 'b)" (list [Symbol("bar"); list [Symbol("quote"); Symbol("b")]]) (Symbol "a")|>

        test "foo" (Symbol("foo")) (Closure ([Ldg (Symbol "x"); Rtn], [])) |>
        test "bar" (Symbol("bar")) (Closure ([Args 0; Ldg (Symbol "foo"); App; Rtn], [] )) |>

        ignore;
        
        0

