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
    | (Nil        , Nil        ) -> true
    | (          _,           _) -> false

let rec equal (x:Value) (y:Value) : bool =
    match (x,y) with
    | (Symbol        v1, Symbol        v2) -> (v1 = v2)//(Object.Equals(x,y))
    | (Cell     (a1,d1), Cell     (a2,d2)) -> (equal a1 a2) && (equal d1 d2)
    | (Number        v1, Number        v2) -> (v1 = v2)
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

    let isdigit  (ch:char) = '0' <= ch && ch <= '9'
    let isalpha  (ch:char) = ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z')
    let issphead (ch:char) = "!$%&*/:<=>?^_~".IndexOf(ch) <> -1
    let issptail (ch:char) = "+-.@".IndexOf(ch) <> -1
    let ishead   (ch:char) = issphead(ch) || isalpha(ch)
    let istail   (ch:char) = issptail(ch) || isalpha(ch) || isdigit(ch)
    let isspace  (ch:char) = " \t\r\n".IndexOf(ch) <> -1

    let (|>->) (lhs : Parser<'a>) (pred:'a->'b) = select pred lhs

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

module ParserCombinatorExt =
    open ParserCombinator
    open System.Runtime.CompilerServices;
    [<Extension>]
    type IEnumerableExtensions() =
        [<Extension>]
        static member inline Not(self: Parser<'T>) = not self
        [<Extension>]
        static member inline And(self: Parser<'T1>, rhs:Parser<'T2>) = self |++> rhs
        [<Extension>]
        static member inline AndL(self: Parser<'T1>, rhs:Parser<'T2>) = self |+-> rhs
        [<Extension>]
        static member inline AndR(self: Parser<'T1>, rhs:Parser<'T2>) = self |-+> rhs
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

    let ident      = choice [
                        seq [
                            char ishead |>-> (fun x -> x.ToString()); 
                            char istail |> repeat |>-> List.fold (fun s x -> s + x.ToString()) ""
                        ] |>-> List.fold (fun s x -> s + x) "";
                        str "+";
                        str "-";
                        str "...";
                     ]
    let boolean    = choice [str "#t" |>-> (fun (_) -> true); str "#f"|>-> (fun (_) -> false)]
    let digits     = char isdigit |> repeat1 |>-> List.fold (fun s x -> s + x.ToString()) ""
    let num        = choice [digits]
    let character  = seq [ str "#\\"; choice [str "space"; str "newline"; any () |>-> (fun x -> x.ToString())] ] |>-> List.fold (fun s x -> s + x.ToString()) ""
    let string     = seq [ str "\""; choice [
                                        str "\\\"" |> select (fun _ -> "\""); 
                                        str "\\r" |> select (fun _ -> "\r"); 
                                        str "\\t" |> select (fun _ -> "\t"); 
                                        str "\\n" |> select (fun _ -> "\n"); 
                                        str "\\f" |> select (fun _ -> "\f"); 
                                        str "\\n" |> select (fun _ -> "\b"); 
                                        (not (str "\"")) |-+> any () |> select (fun x -> x.ToString())
                                    ] ; str "\""] |>-> List.fold (fun s x -> s + x.ToString()) ""
    let single     = choice [str "#("; str ",@"; anychar("()'`,.") |> select (fun x -> x.ToString())]
    let whitespace = char isspace |> repeat  |>-> List.fold (fun s x -> s + x.ToString()) ""
    let alphabets  = char isalpha |> repeat1 |>-> List.fold (fun s x -> s + x.ToString()) ""

    let parse_atom = 
        whitespace |-+> choice [ 
            string  |>-> (fun x -> String x); 
            num     |>-> (fun x -> Number (Int32.Parse x)); 
            boolean |>-> (fun x -> Boolean x)
            ident   |>-> (fun x -> Symbol x)
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
    let test (s:string) (expr:Value) (ret:Value) : unit = 
        let ctx = create_context ()
        in
            match Parser.parse_list s 0 with
            | None -> failwith "syntax error"
            | Some {value=v} -> 
                if (equal v expr) = false 
                then printfn "%s %A %A" "not-match" v expr
                else
                    let code = v |> Compiler.compile
                    in  
                        let ctx' = update_context ctx code
                        let result = run ctx'
                        in  
                            result.s.[0] |> toString |> printfn "%s";
                            if equal ret result.s.[0] then printfn "%s" "match" else printfn "%s %A %A" "not-match" ret result.s.[0]
    
    let context = ref (create_context ())
    let eval_ast (expr:Value) : unit =
        let code = expr |> Compiler.compile
        let ctx = update_context !context code
        let result = run ctx
        in 
            result |> printfn "%A";
            context := { result with s=[]; e=[]; c=[Stop]; d=[]; halt = true }
    in
            test "(quote a)" (list [Symbol("quote"); Symbol("a")]) (Symbol "a");
            test "(if #t 'a 'b)" (list [Symbol("if"); Boolean  true; list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]]) (Symbol "a");
            test "(if #f 'a 'b)" (list [Symbol("if"); Boolean false; list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]]) (Symbol "b");

            test "(car '(a b c))" (list [Symbol("car"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]]) (Symbol "a");
            test "(cdr '(a b c))" (list [Symbol("cdr"); list [Symbol("quote"); list[Symbol("a");Symbol("b");Symbol("c")]]]) (list[Symbol("b");Symbol("c")]);
            test "(cons 'a 'b)" (list [Symbol("cons"); list [Symbol("quote");Symbol("a")]; list [Symbol("quote");Symbol("b")]]) (Cell (Symbol "a", Symbol "b"));
            test "(eq? 'a 'a)" (list [Symbol("eq?"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("a")]]) (Boolean true);
            test "(eq? 'a 'b)" (list [Symbol("eq?"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]]) (Boolean false);
            test "(pair? '(a b c))" (list [Symbol("pair?"); list [Symbol("quote"); list [Symbol("a"); Symbol("b"); Symbol("c")]]]) (Boolean true);
            test "(pair? 'a)" (list [Symbol("pair?"); list [Symbol("quote"); Symbol("a")]]) (Boolean false);

            list [Symbol("define"); Symbol("a"); list [Symbol("quote"); Symbol("b")]] |> eval_ast;
            Symbol("a") |> eval_ast;
            list [Symbol("lambda"); list [Symbol("x")]; Symbol("x") ] |> eval_ast;
            list [list [Symbol("lambda"); list [Symbol("x")]; Symbol("x") ]; list [Symbol("quote"); Symbol("a")]] |> eval_ast;
            list [Symbol("define"); Symbol("list"); list [Symbol("lambda"); Symbol("x"); Symbol("x")]] |> eval_ast;
            list [Symbol("list"); list [Symbol("quote"); Symbol("a")]; list [Symbol("quote"); Symbol("b")]; list [Symbol("quote"); Symbol("c")]; list [Symbol("quote"); Symbol("d")]; list [Symbol("quote"); Symbol("e")]] |> eval_ast;


        0


//start = _ e:expr* _ { return e; } 

//expr 
//  = _ e:quoted { return e; }
//  / _ e:list   { return e; }
//  / _ e:vector { return e; }
//  / _ e:atom   { return e; }
  
//char 
//  = _ "#\\space" { return { type:"char", 0x20; }
//  / _ "#\\newline" { return 0x0A; }
//  / _ "#\\" ch:. { return ch.charCodeAt(0); }
 
//bool 
//  = _ "#t" { return true; }
//  / _ "#f" { return false; }
 
//ch 
//  = "\\\""   { return '"'; }
//  / '\\r'    { return "\r"; }
//  / '\\n'    { return "\n"; }
//  / '\\t'    { return "\t"; }
//  / '\\f'    { return "\f"; }
//  / ch:[^\"] { return ch; }

//string = _ '\"' s:ch* '\"' { return String.concat(...s); }

//number = _ d:digit { return d; }

//digit = a:(digit_prefix a:accuracy_prefix { return a; } / a:accuracy_prefix digit_prefix { return a; }) v:digit_complex { return { type:"digit", accuracy: a, value:v }; }

//accuracy_prefix = ("#i" / "#e")?

//digit_prefix = ("#d")?

//digit_complex 
//  = digit_real "@" digit_real
//  / real:digit_real "+i"                            { return {type:"complex", real:real, imaginary: { type:"int", sign: "+", value: "1" } }; }
//  / real:digit_real "+"  imaginary:digit_usreal "i" { return {type:"complex", real:real, imaginary: imaginary }; }
//  / real:digit_real "-i"                            { return {type:"complex", real:real, imaginary: { type:"int", sign: "-", value: "1" } }; }
//  / real:digit_real "-"  imaginary:digit_usreal "i" { return {type:"complex", real:real, imaginary: (imaginary.sign="-",imaginary) }; }
//  / "+i"                                            { return {type:"complex", real:{ type:"int", sign: "+", value: "0" }, imaginary: imaginary }; }
//  / "+" digit_usreal "i"                            { return {type:"complex", real:{ type:"int", sign: "+", value: "0" }, imaginary: imaginary }; }
//  / "-i"                                            { return {type:"complex", real:{ type:"int", sign: "+", value: "0" }, imaginary: (imaginary.sign="-",imaginary) }; }
//  / "-" digit_usreal "i"                            { return {type:"complex", real:{ type:"int", sign: "+", value: "0" }, imaginary: (imaginary.sign="-",imaginary) }; }
//  / real:digit_real                                 { return real; }

//digit_real
//  = s:sign d:digit_usreal { return (d.sign="-",d); }

//digit_usreal
//  = numerator:digit_usint _ "/" _ denominator:digit_usint { return { type:"fraction", sign:"+", numerator:numerator, denominator:denominator }; }
//  / digit_decimals
//  / digit_usint

//digit_decimals
//  =                    "."  d:digit_num  "#"* suffix:digit_suffix { return { type:"decimal", sign:"+", integer:"0", fractional:d, suffix:suffix };  }
//  / is:digit_num+      "." ds:digit_num* "#"* suffix:digit_suffix { return { type:"decimal", sign:"+", integer:String.concat(...is), fractional:String.concat(...ds), suffix:suffix  }; }
//  / is:digit_num+ "#"+ "."               "#"* suffix:digit_suffix { return { type:"decimal", sign:"+", integer:String.concat(...is), fractional:"0", suffix:suffix  }; }
//  / i :digit_usint                            suffix:digit_suffix { return (i.suffix = suffix, i); }

//digit_suffix
//  = (m:[esfdlESFDL] sign:sign ds:digit_num+ { return { marker:m, exponent:sign+String.concat(...ds)}; }) ?

//digit_num = [0-9]

//digit_usint = is:digit_num+ "#"* { return { type:"int", value: String.concat(...is) }; }

//sign = [+\-]?

//head = [!$%&*:/<=>?^_~A-Za-z]
//tail = [!$%&*:/<=>?^_~+\-.@A-Za-z0-9]

//ident
//  = _ c:head cs:tail*  { return c + (cs.length > 0 ? String.concat(...cs) : ""); }
//  / _ c:"+"   { return c; }
//  / _ c:"-"   { return c; }
//  / _ c:"..." { return c; }

//atom
//  = v:string { return { type:"string", value: v}; }
//  / v:char   { return { type:"char", value: v}; }
//  / v:bool   { return { type:"bool", value: v}; }
//  / v:number { return { type:"number", value: v}; }
//  / v:ident  { return { type:"ident", value: v}; }

//quoted 
//  = _ "'"  e:expr { return ["quote", e]; }
//  / _ "`"  e:expr { return ["quasiquote", e]; }
//  / _ ".@" e:expr { return ["unquote-splicing", e]; }
//  / _ ","  e:expr { return ["unquote", e]; }

//list 
//  = _ "(" _ es:expr+ ee:( _ "." x:expr { return x; } )? _ ")" { return es.reduceRight((s,x) => [x,s], ee); }
//  / _ "(" _ ")" { return [];  }

//vector
//  = _ "#(" _ es:expr+ _ ")" { return Array.concat(es);  }
//  / _ "#(" _ ")" { return [];  }

//_ = ([ \r\n\t] / ';' [^\n]* "\n")*
