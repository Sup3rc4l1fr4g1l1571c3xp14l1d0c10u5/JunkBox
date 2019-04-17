open System.Diagnostics

(* parser combinator *)
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

    let empty () = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            succ (pos) str.[pos]  max

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
            | Fail (pos, max2)  -> Success (pos, None, max2) 
            | Success (pos, value, max2) -> Success (pos, Some(value), max2) 

    let seq (parsers:Parser<'a> list) =
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            let rec loop (parsers:Parser<'a> list) (pos:int) (max:MaxPosition) (values: 'a list) =
                match parsers with
                | []   -> Success (pos, List.rev values, max)
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
                | Fail (pos,max2)  -> Success (pos, List.rev values, max2)
                | Success (pos, value, max2) -> loop pos (value :: values) max2
            in loop pos [] max

    let repeat1 (parser:Parser<'a>) = 
        fun (str:string) (pos : int) (max:MaxPosition) -> 
            let rec loop pos values max = 
                match parser str pos max with
                | Fail (pos,max2)  -> Success (pos, List.rev values, max2)
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
                | Success (pos, value2, max3) -> Success (pos, (value1, value2), max3) 

    let andRight (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) (max:MaxPosition) -> 
            match lhs str pos max with
            | Fail    (pos, max2) -> fail pos "andRight: not match left rule" max2
            | Success (pos, value1, max2) -> 
                match rhs str pos max2 with
                | Fail    (pos, max3) -> fail pos "andRight: not match right rule" max3
                | Success (pos, value2, max3) ->  Success (pos, value2, max3) 

    let andLeft (rhs : Parser<'b>) (lhs : Parser<'a>) =
        fun (str:string) (pos : int) (max:MaxPosition)-> 
            match lhs str pos max with
            | Fail    (pos, max2) -> fail pos "andLeft: not match left rule" max2
            | Success (pos, value1, max2) -> 
                match rhs str pos max2 with
                | Fail    (pos, max3) -> fail pos "andLeft: not match left rule" max3
                | Success (pos, value2, max3) -> Success (pos, value1, max3) 

    let lazy_ (p:unit -> Parser<'a>) = 
        fun (str:string) (pos:int) (max:MaxPosition)  ->  (p ()) str pos max

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
        [<Extension>]
        static member inline Where(self: Parser<'T1>, selector:'T1 -> bool) = where selector self 
        [<Extension>]
        static member inline AsString(self: Parser<'T list>) = asString self 

type Ast = 
      Pred of string * (Ast list)
    | Atom of string
    | Num of int
    | Var of string * int

let rec print_ast ast =
    match ast with
    | Pred (x, xs) -> sprintf "(%s %s)" x (List.map print_ast xs |> String.concat " ")
    | Atom v -> v 
    | Num v -> v.ToString()
    | Var (v,i) -> sprintf "%s{%d}" v i

let default_ops = [
    1300, "xf",  ["."]; (* added *)
    1200, "xfx", ["-->"; ":-"];
    1190, "fx",  [":-"; "?-"];
    1150, "fx",  ["dynamic"; "discontiguous"; "initialization"; "meta_predicate";
                "module_transparent"; "multifile"; "public"; "thread_local";
                "thread_initialization"; "volatile"];
    1100, "xfy", [";"];
    (*1100, "xfy", [";"; "|"];*)
    1050, "xfy", ["->"; "*->"];
    1000, "xfy", [","];
    995,  "xfy", ["|"];(* change *)
    990,  "xfx", [":="];
    900,  "fy",  ["\\+"];
    700,  "xfx", ["<"; "="; "=.."; "=@="; "\\=@="; "=:="; "=<"; "=="; "=\\=";
                ">"; ">="; "@<"; "@=<"; "@>"; "@>="; "\\="; "\\=="; "as";
                "is"; ">:<"; ":<"];
    600,  "xfy", [":"];
    500,  "yfx", ["+"; "-"; "/\\"; "\\/"; "xor"];
    500,  "fx",  ["?"];
    400,  "yfx", ["*"; "/"; "//"; "div"; "rdiv"; "<<"; ">>"; "mod"; "rem"];
    200,  "xfx", ["**"];
    200,  "xfy", ["^"];
    200,  "fy",  ["+"; "-"; "\\"];
    (*100, "yfx", ["."];*)
    1,    "fx",  ["$"];
]

let rec assoc key ls =
    match ls with
    | [] -> None
    | (k,v)::xs -> if (k = key) then Some v else assoc key xs

(* operator perecedences *)
let opsmap = ref []

let rec update x v = function
    | [] -> [x,v]
    | (k,_)::xs when x=k -> (x,v)::xs
    | (k,v1)::xs -> (k,v1)::(update x v xs)

let opadd((p:int),(o:string),(ls : string list)) =
    List.iter(fun (op:string) ->
        let map = (op,p)::((assoc o !opsmap).Value)
        in opsmap := update o map !opsmap
    ) ls

let init_ops() =
    opsmap := ["xfx",[];"yfx",[];"fx",[];"fy",[];"xfy",[];"xf",[];"yf",[]];
    List.iter opadd default_ops

let _ = init_ops ()

let rec opn defaultp os op =
  match os with
  | []     -> defaultp
  | o::os  -> match assoc op ((assoc o !opsmap).Value) with 
              | None -> opn defaultp os op
              | Some v -> v

let opnFx  = opn (-1) ["fx";"fy"]
let opnXf  = opn (-1) ["xf";"yf"]
let opnXfy = opn (-1) ["xfy";"xfx"]
let opnYfx = opn (-1) ["yfx"]

let prefixs  = opn 10001 ["fx";"fy"]
let postfixs = opn 10001 ["xf";"yf"]
let infixrs  = opn 10001 ["xfy";"xfx"]
let infixs   = opn 10001 ["yfx"]

let rec opconvert e =
    fst (opconvert_pre 10000 e)
and opconvert_pre p e = 
    match e with
    | Pred("",[Atom(op);y]) when prefixs op <= p -> 
        let (t,ts) = opconvert_pre (prefixs op) y 
        in (Pred(op,[t]),ts)
    | Pred("",[Pred("",xs);y]) -> 
        let (t,ts) = opconvert_pre 10000 (Pred("",xs)) 
        in opconvert_post p t y
    | Pred("",[x;y]) -> 
        opconvert_post p (opconvert x) y
    | Pred(a,s) -> 
        (Pred(a,List.map opconvert s),Atom"")
    | _ -> 
        (e, Atom "")
and opconvert_post p t = function
    | Pred("",[Atom(op);y]) when infixs op < p ->
        let (t2,ts2) = opconvert_pre (infixs op) y 
        in opconvert_post p (Pred(op,[t;t2])) ts2
    | Pred("",[Atom(op);y]) when infixrs op <= p ->
        let (t2,ts2) = opconvert_pre (infixrs op) y 
        in opconvert_post p (Pred(op,[t;t2])) ts2
    | Pred("",[Atom(op);y]) when postfixs op <= p ->
        opconvert_post p (Pred(op,[t])) y
    | tokens -> (t,tokens)

module Parser =
    open ParserCombinator
    open ParserCombinatorExt

    let non_alphanum_chars = anychar "#$&*+-./:<=>?@^~\\"

    let whitespace = (char (fun x -> "\r\n\t\a ".IndexOf x <> -1)).Select(fun _ -> ())
    let comment = (char (fun x -> x = '%')).And((char (fun x -> x <> '\n')).Many()).And(char (fun x -> x = '\n')).Select(fun _ -> ())
    let isupper = fun x -> 'A' <= x && x <= 'Z' 
    let islower = fun x -> 'a' <= x && x <= 'z'
    let isdigit = fun x -> '0' <= x && x <= '9'
    let alpha_chars = char (fun x -> (x='_') || ('A' <= x && x <= 'Z') || ('a' <= x && x <= 'z'))
    let alphanum_chars = char (fun x -> (x='_') || ('A' <= x && x <= 'Z') || ('a' <= x && x <= 'z') || ('0' <= x && x <= '9'))
    let whitespaces = choice[whitespace;comment].Many()

    let LBRACK = whitespaces.AndR(str "[")
    let RBRACK = whitespaces.AndR(str "]")
    let LPAREN = whitespaces.AndR(str "(")
    let RPAREN = whitespaces.AndR(str ")")
    let DOT = whitespaces.AndR(str ".")
    let COMMA = whitespaces.AndR(str ",")
    let SEMICOLON = whitespaces.AndR(str ";")
    let CUT = whitespaces.AndR(str "!")
    let BAR = whitespaces.AndR(str "|")
    let IDENT = whitespaces.AndR(alpha_chars.And(alphanum_chars.Many().AsString()).Select(fun (x,y) -> x.ToString() + y))
    let ATOM = whitespaces.AndR(choice [
            IDENT.Where(fun x -> (islower x.[0]));
            non_alphanum_chars.Many1().AsString().Where(fun x -> x <> "." && x <> ",");
            SEMICOLON;
            CUT;
            BAR
        ])
    let VAR = whitespaces.AndR(IDENT.Where(fun x -> (isupper x.[0] || '_' = x.[0])))
    let NUM = whitespaces.AndR((char isdigit).Many1().AsString())

    let rec fact1 = lazy_(fun () -> choice [
                     (LBRACK).AndR(expr1).AndL(RBRACK);
            ATOM.AndL(LPAREN).And(expr2).AndL(RPAREN).Select(fun (x,y) -> Pred (x,y));
                     (LPAREN).AndR(expr3).AndL(RPAREN).Select(fun (x) -> Pred("",[x;Atom""]) );
            ATOM.Select(fun x -> Atom(x));
            VAR.Select(fun x -> Var(x, 0));
            NUM.Select(fun x -> Num(int x));
            //STR.Select(fun x -> Str(x));
            //OP.Select(fun x -> Atom(x));
        ])
    and fact2 = lazy_(fun () -> fact1)
    and fact3 = lazy_(fun () -> choice[
            COMMA.Select(fun _ -> Atom(","));
            fact2
        ])
    and term1 = lazy_(fun () -> 
        choice [
            fact1.And(term1).Select(fun (x,y) -> Pred("",[x;y]));
            fact1
        ])
    and term2 = 
        lazy_(fun () -> choice [
            fact2.And(term2).Select(fun (x,y) -> Pred("",[x;y]));
            fact2
        ])
    and term3 = 
        lazy_(fun () -> choice [
            fact3.And(term3).Select(fun (x,y) -> Pred("",[x;y]));
            fact3
        ])
    and expr1 = 
        lazy_(fun () -> choice [
            term1.AndL(COMMA).And(expr1).Select(fun (x,y) -> Pred("[|]",[x;y]) );
            term1.AndL(BAR).And(expr3).Select(fun (x,y) -> Pred("[|]",[x;y]) );
            term1.Select(fun x -> Pred("[|]",[x;Atom"[]"]) );
            empty().Select(fun _ -> Atom "[]")
        ])
    and expr2 = 
        lazy_(fun () -> choice [
            term2.AndL(COMMA).And(expr2).Select(fun (x,y) -> x::y );
            term2.Select(fun x -> [x] );
            empty().Select(fun _ -> [])
        ])
    and expr3 = 
        lazy_(fun () -> term3)
    and sentence = 
        lazy_(fun () -> choice [
            any().Not().Select(fun x -> Atom "" );
            expr3.AndL(DOT).Select(fun x -> opconvert x)
        ])

    let print_ret ret =
        match ret with
        | Success (_,v,_) -> print_ast v
        | Fail(_,(i,msg)) -> sprintf "Fail(pos=%d): %s" i msg
        
[<EntryPoint>]
let main argv = 
    Parser.sentence "p(X, bar, a(bar,X))." 0 (0,"") |> Parser.print_ret |> printfn "%A";
    Parser.sentence "?- p(foo, X, a(X,foo))." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence "p(X,Y) :- q(Y), r(X),s(Z)." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence "x is 1 + 2." 0 (0,"") |> Parser.print_ret|> printfn "%A";

    Parser.sentence "s(N,R) :- N=1,R=0." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence "s(N,R) :- N=2,R=2." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence "s(N,R) :- writeln(fail),N=2,!,fail." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence "s(N,R) :- N=1,writeln(fail2),!,fail." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence "s(N,R) :- writeln(fail3),!,fail." 0 (0,"") |> Parser.print_ret|> printfn "%A";
    Parser.sentence ":- s(1,R),writeln(R),writeln(r=R),R\=0,halt;writeln(ng),halt." 0 (0,"") |> Parser.print_ret|> printfn "%A";

    0
// https://github.com/hsk/wamo
// https://github.com/hsk/gdis_prolog
