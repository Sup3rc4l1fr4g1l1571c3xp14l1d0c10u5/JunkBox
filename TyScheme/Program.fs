open System
open System.Linq.Expressions
open System.Text.RegularExpressions;

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
    | (Number   v1, Number   v2) -> (v1 = v2)
    | (String   v1, String   v2) -> (v1 = v2)
    | (Boolean  v1, Boolean  v2) -> (v1 = v2)
    | (Primitive _, Primitive _) -> (Object.Equals(x,y))
    | (          _,           _) -> false

(* compiler *)

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

type ParserState<'a> = { pos:int; value:'a }
type Parser<'a> = string -> int -> ParserState<'a> option

let char (pred : char -> bool) = 
    fun (str:string) (pos:int) -> 
        if str.Length > pos && (pred str.[pos]) then Some({ pos=pos+1; value=str.[pos]}) else None

let anychar (chs:string) = 
    fun (str:string) (pos:int) -> 
        if str.Length > pos && chs.IndexOf(str.[pos]) <> -1 then Some({ pos=pos+1; value=str.[pos]}) else None

let submatch (s1:string) (i1:int) (s2:string) =
    if (i1 < 0 || s1.Length < s2.Length + i1) 
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
        if submatch str pos s then Some({ pos=pos+s.Length; value=s}) else None

let any () = 
    fun (str:string) (pos:int) -> 
        if str.Length > pos then Some({ pos=pos+1; value=str.[pos]}) else None

