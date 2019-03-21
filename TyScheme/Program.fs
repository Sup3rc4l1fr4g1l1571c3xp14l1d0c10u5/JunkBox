open System

(* Value Type *)

type Value  = 
      Symbol of value : string
    | Nil
    | Cell of car: Value * cdr: Value
    | String of value : string
    | Number of value : int
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
            | Cell (car, cdr) -> 
                match isCdr with
                    | false -> "(" + (loop car false) + " " + (loop cdr true) + ")"
                    | true  -> (loop car false) + " " + (loop cdr true)
            | String value -> escapeString value
            | Number value -> value.ToString ()
            | Boolean value -> 
                match value with
                    | false -> "#f"
                    | true  -> "#t"
            | Primitive value -> sprintf "<Primitive: %s>" (value.ToString ())
            | Closure (value, env) -> sprintf "<Closure: %s>" (value.ToString ())
            | Error (subtype, message) -> sprintf "<%s: %s>" subtype message
    in loop value false

(* Scheme's primitive functions *)

let car (x: Value): Value =
    match x with
        | Cell (car, cdr) -> car
        | _ -> Error ("runtime error", (sprintf "Attempt to apply car on %s" (toString x)))

let cdr (x: Value): Value =
    match x with
        | Cell (car, cdr) -> cdr
        | _ -> Error ("runtime error", (sprintf "Attempt to apply cdr on %s" (toString x)))

let cadr   = car << cdr
let caddr  = car << cdr << cdr
let cadddr = car << cdr << cdr << cdr
let cddr   = cdr << cdr
let cdddr  = cdr << cdr << cdr

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
        | Cell _ ->  true
        | _        ->  false

let iserror x = 
    match x with
        | Error _ ->  true
        | _       ->  false

let list (values : Value list) : Value =
    List.foldBack (fun x y -> Cell (x,y)) values Nil

let list2 (values : Value list) : Value =
    List.reduceBack (fun x y -> Cell (x,y)) values
  
let length (x:Value) : int = 
    let rec loop (x:Value) (n:int) =
        match x with
            | Cell (a, d) -> loop d (n + 1)
            | Nil -> n
            | _ -> failwith "not pair"
    in  loop x 0

let eq (x:Value) (y:Value) : bool =
    match (x,y) with
        | (Symbol v1, Symbol v2) -> (v1 = v2)//(Object.Equals(x,y))
        | (Cell _, Cell _) -> (Object.Equals(x,y))
        | (Number v1, Number v2) -> (v1 = v2)
        | (String v1, String v2) -> (v1 = v2)
        | (Boolean v1, Boolean v2) -> (v1 = v2)
        | (Primitive _, Primitive _) -> (Object.Equals(x,y))
        | _ -> false

(* compiler *)

let compile (expr:Value) : Inst list =
    let position_var (sym:Value) (ls:Value) : int option =
        let rec loop (ls:Value) (i:int) =
            match ls with
            | Nil -> None
            | Symbol _ as y -> if (eq sym y) then Some (-(i + 1)) else None
            | Cell (a, d) ->
                if (eq sym a) then Some i  else loop d (i + 1)
            | _ -> failwith "bad variable table"
        in loop ls 0

    let location (sym:Value) (ls:Value list) : (int * int) option  =
        let rec loop (ls:Value list) (i:int) =
            match ls with
            | [] -> None
            | a::d ->
                match position_var sym a with
                | Some j -> Some(i, j)
                | None -> loop d (i + 1)
        in loop ls 0

    let is_self_evaluation (expr:Value) : bool =
        ((ispair expr)) = false && ((issymbol expr) = false)

    let rec comp (expr: Value) (env: Value list) (code: Inst list): Inst list =
        if is_self_evaluation expr then Ldc(expr) :: code
        else if issymbol expr then 
            match location expr env with
            | Some (i, j) -> Ld(i, j) :: code
            | None -> Ldg(expr) :: code
        else 
            match expr with
            | Cell (Symbol("quote"), Cell(v, Nil)) -> Ldc(v) :: code
            | Cell (Symbol("if"), Cell(cond, Cell(t, Nil))) -> 
                let t_clause = comp t env [Join]
                let f_clause = [Ldc (Symbol "*undef"); Join] 
                in  comp cond env (Sel (t_clause, f_clause) :: code)
            | Cell (Symbol("if"), Cell(cond, Cell(t, Cell(e, Nil)))) -> 
                let t_clause = comp t env [Join]
                let f_clause = comp e env [Join]
                in  comp cond env (Sel (t_clause, f_clause) :: code)
            | Cell ((Symbol "lambda"), _) ->
                let body = comp_body (cddr expr) (cadr expr :: env) [Rtn]
                in  Ldf(body) :: code
            | Cell ((Symbol "define"), Cell((Symbol _) as sym, Cell(body, Nil))) -> 
                comp body env (Def(sym)::code)
            | _ -> 
                complis (cdr expr) env ((Args (length(cdr expr)))::(comp (car expr) env (App::code)))
    and comp_body (body: Value) (env: Value list) (code: Inst list) : Inst list =
        if isnull (cdr body) 
        then comp (car body) env code
        else comp (car body) env (Pop :: (comp_body (cdr body) env code))
    and complis (expr: Value) (env: Value list) (code: Inst list): Inst list =
        if isnull(expr) 
        then code 
        else comp (car expr) env (complis (cdr expr) env code)
    in
        if iserror(expr)
        then [Ldc expr; Stop]
        else comp expr [] [Stop]

(* secd virtual machine *)

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
        let rec loop (x:Value) (n:int) =
            match x with
            | Nil                -> Number n
            | Cell (Number l, xs) -> loop xs (n+l)
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
        | ((key, value) as entry) :: d -> 
            if (eq key sym) then Some entry
                            else loop d
        | _ -> failwith "bad dictionary"
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
        match (context.s, context.e, context.c, context.d) with
        | (s, e, Ld(i,j)::c', d) -> { context with s = (get_lvar e i j) :: s; e = e; c = c'; d = d }
        | (s, e, Ldc(v)::c', d) -> { context with s = v::s; e = e; c = c'; d = d }
        | (s, e, Ldg(v)::c', d) -> { context with s = (get_gvar v) :: s; e = e; c = c'; d = d }
        | (s, e, Ldf(v)::c', d) -> { context with s = Closure (v, e)::s; e = e; c = c'; d = d }
        | ((Primitive value) :: lvar :: s', e, App::c', d) -> { context with s = value lvar :: s'; e = e; c = c'; d = d }
        | (Closure (v',e') :: lvar :: s', e, App::c', d) -> { context with s = []; e = lvar :: e'; c = v'; d = (s', e, c') :: d }
        | (s1::[], e, Rtn::_, (s2, e', c') :: d') -> { context with s = s1 :: s2; e = e'; c = c'; d = d' }
        | ((Boolean v) :: s', e, Sel(t_clo, e_clo)::c', d) -> { context with s = s'; e = e; c = (if v then t_clo else e_clo); d = ([],[],c') :: d }
        | (s, e, Join::_, (_, _, c') :: d') -> { context with s = s; e = e; c = c'; d = d' }
        | (_ :: s', e, Pop::c', d) -> { context with s = s'; e = e; c = c'; d = d }
        | (s, e, Args(value)::c', d) -> 
            let rec loop a s n =
                match (s,n) with
                | (_,0) -> { context with s = a :: s; e = e; c = c'; d = d }
                | (car :: cdr, _)-> loop (Cell(car, a)) cdr (n - 1)
                | _ -> failwith "bad args"
            in loop Nil s value
        | (body :: s', e, Def(sym):: c', d) -> 
            begin
                global_environment := (sym, body) :: !global_environment;
                { context with s = sym :: s'; e = e; c = c'; d = d };
            end
        | (s, e, Stop::c', d) -> { context with halt= true }
        | _ -> failwith "bad context"

let run (context:Context) :Context =
    let rec loop context =
        let context = vm context
        in  if context.halt 
            then context 
            else loop context
    in loop context

let create_context () : Context =
    { s=[]; e=[]; c=[Stop]; d=[]; halt = true }

let update_context(context: Context) (expr:Inst list) : Context =
    { context with c=expr; halt = false }

[<EntryPoint>]
let main argv =
    let context = ref (create_context ());
    let eval_ast (expr:Value) : unit =
        let code = expr |> compile
        let ctx = update_context !context code
        let result = run ctx
        in 
            result |> printfn "%A";
            context := { result with s=[]; e=[]; c=[Stop]; d=[]; halt = true }
    in
        list [Symbol("quote"); Symbol("a")] |> eval_ast;
        list [Symbol("if"); Symbol("#t"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]] |> eval_ast;
        list [Symbol("if"); Symbol("#f"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]] |> eval_ast;
        list [Symbol("car"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]] |> eval_ast;
        list [Symbol("cdr"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]] |> eval_ast;
        list [Symbol("cons"); list [Symbol("quote");Symbol("a")]; list [Symbol("quote");Symbol("b")]] |> eval_ast;
        list [Symbol("eq?"); list [Symbol("quote");Symbol("a")]; list [Symbol("quote");Symbol("a")]] |> eval_ast;
        list [Symbol("eq?"); list [Symbol("quote");Symbol("a")]; list [Symbol("quote");Symbol("b")]] |> eval_ast;
        list [Symbol("pair?"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]] |> eval_ast;
        list [Symbol("pair?"); list [Symbol("quote"); Symbol("a")]] |> eval_ast;

        list [Symbol("define"); Symbol("a"); list [Symbol("quote"); Symbol("b")]] |> eval_ast;
        Symbol("a") |> eval_ast;
        list [Symbol("lambda"); list [Symbol("x")]; Symbol("x") ] |> eval_ast;
        list [list [Symbol("lambda"); list [Symbol("x")]; Symbol("x") ]; list [Symbol("quote"); Symbol("a")]] |> eval_ast;
        list [Symbol("define"); Symbol("list"); list [Symbol("lambda"); Symbol("x"); Symbol("x")]] |> eval_ast;
        list [Symbol("list"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]; list [Symbol("quote"); Symbol("c")]; list [Symbol("quote"); Symbol("d")]; list [Symbol("quote"); Symbol("e")]] |> eval_ast;

        0

