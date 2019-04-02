open System
open System.Linq.Expressions
open System.Text.RegularExpressions;
open Microsoft.FSharp.Core;

(* Value Type *)

type NumberV =
      IntV      of value : int
    | RealV     of value : double
    //| ComplexV  of real:NumberV * imaginary:NumberV
    //| FractionV of numerator:int * denominator:int

let rec valueToString v =
    match v with
    //| ComplexV (r,i) -> (valueToString r) + "+" + (valueToString i) + "i" 
    //| FractionV (n,d) -> n.ToString() + "/" + d.ToString()
    | IntV v -> v.ToString()
    | RealV v -> v.ToString()

module NumberVOp =
    //let toComplex v =
    //    match v with
    //    | ComplexV _ -> v
    //    | FractionV _ -> ComplexV (v, IntV 0)
    //    | IntV _ -> ComplexV (v, IntV 0)
    //    | RealV _  -> ComplexV (v, IntV 0)

    //let toFraction v =
    //    match v with
    //    | ComplexV _ -> failwith "cannt convert"
    //    | FractionV _ -> v
    //    | IntV v -> FractionV (v, 1)
    //    | RealV v  -> FractionV (int (v * 10000.0), 10000)

    let toInt v =
        match v with
        //| ComplexV _ -> failwith "cannt convert"
        //| FractionV _ -> failwith "cannt convert"
        | IntV _ -> v
        | RealV _  -> failwith "cannt convert"

    let toReal v =
        match v with
        //| ComplexV _ -> failwith "cannt convert"
        //| FractionV _ -> v
        | IntV v -> RealV ( float v)
        | RealV _  -> v

    let rec add lhs rhs =
        match (lhs, rhs) with
        //| ComplexV (r1,i1), ComplexV (r2,i2)  -> ComplexV (add r1 r2, add i1 i2)
        //| ComplexV _, _  -> add lhs (toComplex rhs)
        //| _, ComplexV  _  -> add (toComplex lhs) rhs

        //| FractionV (n1,d1), FractionV (n2,d2) -> FractionV (n1*d2+n2*d1,d1*d2)
        //| FractionV _,_ -> add lhs (toFraction rhs)
        //| _, FractionV _ -> add (toFraction lhs) rhs

        | RealV v1, RealV v2 -> RealV (v1 + v2)
        | RealV  _,       _  -> add lhs (toReal rhs)
        |        _, RealV _  -> add (toReal lhs) rhs

        | IntV v1, IntV v2 -> IntV (v1 + v2)
        | IntV _ ,_        -> add lhs (toInt rhs)
        |       _, IntV _  -> add (toInt lhs) rhs

        | _ -> failwith "cannot add"

    let neg v =
        match v with
        //| ComplexV _ -> failwith "cannot neg" 
        //| FractionV (n,d) -> FractionV (-n,d)
        | IntV v -> IntV (-v)
        | RealV v -> RealV (-v)

type Value  = 
      Symbol of value : string
    | Nil
    | Array of value : Value list
    | Cell of car: (Value ref) * cdr: (Value ref)
    | String of value : string
    | Char of value : char
    | Number of value : NumberV
    | Boolean of value : bool
    | Primitive of value : (Value -> Value)
    | Closure of body:Inst list * env:Value list
    | Macro of value : Value // append to implements macro
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
    | Defm of Value // append to implements macro
    | Args of int
    | Lset of pos:(int * int) // append to implements set!
    | Gset of sym:Value // append to implements set!
    | Stop

let nil    = Nil
let true'  = Boolean true
let false' = Boolean false

let gemSym =
    let dic = ref Map.empty
    in fun str -> 
        match Map.tryFind str !dic with
        | Some x -> x
        | None   -> let v = Symbol str
                    in dic := (!dic).Add(str, v); v;

let toBool value = 
    if value then true' else false'

let (|Cons|_|) value =
    match value with
    | Cell (x,y) -> Some(!x, !y)
    | _ -> None

let set_car (c : Value) (v : Value) =
    match c with
    | Cell (x, y) -> x := v
    | _ -> failwith "cannot change"

let set_cdr (c : Value) (v : Value) =
    match c with
    | Cell (x, y) -> y := v
    | _ -> failwith "cannot change"


let toString (value:Value) = 
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
    let rec loop (value:Value) (isCdr : bool) =
        match value with 
        | Symbol value -> value
        | Nil -> "()"
        | Array v -> "#(" + String.Join(", ", (List.map (fun x -> loop x false) v)) + ")"
        | Cons (car, Nil) -> 
            match isCdr with
            | false -> "(" + (loop car false) + ")"
            | true  ->       (loop car false)
        | Cons (car, (Cons _ as cdr)) -> 
            match isCdr with
            | false -> "(" + (loop car false) + " " + (loop cdr true) + ")"
            | true  ->       (loop car false) + " " + (loop cdr true)
        | Cons (car,  cdr) -> 
            match isCdr with
            | false -> "(" + (loop car false) + " . " + (loop cdr true) + ")"
            | true  ->       (loop car false) + " . " + (loop cdr true)
        | Char value -> 
            match value with
            | '\n' -> "#\\newline"
            | ' ' -> "#\\space"
            | _ -> "#\\" + value.ToString()
        | String value -> escapeString value
        | Number value -> valueToString value
        | Boolean false -> "#f"
        | Boolean true  -> "#t"
        | Primitive value -> sprintf "<Primitive: %s>" (value.ToString ())
        | Closure (value, env) -> sprintf "<Closure: %A>" (value)
        | Macro (value) -> sprintf "<Macro: %s>" (value.ToString ())
        | Error (subtype, message) -> sprintf "<%s: %s>" subtype message
    in loop value false