let not (parser:Parser<'a>) = 
    fun (str:string) (pos:int) -> 
        match parser str pos  with
        | None   -> Some { pos=pos; value=() }
        | Some _ -> None

let select (pred:'a->'b) (parser:Parser<'a>) = 
    fun (str:string) (pos:int) -> 
        match parser str pos  with
        | None   -> None
        | Some {pos=pos; value=value} -> Some {pos=pos; value=pred value}

let (|>->) (lhs : Parser<'a>) (pred:'a->'b) = select pred lhs

let where (pred:'a->bool) (parser:Parser<'a>) = 
    fun (str:string) (pos:int) -> 
        match parser str pos  with
        | None   -> None
        | Some {pos=pos; value=value} as ret -> if pred value then ret else None

let opt (parser:Parser<'a>) = 
    fun (str:string) (pos:int) ->       
        match parser str pos  with
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

let isdigit  (ch:char) = '0' <= ch && ch <= '9'
let isalpha  (ch:char) = ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z')
let issphead (ch:char) = "!$%&*/:<=>?^_~".IndexOf(ch) <> -1
let issptail (ch:char) = "+-.@".    IndexOf(ch) <> -1
let ishead   (ch:char) = issphead(ch) || isalpha(ch)
let istail   (ch:char) = issptail(ch) || isalpha(ch) || isdigit(ch)
let isspace  (ch:char) = " \t\r\n".IndexOf(ch) <> -1

let ident      = choice [
                    seq [
                        char ishead |>-> (fun x -> x.ToString()); 
                        char istail |> repeat |>-> List.fold (fun s x -> s + x.ToString()) ""
                    ] |>-> (fun xs -> List.fold (fun s x -> s + x.ToString()) "" xs);
                    str "+";
                    str "-";
                    str "...";
                 ]
let boolean    = choice [str "#t"; str "#f"]
let digits     = char isdigit |> repeat1 |>-> (fun xs -> List.fold (fun s x -> s + x.ToString()) "" xs)
let num        = choice [digits]
let character  = seq [ str "#\\"; choice [str "space"; str "newline"; any () |> select (fun x -> x.ToString())] ] |> select (fun xs -> List.fold (fun s x -> s + x.ToString()) "" xs)
let string     = seq [ str "\""; choice [
                                    str "\\\"" |> select (fun _ -> "\""); 
                                    str "\\r" |> select (fun _ -> "\r"); 
                                    str "\\t" |> select (fun _ -> "\t"); 
                                    str "\\n" |> select (fun _ -> "\n"); 
                                    str "\\f" |> select (fun _ -> "\f"); 
                                    str "\\n" |> select (fun _ -> "\b"); 
                                    any ()    |> select (fun x -> x.ToString())
                                ] ; str "\""] |> select (fun xs -> List.fold (fun s x -> s + x.ToString()) "" xs)
let single     = choice [str "#("; str ",@"; anychar("()'`,.") |> select (fun x -> x.ToString())]
let whitespace = char isspace |> repeat  |> select (fun xs -> List.fold (fun s x -> s + x.ToString()) "" xs)
let alphabets  = char isalpha |> repeat1 |> select (fun xs -> List.fold (fun s x -> s + x.ToString()) "" xs)


let (|++>) (lhs : Parser<'b>) (rhs : Parser<'a>) =
    fun (str:string) (pos : int) -> 
        match lhs str pos with
        | None -> None
        | Some {pos=pos; value=value1} -> 
            match rhs str pos with
            | None -> None
            | Some {pos=pos; value=value2} -> 
                Some {pos=pos; value=(value1, value2)} 

let (|-+>) (lhs : Parser<'b>) (rhs : Parser<'a>) =
    fun (str:string) (pos : int) -> 
        match lhs str pos with
        | None -> None
        | Some {pos=pos; value=value1} -> 
            match rhs str pos with
            | None -> None
            | Some {pos=pos; value=value2} -> 
                Some {pos=pos; value=value2} 

let (|+->) (lhs : Parser<'b>) (rhs : Parser<'a>) =
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

let parse_atom = 
    whitespace |-+> choice [ 
        string |>-> (fun x -> String x); 
        num    |>-> (fun x -> Number (Int32.Parse x)); 
        ident  |>-> (fun x -> Symbol x)
    ] 
    
let rec parse_quoted = 
    whitespace |-+> choice [
        str "'"  |-+> whitespace |-+> parse_expr |>-> (fun x -> list [Symbol("quote"); x] );
        str "`"  |-+> whitespace |-+> parse_expr |>-> (fun x -> list [Symbol("quasiquote"); x] );
        str ",@" |-+> whitespace |-+> parse_expr |>-> (fun x -> list [Symbol("unquote-splicing"); x] );
        str ","  |-+> whitespace |-+> parse_expr |>-> (fun x -> list [Symbol("unquote"); x] );
    ]

and parse_list =
    whitespace 
    |+-> str "(" 
    |-+> (repeat1 parse_expr) 
    |+-> whitespace 
    |++> opt (
        str "." 
        |-+> parse_expr 
        |+-> whitespace
    ) 
    |+-> str ")" 
    |>-> (fun (xs, x) -> List.foldBack (fun x s -> Cell(x, s)) xs (match x with Some v -> v | None -> Nil) );

and parse_expr = lazy_( fun () ->
    whitespace |-+> choice [
        parse_quoted; 
        parse_list; 
        parse_atom
    ]
)

let run (context:Context) :Context =
    let rec loop context =
        let context = vm context
        in  if context.halt then context else loop context
    in loop context

let create_context () : Context =
    { s=[]; e=[]; c=[Stop]; d=[]; halt = true }

let update_context(context: Context) (expr:Inst list) : Context =
    { context with c=expr; halt = false }

[<EntryPoint>]
let main argv =
    let test(s:string) (ret:Value) : unit = 
        let ctx = create_context ()
        in
            match parse_list s 0 with
            | None -> failwith "syntax error"
            | Some {value=v} -> 
                let code = v |> compile
                let ctx' = update_context ctx code
                let result = run ctx'
                in  
                    result.s.[0] |> toString |> printfn "%s";
                    if eq ret result.s.[0] then printfn "%s" "match" else printfn "%s" "not-match"
    
    let context = ref (create_context ())
    let eval_ast (expr:Value) : unit =
        let code = expr |> compile
        let ctx = update_context !context code
        let result = run ctx
        in 
            result |> printfn "%A";
            context := { result with s=[]; e=[]; c=[Stop]; d=[]; halt = true }
    in
            test "(quote a)" (Symbol "a");

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