(* Scheme's primitive functions *)

let car (x: Value): Value =
    match x with
    | Cons (car, cdr) -> car
    | _               -> Error ("runtime error", (sprintf "Attempt to apply car on %s" (toString x)))

let cdr (x: Value): Value =
    match x with
    | Cons (car, cdr) -> cdr
    | _               -> Error ("runtime error", (sprintf "Attempt to apply cdr on %s" (toString x)))

let cadr = car << cdr

let issymbol x = 
    match x with
    | Symbol _ -> true
    | _        -> false

let ispair x = 
    match x with
    | Cons _ -> true
    | _      -> false

let iserror x = 
    match x with
    | Error _ -> true
    | _       -> false

let list (values : Value list) : Value =
    List.foldBack (fun x y -> Cell (ref x,ref y)) values nil

let list_length (x:Value) : int = 
    let rec loop (x:Value) (n:int) =
        match x with
        | Cons (a, d) -> loop d (n + 1)
        | Nil -> n
        | _ -> failwith "not pair"
    in  loop x 0

let list_reduce (f : Value -> 'a -> 'a) (ridentity:'a) (list:Value) =
    let rec loop (ridentity:'a) (list:Value) =
        match list with
        | Nil                 -> ridentity
        | Cons (x, xs) -> loop (f x ridentity) xs
        | _ -> failwith "bad argument"
    in  loop ridentity list

let list_fold (f : Value -> Value -> Value) (list:Value) : Value =
    match list with
        | Cons (x, xs) -> list_reduce f x xs
        | _            -> failwith "bad argument"

let rec ceq (x:Value) (y:Value) : bool =
    match (x,y) with
    | (Symbol         v1, Symbol         v2) -> (v1 = v2)
    | (Cons      (a1,d1), Cons      (a2,d2)) -> (ceq a1 a2) && (ceq d1 d2)
    | (Array          v1, Array          v2) -> (List.forall2 ceq v1 v2)
    | (Number         v1, Number         v2) -> (v1 = v2)
    | (Char           v1, Char           v2) -> (v1 = v2)
    | (String         v1, String         v2) -> (v1 = v2)
    | (Boolean        v1, Boolean        v2) -> (v1 = v2)
    | (Primitive      v1, Primitive      v2) -> (Object.Equals(v1,v2))
    | (Closure   (i1,e1), Closure   (i2,e2)) -> (List.forall2 ieq i1 i2) && (List.forall2 ceq e1 e2)
    | (Macro          i1, Macro          i2) -> (ceq i1 i2)
    | (Nil              , Nil              ) -> true
    | (                _,                 _) -> false

and ieq (x:Inst) (y:Inst) : bool =
    match (x,y) with
    | (Ld   (i1,j1), Ld   (i2,j2)) -> (i1=i2) && (j1=j2)
    | (Ldc     (v1), Ldc     (v2)) -> ceq v1 v2
    | (Ldg     (v1), Ldg     (v2)) -> ceq v1 v2
    | (Ldf     (i1), Ldf     (i2)) -> List.forall2 ieq i1 i2
    | (Join        , Join        ) -> true
    | (Rtn         , Rtn         ) -> true
    | (App         , App         ) -> true
    | (Pop         , Pop         ) -> true
    | (Sel  (t1,f1), Sel  (t2,f2)) -> (List.forall2 ieq t1 t2) && (List.forall2 ieq f1 f2)
    | (Def       v1, Def       v2) -> ceq v1 v2 // append to implements macro
    | (Defm      v1, Defm      v2) -> ceq v1 v2
    | (Args      i1, Args      i2) -> (i1=i2) 
    | (Stop        , Stop        ) -> true
    | (           _,            _) -> false

let eq (x:Value) (y:Value) : bool =
    Object.ReferenceEquals(x,y)

let rec eqv (x:Value) (y:Value) : bool =
    match (x,y) with
    | (Number        v1, Number        v2) -> (v1 = v2)
    | (Char          v1, Char          v2) -> (v1 = v2)
    | (Boolean       v1, Boolean       v2) -> (v1 = v2)
    | _ -> eq x y

let rec equal (x:Value) (y:Value) : bool =
    match (x,y) with
    | (Cons     (a1,d1), Cons     (a2,d2)) -> (equal a1 a2) && (equal d1 d2)
    | (Array         v1, Array         v2) -> (List.forall2 equal v1 v2)
    | (String        v1, String        v2) -> (v1 = v2)
    | _ -> eqv x y


module Scheme =
    type EnvEntry = Value * (Value ref)
    type Env = EnvEntry list

    let global_environment: Env ref =
        ref [
                (gemSym "#t",   ref (true')) ;
                (gemSym "#f",    ref (false'));

                (gemSym "car",   ref (Primitive (fun xs -> car (car xs))));
                (gemSym "cdr",   ref (Primitive (fun xs -> cdr (car xs))));
                (gemSym "cons",  ref (Primitive (fun xs -> Cell (ref (car xs), ref (cadr xs)))));
                (gemSym "eq?",   ref (Primitive (fun xs -> toBool (eq  (car xs) (cadr xs)))));
                (gemSym "eqv?",  ref (Primitive (fun xs -> toBool (eqv (car xs) (cadr xs)))));
                (gemSym "pair?", ref (Primitive (fun xs -> toBool (ispair (car xs)))));
                (gemSym "+",     ref (Primitive (list_fold (fun x s -> match s,x with | Number l, Number r -> Number (NumberVOp.add l r) | _ -> failwith "bad argument" ))));
            ]

    let assoc (sym: Value) (dic: Env ref) : EnvEntry option =
        let rec loop (dic: (Value*(Value ref)) list) =
            match dic with
            | [] -> None
            | ((key, value) as entry) :: d -> if (eq key sym) then Some entry else loop d
        in loop !dic        
        
    let get_gvar (sym:Value) : Value =
        match assoc sym global_environment with
        | Some (key, value) -> !value
        | _ -> Error ("runtime error", String.Format("unbound variable:{0}",toString sym))

    type Context = { s: Value list; e: Value list; c: (Inst list); d: (Value list * Value list * (Inst list)) list; halt: bool }

    
    let rec compile (expr:Value) : Inst list =
        (* compiler *)
        let position_var (sym:Value) (ls:Value) : int option =
            let rec loop (ls:Value) (i:int) =
                match ls with
                | Nil            -> None
                | Symbol(_) as y -> if (eq sym y) then Some (-(i + 1)) else None
                | Cons(a, d)     -> if (eq sym a) then Some i else loop d (i + 1)
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

        (*  start: append to implements macro *)
        let ismacro (sym:Value) : bool =
            match assoc sym global_environment with
            | Some (key, { contents=Macro _ }) -> true
            | _ -> false

        let get_macro_code (sym:Value) : Inst list =
            match get_gvar sym with
            | Macro (Closure (v,e)) -> v
            | _ -> failwith "not macro"

        (*  end: append to implements macro *)

        let rec comp (expr: Value) (env: Value list) (code: Inst list): Inst list =
            if is_self_evaluation expr 
            then Ldc(expr) :: code
            else 
                match expr with
                | Symbol _ ->
                    match location expr env with
                    | Some (i, j) -> Ld(i, j)  :: code
                    | None        -> Ldg(expr) :: code
                | Cons(Symbol("quote"), Cons(v, Nil)) -> 
                    Ldc(v) :: code
                | Cons(Symbol("if"), Cons(cond, Cons(t, Nil))) -> 
                    let t_clause = comp t env [Join]
                    let f_clause = [Ldc (gemSym "*undef"); Join] 
                    in  comp cond env (Sel (t_clause, f_clause) :: code)
                | Cons(Symbol("if"), Cons(cond, Cons(t, Cons(e, Nil)))) -> 
                    let t_clause = comp t env [Join]
                    let f_clause = comp e env [Join]
                    in  comp cond env (Sel (t_clause, f_clause) :: code)
                | Cons((Symbol "lambda"), Cons(name, body)) ->
                    let body = comp_body body (name :: env) [Rtn]
                    in  Ldf(body) :: code
                | Cons((Symbol "define"), Cons((Symbol _) as sym, Cons(body, Nil))) -> 
                    comp body env (Def(sym)::code)
                (*  start: append to implements macro *)
                | Cons((Symbol "define-macro"), Cons((Symbol _) as sym, Cons(body, Nil))) -> 
                    comp body env (Defm(sym)::code)
                (*  end: append to implements macro *)
                (*  start: append to implements set! *)
                | Cons((Symbol "set!"), Cons(sym,Cons(body, _))) -> 
                    let pos = location sym env
                    match pos with
                    | Some pos -> comp body env (Lset (pos)::code)
                    | None -> comp body env (Gset (sym)::code)
                (*  end: append to implements set! *)
                (*  start: append to implements macro *)
                | Cons(fn, args) when ismacro fn -> 
                    let context = { s= []; e= [args] ; c= get_macro_code fn; d= [([], [], [Stop])]; halt= false }
                    let newexpr = run context
                    in  printfn "macro(%s) => %s" (toString fn) (toString newexpr.s.Head); comp newexpr.s.Head env code
                (*  end: append to implements macro *)
                | Cons(fn, args) -> 
                    complis args env ((Args (list_length args))::(comp fn env (App :: code)))
                | _ -> failwith "syntax error"
            
        and comp_body (body: Value) (env: Value list) (code: Inst list) : Inst list =
            match body with
            | Cons(x, Nil) -> comp x env code
            | Cons(x, xs)  -> comp x env (Pop :: (comp_body xs env code))
            | _            -> failwith "syntax error"
        and complis (expr: Value) (env: Value list) (code: Inst list): Inst list =
            match (expr) with 
            | Nil       -> code 
            | Cons(a,d) -> comp a env (complis d env code)
            | _         -> failwith "syntax error"
        in
            if iserror(expr)
            then [Ldc expr; Stop]
            else comp expr [] [Stop]


    (* secd virtual machine *)
    and vm (context:Context) : Context =
        let rec drop (ls: Value) (n: int): Value =
            if n <= 0 
            then ls
            else 
                match ls with
                | Cons (a, d) -> drop d (n - 1)
                | _ -> failwith "not cell"
        let rec list_ref (x:Value) (n:int) : Value = 
            match x with
                | Nil -> failwith "out of range"
                | Cons(a,d) -> 
                    if n < 0 then failwith "out of range"
                    else if n = 0 then a
                    else list_ref d (n - 1)
                | _  -> failwith "not cell"

        let get_lvar (e: Value list) (i: int) (j: int) : Value =
            if 0 <= j
            then list_ref (List.item i e) j
            else drop     (List.item i e) -(j + 1)

        let set_lvar (e: Value list) (i: int) (j: int) (value:Value) =
            if 0 <= j
            then set_car (drop (List.item i e) j) value
            else if j = -1
              then set_car (List.item i e) value
              else set_cdr (drop (List.item i e) (- (j + 2))) value

        let set_gvar (sym:Value) (value:Value) =
            match assoc sym global_environment with
            | Some (car,cdr) -> cdr := value
            | None -> failwith "unbound variable: " sym


        in
            if context.halt 
            then context
            else 
                match (context.c, context.s, context.e, context.d) with
                | (Ld(i,j)  ::c',                         s , e,                 d ) -> { context with s = (get_lvar e i j) :: s ; e = e        ; c = c'; d = d }
                | (Ldc(v)   ::c',                         s , e,                 d ) -> { context with s = v :: s                ; e = e        ; c = c'; d = d }
                | (Ldg(v)   ::c',                         s , e,                 d ) -> { context with s = (get_gvar v) :: s     ; e = e        ; c = c'; d = d }
                | (Ldf(v)   ::c',                         s , e,                 d ) -> { context with s = Closure (v, e) :: s   ; e = e        ; c = c'; d = d }
                | (App      ::c', Primitive(f)  :: arg :: s', e,                 d ) -> { context with s = (f arg) :: s'         ; e = e        ; c = c'; d = d }
                | (App      ::c', Closure(f,e') :: arg :: s', e,                 d ) -> { context with s = []                    ; e = arg :: e'; c = f ; d = (s', e, c') :: d }
                | (Rtn      ::_ ,                   s1 :: [], e, (s2, e', c') :: d') -> { context with s = s1 :: s2              ; e = e'       ; c = c'; d = d' }
                | (Sel(_, f)::c',      (Boolean false) :: s', e,                 d ) -> { context with s = s'                    ; e = e        ; c = f ; d = ([],[],c') :: d }
                | (Sel(t, _)::c',                    _ :: s', e,                 d ) -> { context with s = s'                    ; e = e        ; c = t ; d = ([],[],c') :: d }
                | (Join     ::_ ,                         s , e,   (_, _, c') :: d') -> { context with s = s                     ; e = e        ; c = c'; d = d' }
                | (Pop      ::c',                    _ :: s', e,                 d ) -> { context with s = s'                    ; e = e        ; c = c'; d = d }
                | (Args(v)  ::c',                         s , e,                 d ) -> let (a'',s') = List.splitAt v s
                                                                                        let a' = List.fold (fun s x -> Cell(ref x,ref s))  nil a''
                                                                                        in { context with s = a' :: s'; e = e; c = c'; d = d }
                | (Def(sym) ::c',                 body :: s', e,                 d ) -> global_environment := (sym, ref body) :: !global_environment;
                                                                                        { context with s = sym :: s'; e = e; c = c'; d = d };
                (*  start: append to implements macro *)
                | (Defm(sym)::c',   (Closure _ as body):: s', e,                 d ) -> global_environment := (sym, ref (Macro body)) :: !global_environment;
                                                                                        { context with s = sym :: s'; e = e; c = c'; d = d };
                (*  end: append to implements macro *)
                (*  start: append to implements set! *)
                | (Lset(i,j)::c',                value :: s', e,                 d ) -> set_lvar e i j value; 
                                                                                        { context with s = context.s; e = e; c = c'; d = d };
                | (Gset(sym)::c',                value :: s', e,                 d ) -> set_gvar sym value;
                                                                                        { context with s = context.s; e = e; c = c'; d = d };
                (*  end: append to implements set! *)
                | (Stop     ::c',                          s, e,                 d ) -> { context with halt= true }
                | _                                                                  -> failwith "bad context"
    and run (context:Context) :Context =
        let rec loop context =
            let context = vm context
            in  if context.halt then context else loop context
        in loop context

    let create_context () : Context =
        { s=[]; e=[]; c=[Stop]; d=[]; halt = true }

    let update_context(context: Context) (expr:Inst list) : Context =
        { context with c=expr; halt = false }


(* parser combinator *)
module ParserCombinator =
    type ParserState<'a> = Success of pos:int * value:'a
                         | Fail    of pos:int * message:string
    type Parser<'a> = string -> int -> ParserState<'a>

    let char (pred : char -> bool) = 
        fun (str:string) (pos:int) -> 
            if (str.Length > pos) && (pred str.[pos]) 
            then Success (pos+1, str.[pos]) 
            else Fail    (pos, "char: not match character "+(if (str.Length > pos) then str.[pos].ToString() else "EOS"))

    let anychar (chs:string) = 
        fun (str:string) (pos:int) -> 
            if (str.Length > pos) && (chs.IndexOf(str.[pos]) <> -1 )
            then Success (pos+1, str.[pos]) 
            else Fail    (pos, "anychar: not match character  "+(if (str.Length > pos) then str.[pos].ToString() else "EOS"))

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
            then Success (pos+s.Length, s) 
            else Fail    (pos, "str: require is '"+s+"' but get"+(if (str.Length > pos) then str.[pos].ToString() else "EOS")+".")

    let any () = 
        fun (str:string) (pos:int) -> 
            if str.Length > pos 
            then Success (pos+1, str.[pos]) 
            else Fail    (pos, "any: require anychar but get EOF.")

    let not (parser:Parser<'a>) = 
        fun (str:string) (pos:int) -> 
            match parser str pos with
            | Fail    _ -> Success (pos, ())
            | Success _ -> Fail    (pos, "not: require rule was fail but success.")

    let select (pred:'a->'b) (parser:Parser<'a>) = 
        fun (str:string) (pos:int) -> 
            match parser str pos  with
            | Fail    (pos, value) -> Fail (pos, value)
            | Success (pos, value) -> Success (pos, (pred value))

    let where (pred:'a->bool) (parser:Parser<'a>) = 
        fun (str:string) (pos:int) -> 
            match parser str pos with
            | Fail    (pos, value) -> Fail (pos, value)
            | Success (_, value) as f -> if pred value then f else Fail (pos, "where: require rule was fail but success.")

    let opt (parser:Parser<'a>) = 
        fun (str:string) (pos:int) ->       
            match parser str pos with
            | Fail _               -> Success (pos, None)
            | Success (pos, value) -> Success (pos, Some(value))

    let seq (parsers:Parser<'a> list) =
        fun (str:string) (pos:int) -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (values: 'a list) =
                match parsers with
                | []   -> Success (pos, List.rev values)
                | x::xs -> 
                    match x str pos with
                    | Fail    (pos, value) -> Fail (pos, value)
                    | Success (pos, value) -> loop xs pos (value :: values)
            in loop parsers pos [];
    
    let choice(parsers:Parser<'a> list) =
        fun (str:string) (pos:int) -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) =
                match parsers with
                | []   -> Fail (pos, "choice: not match any rules.")
                | x::xs -> 
                    match x str pos with
                    | Fail _  -> loop xs pos
                    | Success _ as ret -> ret;
            in loop parsers pos;

    let repeat (parser:Parser<'a>) = 
        fun (str:string) (pos : int) -> 
            let rec loop pos values = 
                match parser str pos with
                | Fail _  -> Success (pos, List.rev values)
                | Success (pos, value) -> loop pos (value :: values)
            in loop pos []

    let repeat1 (parser:Parser<'a>) = 
        fun (str:string) (pos : int) -> 
            let rec loop pos values = 
                match parser str pos with
                | Fail _  -> Success (pos, List.rev values)
                | Success (pos, value) -> loop pos (value :: values)
            in 
                match parser str pos with
                | Fail    (pos, value) -> Fail (pos, value)
                | Success (pos, value) -> loop pos [value]


    let andBoth (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) -> 
            match lhs str pos with
            | Fail    (pos, value) -> Fail (pos, value)
            | Success (pos, value1) -> 
                match rhs str pos with
                | Fail    (pos, value) -> Fail (pos, value)
                | Success (pos, value2) -> 
                    Success (pos, (value1, value2)) 

    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) -> 
            match lhs str pos with
            | Fail    (pos, value) -> Fail (pos, value)
            | Success (pos, value1) -> 
                match rhs str pos with
                | Fail    (pos, value) -> Fail (pos, value)
                | Success (pos, value2) -> 
                     Success (pos, value2) 

    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) -> 
            match lhs str pos with
            | Fail    (pos, value) -> Fail (pos, value)
            | Success (pos, value1) -> 
                match rhs str pos with
                | Fail    (pos, value) -> Fail (pos, value)
                | Success (pos, value2) -> 
                     Success (pos, value1) 

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

    let rec whitespace = 
        let space = (ParserCombinator.char isspace).Select(fun _ -> " ")
        let comment = (ParserCombinator.char (fun x -> x = ';')).AndR((ParserCombinator.char (fun x -> x <> '\n')).Many()).Select(fun _ -> " ");
        in space.Or(comment).Many()
    
    and eof = not(any())

    and start = whitespace.AndR(eof.Select(fun _ -> None).Or(expr.Select(Some).AndL(whitespace)))

    and expr = lazy_(fun () -> whitespace.AndR(quoted.Or(list_).Or(vector).Or(atom)))
    
    and char = 
        let space     = (str "#\\space").Select(fun _ -> ' ')
        let newline   = (str "#\\newline").Select(fun _ -> '\n')
        let character = (str "#\\").AndR(any())
        in  whitespace.AndR(space.Or(newline).Or(character)).Select(Char)

    and bool = 
        let trueV  = (str "#t").Select(fun _ -> true)
        let falseV = (str "#f").Select(fun _ -> false)
        in  whitespace.AndR(trueV.Or(falseV)).Select(toBool)

    and string = 
        let ch = 
            let dquote    = (str "\\\"").Select(fun _ -> '"')
            let cr        = (str "\\r" ).Select(fun _ -> '\r')
            let lf        = (str "\\n" ).Select(fun _ -> '\n')
            let tab       = (str "\\t" ).Select(fun _ -> '\t')
            let formfeed  = (str "\\f" ).Select(fun _ -> '\f')
            let notescape = ParserCombinator.char (fun x -> x <> '\\' && x <> '"')
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
            let d4 = (digit_num.Many1()).Select(fun i -> (List.fold (fun s x -> s + x.ToString()) "" i) |> int |> IntV )
            in  d1.Or(d2).Or(d3).Or(d4) 
         let digit_usreal = 
            //let faction = digit_usint.AndL(whitespace.And(str "/").And(whitespace)).And(digit_usint).Select(fun v -> FractionV v)
            //in  faction.Or(digit_decimals).Or(digit_usint.Select(IntV))
            digit_decimals.Or(digit_usint.Select(IntV))
         let digit_real = sign.And(digit_usreal).Select(fun (s,v) -> match s with Some("+") -> v | Some("-") -> NumberVOp.neg v | _ -> v)
         //let digit_complex = 
         //   // digit_real.AndL(str "@").AndR(digit_real) <- ?
         //   let p1 = digit_real.AndL(str "+i").Select(fun real -> ComplexV (real, RealV 1.0))
         //   let p2 = digit_real.AndL(str "+" ).And(digit_usreal).AndL(str "i").Select(fun (real,imaginary) -> ComplexV (real, imaginary))
         //   let p3 = digit_real.AndL(str "-i").Select(fun real -> ComplexV (real, RealV -1.0))
         //   let p4 = digit_real.AndL(str "-" ).And(digit_usreal).AndL(str "i").Select(fun (real,imaginary) -> ComplexV (real, NumberVOp.neg imaginary))
         //   in p1.Or(p2).Or(p3).Or(p4)
         //in prefix.And(digit_complex.Or(digit_real)).Select(fun (a,v) -> Number v )
         in prefix.And(digit_real).Select(fun (a,v) -> Number v )
    and  ident = 
         let normal = 
            let head = ParserCombinator.char ishead
            let tail = ParserCombinator.char istail
            in head.And(tail.Many()).Select(fun (x,xs) -> List.fold (fun s x -> s + x.ToString()) (x.ToString()) xs)
         let plus  = str "+"
         let minus = str "-"
         let dots  = str "..."
         in whitespace.AndR(normal.Or(plus).Or(minus).Or(dots)).Select(gemSym)
    and atom = whitespace.AndR(string.Or(char).Or(bool).Or(number).Or(ident))
    and quoted = 
        let quote = (str "'").AndR(expr).Select(fun x -> list [gemSym "quote"; x])
        let quasiquote = (str "`").AndR(expr).Select(fun x -> list [gemSym "quasiquote"; x])
        let unquote_splicing = (str ",@").AndR(expr).Select(fun x -> list [gemSym "unquote-splicing"; x])
        let unquote = (str ",").AndR(expr).Select(fun x -> list [gemSym "unquote"; x])
        in whitespace.AndR(quote.Or(quasiquote).Or(unquote_splicing).Or(unquote))
    and list_ = 
        let h    = (str "(").And(whitespace)
        let body = 
            let a = expr.Many1()
            let d = whitespace.AndR(str ".").AndR(expr).Option().Select(fun x -> match x with Some v -> v | None -> nil)
            in a.And(d).Select( fun (a,d) -> List.foldBack (fun x s -> Cell(ref x,ref s)) a d)
        let t    = whitespace.And(str ")")
        in whitespace.And(h).AndR(body.Option()).AndL(t).Select(fun x -> match x with Some v -> v | None -> nil)
    and vector = 
        let h    = (str "#(").And(whitespace)
        let body = expr.Many1()
        let t    = whitespace.And(str ")")
        in whitespace.And(h).AndR(body.Option()).AndL(t).Select(fun x -> match x with Some v -> Array v | None -> nil)
    let test () =
        let check (p:Parser<'a>) (s:string) (ret:ParserState<'a>) (comp:('a -> 'a -> bool)) =
            let result = 
                match (p s 0), ret with
                | Fail (p1, _), Fail (p2, _) -> p1 = p2
                | Success (p1, v1), Success  (p2, v2) -> p1 = p2 && comp v1 v2
                | _ -> false
            in  (if result then "." else "!") |> printfn "%s"
        in  
            check char "#\\space" (Success(7, Char ' ')) ceq;
            check char "#\\newline" (Success(9, Char '\n')) ceq;
            check char "#\\a" (Success(3, Char 'a')) ceq;
            check char "#\\&" (Success(3, Char '&')) ceq;
            check char "#\\1" (Success(3, Char '1')) ceq;

            check bool "#t" (Success(2, true')) ceq;
            check bool "#f" (Success(2, false')) ceq;

            check string "\"\"" (Success(2, String "")) ceq;
            check string "\"abc\"" (Success(5, String "abc")) ceq;
            check string "\"xy\\rz\"" (Success(7, String "xy\rz")) ceq;
            check string "\"ab\\ncd\"" (Success(8, String "ab\ncd")) ceq;
            check string "\"\\t123\"" (Success(7, String "\t123")) ceq;
            check string "\"\\f\\f\"" (Success(6, String "\f\f")) ceq;
            check string "\"1\\\"23\"" (Success(7, String "1\"23")) ceq;

            check quoted "'a" (Success(2, list [gemSym "quote"; gemSym "a"])) ceq;
            check quoted "`a" (Success(2, list [gemSym "quasiquote"; gemSym "a"])) ceq;
            check quoted ",@a" (Success(3, list [gemSym "unquote-splicing"; gemSym "a"])) ceq;
            check quoted ",a" (Success(2, list [gemSym "unquote"; gemSym "a"])) ceq;

            check list_ "(1 2 3 4)" (Success(9, list [Number (IntV 1); Number (IntV 2); Number (IntV 3); Number (IntV 4)])) ceq;

            check vector "#(1 2 3)" (Success(8, Array [Number (IntV 1); Number (IntV 2); Number (IntV 3)])) ceq            

[<EntryPoint>]
let main argv =
    let test (s:string) (expr:Value) (ret:Value) (ctx:Scheme.Context): Scheme.Context = 
        printfn "Test:";
        printfn "  Input Code: %s" s;
        match Parser.expr s 0 with
        | ParserCombinator.Fail    (pos, value) -> printfn "Parse Failed at (%d): %s" pos value; ctx;
        | ParserCombinator.Success (pos, value) -> 
            printfn "  Parsed   AST: %A" value;
            printfn "  Expected AST: %A" expr;
            if (ceq value expr) = false 
            then (printfn "  -> AST Not Match"); ctx
            else
                let code = value |> Scheme.compile
                let ctx' = Scheme.update_context ctx code
                let result = Scheme.run ctx'
                in  
                    result.s.[0] |> toString |> printfn "  Execute  Result: %s";
                    ret          |> toString |> printfn "  Expected Result: %s";
                    (if ceq ret result.s.[0] then printfn "  All Match" else printfn "  -> Result Not Match");
                    ctx'
    let eval (s:string) (ctx:Scheme.Context): Scheme.Context = 
        printfn "Eval: %s" s;
        let rec loop pos ctx = 
            match Parser.start s pos with
            | ParserCombinator.Fail    (pos, value) -> printfn "Parse Failed at (%d): %s" pos value; ctx;
            | ParserCombinator.Success (pos, Some value) -> 
                //let code = value |> Scheme.compile
                let code = value |> toString |> printfn "%s";value |> printfn "%A"; value |> Scheme.compile
                let ctx' = Scheme.update_context ctx code
                let result = Scheme.run ctx'
                in  
                    result.s.[0] |> toString |> printfn "%s";
                    loop pos ctx' 
            | ParserCombinator.Success (pos, None) -> ctx
        in loop 0 ctx
    in
        Parser.test();
        Scheme.create_context () |>
        eval "((lambda (x) (set! x 2) x) 1)" |>
        test "(quote a)" (list [gemSym("quote"); gemSym("a")]) (gemSym "a")|>
        test "(if #t 'a 'b)" (list [gemSym("if");  true'; list [gemSym("quote"); gemSym("a")]; list [gemSym("quote"); gemSym("b")]]) (gemSym "a")|>
        test "(if #f 'a 'b)" (list [gemSym("if"); false'; list [gemSym("quote"); gemSym("a")]; list [gemSym("quote"); gemSym("b")]]) (gemSym "b")|>

        test "(car '(a b c))" (list [gemSym("car"); list [gemSym("quote"); list[gemSym("a");gemSym("b");gemSym("c")]]]) (gemSym "a")|>
        test "(cdr '(a b c))" (list [gemSym("cdr"); list [gemSym("quote"); list[gemSym("a");gemSym("b");gemSym("c")]]]) (list[gemSym("b");gemSym("c")])|>
        test "(cons 'a 'b)" (list [gemSym("cons"); list [gemSym("quote");gemSym("a")]; list [gemSym("quote");gemSym("b")]]) (Cell (ref (gemSym "a"), ref (gemSym "b")))|>
        test "(eq? 'a 'a)" (list [gemSym("eq?"); list [gemSym("quote"); gemSym("a")]; list [gemSym("quote"); gemSym("a")]]) (true')|>
        test "(eq? 'a 'b)" (list [gemSym("eq?"); list [gemSym("quote"); gemSym("a")]; list [gemSym("quote"); gemSym("b")]]) (false')|>
        test "(pair? '(a b c))" (list [gemSym("pair?"); list [gemSym("quote"); list [gemSym("a"); gemSym("b"); gemSym("c")]]]) (true')|>
        test "(pair? 'a)" (list [gemSym("pair?"); list [gemSym("quote"); gemSym("a")]]) false' |>

        test "(define a 'b)" (list [gemSym("define");gemSym("a");list[gemSym("quote");gemSym("b")]]) (gemSym "a") |>
        test "a" (gemSym "a") (gemSym "b")|>
        test "(lambda (x) x)" (list [gemSym "lambda"; list [gemSym "x"]; gemSym "x"]) (Closure ([Ld (0, 0); Rtn], [])) |>
        test "((lambda (x) x) 'a)" (list [list [gemSym "lambda"; list [gemSym "x"]; gemSym "x"]; list[gemSym "quote"; gemSym "a"]]) (gemSym "a") |>
        test "(define list (lambda x x))" (list [gemSym "define"; gemSym "list"; list [gemSym "lambda"; gemSym "x"; gemSym "x"]]) (gemSym "list") |>
        test "(list 'a 'b 'c 'd 'e)" (list [gemSym "list"; list [gemSym "quote";gemSym "a"]; list [gemSym "quote";gemSym "b"]; list [gemSym "quote";gemSym "c"]; list [gemSym "quote";gemSym "d"]; list [gemSym "quote";gemSym "e"]]) (list [gemSym "a";gemSym "b";gemSym "c";gemSym "d";gemSym "e"]) |> 
        
        test "(define x 'a)" (list [gemSym("define"); gemSym("x"); list [gemSym("quote"); gemSym("a")]]) (gemSym "x")|>
        test "x" (gemSym "x") (gemSym "a")|>
        test "(define foo (lambda () x))" (list [gemSym("define"); gemSym("foo"); list [gemSym("lambda"); nil; gemSym("x") ]]) (gemSym "foo")|>
        test "(foo)" (list [gemSym("foo")]) (gemSym "a")|>
        test "(define bar (lambda (x) (foo)))"  (list [gemSym("define"); gemSym("bar"); list [gemSym("lambda"); list[gemSym("x")]; list[gemSym("foo")] ]]) (gemSym "bar")|>
        test "(bar 'b)" (list [gemSym("bar"); list [gemSym("quote"); gemSym("b")]]) (gemSym "a")|>

        test "foo" (gemSym("foo")) (Closure ([Ldg (gemSym "x"); Rtn], [])) |>
        test "bar" (gemSym("bar")) (Closure ([Args 0; Ldg (gemSym "foo"); App; Rtn], [] )) |>

        eval "
(define = eq?)

(define not (lambda (x) (if x #f #t)))
(define null? (lambda (x) (eq? x ())))

(define zero? (lambda (x) (= x 0)))
(define positive? (lambda (x) (< 0 x)))
(define negative? (lambda (x) (> 0 x)))
(define even? (lambda (x) (zero? (mod x 2))))
(define odd? (lambda (x) (not (even? x))))
(define abs (lambda (x) (if (negative? x) (- x) x)))
(define max
  (lambda (x . xs)
    (fold-left (lambda (a b) (if (< a b) b a)) x xs)))
(define min
  (lambda (x . xs)
    (fold-left (lambda (a b) (if (> a b) b a)) x xs)))

(define gcdi
  (lambda (a b)
    (if (zero? b)
	a
      (gcdi b (mod a b)))))
(define gcd
  (lambda xs
    (if (null? xs)
	0
      (fold-left (lambda (a b) (gcdi a b)) (car xs) (cdr xs)))))

(define lcmi (lambda (a b) (/ (* a b) (gcdi a b))))
(define lcm
  (lambda xs
    (if (null? xs)
	1
      (fold-left (lambda (a b) (lcmi a b)) (car xs) (cdr xs)))))

; cxxr
(define caar (lambda (xs) (car (car xs))))
(define cadr (lambda (xs) (car (cdr xs))))
(define cdar (lambda (xs) (cdr (car xs))))
(define cddr (lambda (xs) (cdr (cdr xs))))

; cxxxr
(define caaar (lambda (xs) (car (caar xs))))
(define caadr (lambda (xs) (car (cadr xs))))
(define cadar (lambda (xs) (car (cdar xs))))
(define caddr (lambda (xs) (car (cddr xs))))
(define cdaar (lambda (xs) (cdr (caar xs))))
(define cdadr (lambda (xs) (cdr (cadr xs))))
(define cddar (lambda (xs) (cdr (cdar xs))))
(define cdddr (lambda (xs) (cdr (cddr xs))))

(define list (lambda x x))

(define append-1
  (lambda (xs ys)
    (if (null? xs)
	ys
      (cons (car xs) (append-1 (cdr xs) ys)))))

(define append
  (lambda xs
    (if (null? xs)
	'()
      (if (null? (cdr xs))
	  (car xs)
	(append-1 (car xs) (apply append (cdr xs)))))))

(define length
  (lambda (xs)
    (fold-left (lambda (a x) (+ a 1)) 0 xs)))

(define reverse
  (lambda (xs)
    (fold-left (lambda (a x) (cons x a)) () xs)))

(define list-tail
  (lambda (xs k)
    (if (zero? k)
	xs
      (list-tail (cdr xs) (- k 1)))))

(define list-ref 
  (lambda (xs k)
    (if (zero? k)
	(car xs)
      (list-ref (cdr xs) (- k 1)))))

(define null? (lambda (x) (eq? x '())))
(define not (lambda (x) (if (eq? x #f) #t #f)))
(define null? (lambda (x) (eq? x '())))
(define append
  (lambda (xs ys)
    (if (null? xs)
        ys
      (cons (car xs) (append (cdr xs) ys)))))
      
(define reverse
  (lambda (ls)
    (if (null? ls)
        '()
      (append (reverse (cdr ls)) (list (car ls))))))" |>

        eval "(append '(a b c) '(d e f))" |>        //"(a b c d e f)"
        eval "(append '((a b) (c d)) '(e f g))" |>  //((a b) (c d) e f g)
        eval "(reverse '(a b c d e))" |>            //(e d c b a)
        eval "(reverse '((a b) c (d e)))" |>        //((d e) c (a b))

        eval "
(define memq
  (lambda (x ls)
    (if (null? ls)
        #f
        (if (eq? x (car ls))
            ls
          (memq x (cdr ls))))))

(define assq
  (lambda (x ls)
    (if (null? ls)
        #f
      (if (eq? x (car (car ls)))
          (car ls)
        (assq x (cdr ls))))))" |>

        eval "(memq 'a '(a b c d e))" |> //(a b c d e)
        eval "(memq 'c '(a b c d e))" |> //(c d e)
        eval "(memq 'f '(a b c d e))" |> //#f
        eval "(assq 'a '((a 1) (b 2) (c 3) (d 4) (e 5)))" |> //(a 1)
        eval "(assq 'e '((a 1) (b 2) (c 3) (d 4) (e 5)))" |> //(e 5)
        eval "(assq 'f '((a 1) (b 2) (c 3) (d 4) (e 5)))" |> //#f

        eval "
(define map
  (lambda (fn ls)
    (if (null? ls)
        '()
      (cons (fn (car ls)) (map fn (cdr ls))))))

(define filter
  (lambda (fn ls)
    (if (null? ls)
        '()
      (if (fn (car ls))
          (cons (car ls) (filter fn (cdr ls)))
        (filter fn (cdr ls))))))

(define fold-right
  (lambda (fn a ls)
    (if (null? ls)
        a
      (fn (car ls) (fold-right fn a (cdr ls))))))

(define fold-left
  (lambda (fn a ls)
    (if (null? ls)
        a
      (fold-left fn (fn a (car ls)) (cdr ls)))))" |>

        eval "(map car '((a 1) (b 2) (c 3) (d 4) (e 5)))" |> //(a b c d e)
        eval "(map cdr '((a 1) (b 2) (c 3) (d 4) (e 5)))" |> //((1) (2) (3) (4) (5))
        eval "(map (lambda (x) (cons x x)) '(a b c d e))" |> //((a . a) (b . b) (c . c) (d . d) (e . e))
        eval "(filter (lambda (x) (not (eq? x 'a))) '(a b c a b c a b c))" |> //(b c b c b c)
        eval "(fold-left cons '() '(a b c d e))" |> //(((((() . a) . b) . c) . d) . e)
        eval "(fold-right cons '() '(a b c d e))" |> //(a b c d e)

        eval "
(define transfer
  (lambda (ls)
    (if (pair? ls)
        (if (pair? (car ls))
            (if (eq? (caar ls) 'unquote)
                (list 'cons (cadar ls) (transfer (cdr ls)))
              (if (eq? (caar ls) 'unquote-splicing)
                  (list 'append (cadar ls) (transfer (cdr ls)))
                (list 'cons (transfer (car ls)) (transfer (cdr ls)))))
          (list 'cons (list 'quote (car ls)) (transfer (cdr ls))))
      (list 'quote ls))))

(define-macro quasiquote (lambda (x) (transfer x)))


" |>
        eval "(transfer '(a b c) )" |>
        eval "(transfer '(,a b c) )" |>
        eval "(transfer '(,a ,@b c) )" |>
        eval "(transfer '(,(car a) ,@(cdr b) c) )" |>

        eval "(define a '(1 2 3))"|>
        eval "`(a b c)"|>
        eval "`(,a b c)"|>
        eval "`(,@a b c)"|>

        eval "

(define cadr (lambda (x) (car (cdr x))))
(define cdar (lambda (x) (cdr (car x))))
(define caar (lambda (x) (car (car x))))
(define cddr (lambda (x) (cdr (cdr x))))

(define-macro let
  (lambda (args . body)
    `((lambda ,(map car args) ,@body) ,@(map cadr args))))
    " |>

        eval "
(define a 0)
(define b 1)
(let ((a 10) (b 20)) (cons a b))
a
b
        " |>

        eval "
(define-macro and
  (lambda args
    (if (null? args)
        #t
      (if (null? (cdr args))
          (car args)
        `(if ,(car args) (and ,@(cdr args)) #f)))))

(define-macro or
  (lambda args
    (if (null? args)
        #f
      (if (null? (cdr args))
          (car args)
        `(let ((*value* ,(car args)))
          (if *value* *value* (or ,@(cdr args))))))))
" |>

        eval "(and 1 2 3)" |>
        eval "(and 1 #f 3)" |>
        eval "(or 1 2 3)" |>
        eval "(or #f #f 3)" |>
        eval "(or #f #f #f)" |>

        eval "
(define-macro let*
  (lambda (args . body) 
    (if (null? (cdr args))
        `(let (,(car args)) ,@body)
      `(let (,(car args)) (let* ,(cdr args) ,@body)))))        
        " |>

        eval "(let* ((a 100) (b a) (c (cons a b))) c)" |>

        eval "
(define map-2
  (lambda (fn xs ys)
    (if (null? xs)
        '()
      (cons (fn (car xs) (car ys)) (map-2 fn (cdr xs) (cdr ys))))))

(define-macro letrec
  (lambda (args . body)
    (let ((vars (map car args))
          (vals (map cadr args)))
      `(let ,(map (lambda (x) `(,x '*undef*)) vars)
            ,@(map-2 (lambda (x y) `(set! ,x ,y)) vars vals)
            ,@body))))

 (define reverse
  (lambda (ls)
    (letrec ((iter (lambda (ls a)
                     (if (null? ls)
                         a
                       (iter (cdr ls) (cons (car ls) a))))))
      (iter ls '()))))           
        " |>

        eval "(reverse '(a b c d e))" |>
        eval "(reverse '())" |>
        eval "(letrec ((a a)) a)" |>
        
        eval "
(define-macro let
  (lambda (args . body)
    (if (pair? args)
        `((lambda ,(map car args) ,@body) ,@(map cadr args))
      ; named-let
      `(letrec ((,args (lambda ,(map car (car body)) ,@(cdr body))))
        (,args ,@(map cadr (car body)))))))        
        " |>

        eval "
(define reversei
  (lambda (ls)
    (let loop ((ls ls) (a '()))
      (if (null? ls)
          a
          (loop (cdr ls) (cons (car ls) a))))))      
        " |>

        eval "(reversei '(a b c d e))" |>
        eval "(reversei '())" |>
        
        eval "
(define-macro begin
  (lambda args
    (if (null? args)
        `((lambda () '*undef*))
      `((lambda () ,@args)))))        
" |>

        eval "(begin)" |>
        eval "(begin 1 2 3 4 5)" |>

        eval "
(define-macro cond
  (lambda args
    (if (null? args)
        '*undef*
      (if (eq? (caar args) 'else)
          `(begin ,@(cdar args))
        (if (null? (cdar args))
            `(let ((*value* ,(caar args)))
              (if *value* *value* (cond ,@(cdr args))))
          `(if ,(caar args)
               (begin ,@(cdar args))
            (cond ,@(cdr args))))))))        
        " |>
        
        eval "(define cond-test
  (lambda (x)
    (cond ((eq? x 'a) 1)
          ((eq? x 'b) 2)
          ((eq? x 'c) 3)
          (else 0))))" |>

        eval "(cond-test 'a)" |>
        eval "(cond-test 'b)" |>
        eval "(cond-test 'c)" |>
        eval "(cond-test 'd)" |>
        eval "(cond-test 'e)" |>

        eval "
(define memv
  (lambda (x ls)
    (if (null? ls)
        #f
        (if (eqv? x (car ls))
            ls
          (memv x (cdr ls))))))

(define-macro case
  (lambda (key . args)
    (if (null? args)
        '*undef*
      (if (eq? (caar args) 'else)
          `(begin ,@(cdar args))
        `(if (memv ,key ',(caar args))
             (begin ,@(cdar args))
           (case ,key ,@(cdr args)))))))        
        " |>

        eval "
(define case-test
  (lambda (x)
    (case x
      ((a b c) 1)
      ((d e f) 2)
      ((g h i) 3)
      (else    0))))        
        " |>

        eval "
(define-macro do
  (lambda (var-form test-form . args)
    (let ((vars (map car var-form))
          (vals (map cadr var-form))
          (step (map cddr var-form)))
      `(letrec ((loop (lambda ,vars
                        (if ,(car test-form)
                            (begin ,@(cdr test-form))
                          (begin
                            ,@args
                            (loop ,@(map-2 (lambda (x y)
                                             (if (null? x) y (car x)))
                                           step
                                           vars)))))))
        (loop ,@vals)))))        
        " |>

        
        eval "
(define reverse-do
  (lambda (xs)
    (do ((ls xs (cdr ls)) (result '()))
        ((null? ls) result)
      (set! result (cons (car ls) result)))))        
        " |>

        eval "(reverse-do '(a b c d e))" |>

        //eval "" |>

        ignore;
        
        0

// http://www.ccs.neu.edu/home/dorai/mbe/mbe-imps.html
