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
            else fail pos (sprintf "str: require is \"%s\" but get %s." s (if (str.Length > pos) then str.[pos].ToString() else "EOS")) max

    let any () = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            if str.Length > pos 
            then succ (pos+1) str.[pos]  max
            else fail pos "any: require any char but get EOS." max

    let eos () = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            if str.Length > pos 
            then fail pos (sprintf "eos: require EOS but get %c." str.[pos]) max
            else succ (pos) ()  max

    let empty () = 
        fun (str:string) (pos:int) (max:MaxPosition)  -> 
            succ (pos) ()  max

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

module KeyValueList =
    type Type<'key,'value> = ('key * 'value) list
    let empty = []
    let assoc (key : 'key) (kvs : Type<'key,'value>) : 'value option =
        let rec loop kvs =
            match kvs with
            | [] -> None
            | (k,v)::xs -> if (k = key) then Some v else loop xs
        in  loop kvs

    let update (key : 'key) (value : 'value) (kvs : Type<'key,'value>) : Type<'key,'value> =
        let rec loop kvs =
            match kvs with
            | [] -> [(key,value)]
            | (k,_)::xs when key=k -> (key,value)::xs
            | (k,v1)::xs -> (k,v1)::(loop xs)
        in  loop kvs

module Ast =
    type Type = 
          Pred of string * (Type list)
        | Atom of string
        | Num of int
        | Var of (string * int)

    let rec ToString ast =
        match ast with
        | Pred (x, xs) -> sprintf "(%s %s)" x (List.map ToString xs |> String.concat " ")
        | Atom v -> v 
        | Num v -> v.ToString()
        | Var (v,i) -> sprintf "%s{%d}" v i

module Operator =
    open Ast
    type EntryType = (int * string * (string list))

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



    (* operator perecedences *)
    let add maps (p:int) (o:string) (op:string) =
        let subtable = match KeyValueList.assoc o maps with | None -> KeyValueList.empty | Some v -> v
        let subtable = (op,p)::subtable
        in  KeyValueList.update o subtable maps
        
    let addRange maps ((p:int), (o:string), (ls : string list)) =
        List.fold(fun s op -> add s p o op) maps ls

    let opsmap = ref (List.fold addRange KeyValueList.empty default_ops)

    let regist op = opsmap := addRange !opsmap op

    // get operator's priolity
    let get_priolity defaultp os op =
        let rec loop os =
            match os with
            | []     -> defaultp
            | o::os  ->
                match  KeyValueList.assoc o !opsmap with
                | None -> loop os
                | Some subtable -> 
                    match KeyValueList.assoc op subtable with 
                    | None -> loop os
                    | Some v -> v
        in loop os

    let opnFx  = get_priolity (-1) ["fx";"fy"]
    let opnXf  = get_priolity (-1) ["xf";"yf"]
    let opnXfy = get_priolity (-1) ["xfy";"xfx"]
    let opnYfx = get_priolity (-1) ["yfx"]

    let prefixs  = get_priolity 10001 ["fx";"fy"]
    let postfixs = get_priolity 10001 ["xf";"yf"]
    let infixrs  = get_priolity 10001 ["xfy";"xfx"]
    let infixs   = get_priolity 10001 ["yfx"]

    let rec opconvert e =
        fst (opconvert_pre 10000 e)
    and opconvert_pre p e = 
        match e with
        | Pred("",[Atom(op);y]) when prefixs op <= p -> 
            // opは有効な優先順位を持つ前置演算子
            let (t,ts) = opconvert_pre (prefixs op) y // 同じ優先度でyに前置演算子解析を適用
            in (Pred(op,[t]),ts)
        | Pred("",[Pred("",xs);y]) -> 
            // 述語の場合、xsに前置演算子解析を適用、yには後置演算子解析を適用
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
    open Ast

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
    let QUOTEDATOM = 
        let ch  = (anychar "'\\").Not().AndR(any())
        let ech = (str "\\").AndR( anychar "\\/bfnrt'").Select(fun x -> match x with | '\\' -> x | '/' -> x | 'b' -> '\b' | 'f' -> '\f' | 'n' -> '\n' | 'r' -> '\r' | 't' -> '\t' | '\'' -> x)
        in  whitespaces.And(str "'").AndR((choice[ch;ech]).Many().AsString()).AndL(str "'")
    let IDENT = whitespaces.AndR(alpha_chars.And(alphanum_chars.Many().AsString()).Select(fun (x,y) -> x.ToString() + y))
    let ATOM = whitespaces.AndR(
                    choice [
                        IDENT.Where(fun x -> (islower x.[0]));
                        non_alphanum_chars.Many1().AsString().Where(fun x -> x <> "." && x <> ",");
                        QUOTEDATOM;
                        SEMICOLON;
                        CUT;
                        BAR
                    ]
                )
    let VAR = whitespaces.AndR(IDENT.Where(fun x -> (isupper x.[0] || '_' = x.[0])))
    let NUM = whitespaces.AndR((char isdigit).Many1().AsString())

    let rec fact1 = 
        lazy_(fun () -> 
            choice [
                         (LBRACK).AndR(expr1).AndL(RBRACK);
                ATOM.AndL(LPAREN).And(expr2).AndL(RPAREN).Select(fun (x,y) -> Pred (x,y));
                         (LPAREN).AndR(expr3).AndL(RPAREN).Select(fun (x) -> Pred("",[x;Atom""]) );
                ATOM.Select(fun x -> Atom(x));
                VAR.Select(fun x -> Var(x, 0));
                NUM.Select(fun x -> Num(int x));
                //STR.Select(fun x -> Str(x));
                //OP.Select(fun x -> Atom(x));
            ]
        )
    and fact2 = 
        lazy_(fun () -> fact1)
    and fact3 = 
        lazy_(fun () -> 
            choice[
                COMMA.Select(fun _ -> Atom(","));
                fact2
            ]
        )
    and term1 = 
        lazy_(fun () -> 
            choice [
                fact1.And(term1).Select(fun (x,y) -> Pred("",[x;y]));
                fact1
            ]
        )
    and term2 = 
        lazy_(fun () -> 
            choice [
                fact2.And(term2).Select(fun (x,y) -> Pred("",[x;y]));
                fact2
            ]
        )
    and term3 = 
        lazy_(fun () -> 
            choice [
                fact3.And(term3).Select(fun (x,y) -> Pred("",[x;y]));
                fact3
            ]
        )
    and expr1 = 
        lazy_(fun () -> 
            choice [
                term1.AndL(COMMA).And(expr1).Select(fun (x,y) -> Pred("[|]",[x;y]) );
                term1.AndL(BAR).And(expr3).Select(fun (x,y) -> Pred("[|]",[x;y]) );
                term1.Select(fun x -> Pred("[|]",[x;Atom"[]"]) );
                empty().Select(fun _ -> Atom "[]")
            ]
        )
    and expr2 = 
        lazy_(fun () -> 
            choice [
                term2.AndL(COMMA).And(expr2).Select(fun (x,y) -> x::y );
                term2.Select(fun x -> [x] );
                empty().Select(fun _ -> [])
            ]
        )
    and expr3 = 
        lazy_(fun () -> term3)
    and sentence = 
        lazy_(fun () -> 
            choice [
                whitespaces.And(eos()).Select(fun x -> Atom "" );
                expr3.AndL(DOT)
            ]
        )
    and query = 
        lazy_(fun () -> 
            expr3.AndL(DOT).Select(fun x -> Operator.opconvert x)
        )

    let print_ret ret =
        match ret with
        | Success (_,v,_) -> Ast.ToString v
        | Fail(_,(i,msg)) -> sprintf "Fail(pos=%d): %s" i msg

module DB =
    open Ast

    type Type = (Ast.Type * int) array    

    let empty () = 
        Array.copy [|
            Atom"freelist",0;
            Atom"start",3;
            Atom"end",2;
            (*Pred(":-",[Atom "fail";Atom "false"]),0;*)
        |]
    let dump db =
        let i = ref 0
        in
            Array.iter(fun (p,n) ->
                printfn "%d : %s %d" !i (Ast.ToString p) n;
                i := !i + 1
            ) db

    let get_free (db:Type):int = snd db.[0]
    let get_start (db:Type) = snd db.[1]
    let get_end (db:Type) = snd db.[2]
    let id = ref 0
    let newvar() = incr id; Var(sprintf "%%%d" !id,1)

    let copy_term p =
        let t = System.Collections.Generic.Dictionary<(string * int), Ast.Type>(10)
        let rec loop = 
            function
            | Var (a,i) when t.ContainsKey (a,i) -> t.[(a,i)]
            | Var (a,i)                          -> let v = newvar() in t.[(a,i)] <- v; v
            | Pred(a,ls)                         -> Pred(a,List.map loop ls)
            | a -> a
        in
            let p2 = loop p 
            in    (*printfn "copy_term %s to %s" (show p) (show p2);*)
                p2

    let new_free (db:Type) p n : (int * Type) =
        let p = copy_term p
        let free = get_free db
        in
            if free = 0 
            then (Array.length db, Array.append db [|p,n|])
            else failwith "error use free datas"
            

    let asserta db p =
        let startp = get_start db
        let (newp,db) = new_free db p startp
        in
            db.[1] <- (fst db.[1],newp);
            db
        
    let assertz (db:Type) p =
        let endp = get_end db
        let (newp,db) = new_free db p 0
        in
            db.[endp] <- (fst db.[endp], newp);
            db.[2]    <- (fst db.[2]   , newp);
            db

    let remove (db:Type) n =
        db.[n] <- (Num (int (snd db.[0])), snd db.[n]);
        db.[0] <- (Num (int n           ), snd db.[0]);
        db

    let retract (db:Type) (f:Ast.Type->bool) =
        let rec loop i (db:Type) =
            if i = 0 
            then db 
            else let (t,n) = db.[i] in if (f t) then remove db i else loop n db
        in
            loop (get_start db) db

    let retractall (db:Type) f =
        let rec loop i (db:Type) =
            if i = 0 
            then db 
            else let (t,n) = db.[i] in loop n (if (f t) then remove db i else db)
        in
            loop (get_start db) db

module VM =
    open Ast

    type Env = KeyValueList.Type<(string * int),Ast.Type>    (* env *)
    type Result = Env option   (* result *)

    let rec deref e t = 
        match t with
        | Pred (n, ts) -> Pred(n, List.map (deref e) ts)
        | Var  v       -> match (KeyValueList.assoc v e) with None -> t | Some x -> deref e x
        | t            -> t

    let show e =
        let tos ((n, l), t) ls =
            if l >= 1 
            then ls 
            else (sprintf "%s=%s\n" n (Ast.ToString (deref e t))) + ls
        in List.foldBack tos e ""


    let rec unify e t1 t2 =
        let rec unify r (t1, t2) = 
            match r with
            | None -> None
            | Some e ->
                let rec bind t1 v t2 =
                    match KeyValueList.assoc v e with
                    | Some (Var v as t3) -> if t1 = t3 then None else bind t1 v t2
                    | Some (t3         ) -> if t2 = t3 then r    else mgu (t3, t2)
                    | None               -> Some((v, t2) :: e)
                and mgu (t1, t2) = 
                    match (t1, t2) with
                    |          t1 ,      Var v2  -> bind t2 v2 t1
                    |       Var v ,          t2  -> bind t1 v t2
                    | Pred(x1, g1), Pred(x2, g2) -> if x1 <> x2 then None else if List.length g1 = List.length g2 then List.fold unify r (List.zip g1 g2) else None
                    |          t1 ,          t2  -> if t1 = t2 then r else None
                in mgu (t1, t2)
        in unify (Some e) (t1, t2)

    type Goals    = Ast.Type list                       (* goals *)
    type Database = (Ast.Type * int)array               (* database *)
    type Index    = int                                 (* index *)
    type Stack    = (Goals * Env * Index * Index) list  (* stack *)
    type Machine  = Goals * Database * Index * Stack    (* gdis machine *)

    type ('a, 'b) res = Fail of 'a | Succ of 'b
    let trace = ref false
    let interactive = ref false
    
    let stack_getEnv (s : Stack) = 
        match s with
        | [] -> []
        | (_, e, _, _)::_ -> e

    let el1 s = 
        match s with
        | [] -> [],1
        | (_, e, l, _)::_ -> e, l+1

    let pop m = 
        match m with
        |   _,  d, _,              [] -> Fail d
        |   _,  d, _, ( g, _,_, i)::s -> Succ (g, d, i, s)
    
    let rec pop1 l1 d s = 
        match s with
        | [] | [_]     -> Fail d
        | (g,e,l,i)::s -> Succ(g,d,0, s)

    let backtrack d s = 
        match s with
        | []->Fail d
        | (_,e,l,i)::s -> pop1 l d s

    let uni m s t t2 =
        match unify (stack_getEnv s) t t2, m with
        | Some e, (_::g, d, _, (sg, _,l, i)::s) -> Succ (g, d, -1, (sg, e, l, i) :: s)
        |      _,                           m -> pop m

    let arity = 
        function
        | Atom a -> sprintf "%s/0" a
        | Pred(a,ts)-> sprintf "%s/%d" a (List.length ts)
        | _ -> "none"

    let params = 
        function
        | Atom a -> []
        | Pred(a,ts)-> ts
        | _ -> failwith "bad params"

    type BuiltinFunc = (Ast.Type list) -> Goals -> Database -> Stack -> Machine -> res<Database, Machine>
    
    let builtins : KeyValueList.Type<string, BuiltinFunc> ref = ref []

    let step = function
      | Fail d     -> Fail d
      | Succ (g,d,i,s as m) ->
            if !trace then printfn "i=%d g=[%s],d=%d,e=[%s],s=%d" i (String.concat "; " (List.map Ast.ToString g)) (Array.length d) (show (stack_getEnv s)) (List.length s)
            match m with
            | (           [], d,  i, s) -> Succ m
            | (            g, d, -2, s) -> backtrack d s
            | (Pred(a,[])::g, d,  i, s) -> Succ(Atom a::g,d,i,s)
            | (         t::g, d, -1, s) when (KeyValueList.assoc (arity t) !builtins).IsSome -> (KeyValueList.assoc (arity t) !builtins).Value (params t) g d s m
            | (            g, d, -1, s) -> Succ(g, d, DB.get_start d, s)
            | (         t::g, d,  i, s) ->
                let rec loop i =
                    if i=0 || Array.length d = 4 then pop m else (* todo この Array.length d = 4 は消したい *)
                    if Array.length d <= i 
                    then Fail d 
                    else
                        match d.[i] with
                        | (Pred(":-", [t2; t3]),nx) ->
                            let e,l1 = el1 s 
                            let rec gen_t = 
                                function
                                | Pred(n, ts) -> Pred(n, List.map (fun a -> gen_t a) ts)
                                | Var(n, _)   -> Var(n, l1)
                                | t -> t
                            in
                                match unify e t (gen_t t2) with
                                | None   -> loop nx
                                | Some e -> Succ(gen_t t3::g, d,    -1, (t::g, e, l1, nx) :: s)
                        | (_,nx) -> loop nx
                in loop i

    (* マシンを受取り、ステップ実行を停止状態まで動かし、マシンを返す *)
    let rec solve m =
        let m = 
            match m with
            | [], _, _, _ -> pop m
            | _           -> Succ m
        in
            match step m with
            | (Succ ([],d,i,s) ) as m -> 
                if !trace then printfn "i=%d g=[],d=%d,e=[%s],s=%d" i (Array.length d) (show (stack_getEnv s)) (List.length s) else ();
                m
            | Succ m -> solve m
            | Fail d -> Fail d

    let rec prove m = 
        match solve m with
        | Fail d -> printfn "false"; d
        | Succ (g, d, i, s as m) -> d

    let rec iprove m = 
        match solve m with
        | Fail d -> printfn "false"; d
        | Succ (g, d, i, s as m) ->
            printf "%s" (show (stack_getEnv s));
            if List.length s <= 1 then d 
            else if ";" <> System.Console.ReadLine () then (printfn "true"; d) 
            else iprove m

    (* DBと項を受取って、マシンの初期設定をして実行し結果のDBを返す *)
    let process d t =
        if !interactive 
        then iprove ([t], d, -1, [[],[],1,-2])
        else  prove ([t], d, -1, [[],[],1,-2])

    let solve1 d t = solve ([t],d,-1,[[],[],1,-2])        
        
module Builtin =
    open Ast
    open VM

    let libpath = ref "./lib/" // "/usr/share/gdispl/lib/"

    let uninot m s t t2 =
        match unify (stack_getEnv s) t t2, m with
        | Some _, m -> pop m
        |      _, (_::g,d,_,s) -> Succ(g,d,-1,s)
        |      _, ([],d,_,s) -> Fail d

    let rec eval e = function
        | Num i -> i
        | Pred("+", [x;y]) -> (eval e x) + (eval e y)
        | Pred("*", [x;y]) -> (eval e x) * (eval e y)
        | Pred("-", [x;y]) -> (eval e x) - (eval e y)
        | Pred("/", [x;y]) -> (eval e x) / (eval e y)
        | t -> failwithf "unknown term %s" (Ast.ToString t)

    let write1 e t = printf "%s" (Ast.ToString (deref e t))

    let call t g d i s =
        let call1 t =
            let e,l1  = el1 s in
            Succ(t::g,d,i,(g,e,l1,0)::s)
        in
        match List.map (fun t -> deref (stack_getEnv s) t) t with
        | Atom a::ts -> call1 (Pred(a,ts))
        | Pred(a,ts1)::ts -> call1 (Pred(a,ts1@ts))
        | p -> Fail(d)

    let to_db = function
        | Pred(":-",[_;_]) as t -> t
        | t               -> Pred(":-",[t;Atom "true"])

    let retract retractf d t e =
        let t = to_db t in
        retractf d (fun dt ->
            match dt,t with
            | (Pred(":-",[dt;_]),Pred(":-",[t;_])) ->
                (match unify e t dt with Some _ -> true | None -> false)
            | _ -> false
        )

    let retract1 = retract DB.retract
    let retractall = retract DB.retractall

    let rec to_list = function
        | Atom "[]" -> []
        | Pred("[|]",[a;b]) -> a::to_list b
        | c -> [c]

    let opadd s p a ls =
        let f a =
            match deref (stack_getEnv s) a with
            | Atom a -> a
            | _ -> failwith "badop"
        let ls = List.map f (to_list ls) in
        match deref (stack_getEnv s) p, deref (stack_getEnv s) a, ls with
        | (Num p, Atom a, ls) -> Operator.regist(int p, a, ls)
        | _ -> ()

    let assertz d t = DB.assertz d (to_db t)
    let asserta d t = DB.asserta d (to_db t)
    let current_predicate = fun [u] g d s m ->
        let p = (Ast.ToString (deref (stack_getEnv s) u)) in
        if (KeyValueList.assoc p !builtins).IsSome || Array.toList d |>List.exists(function | (Pred(":-",[u;v]),_)->p=arity u |(p,_)->false )
        then Succ(g,d,-1,s) 
        else Fail d

    let read d t =
        let filename = 
            match t with
            | Atom a -> a
            | Pred("library",[Atom t]) -> sprintf "%s/%s.pl" !libpath t
            | _ -> failwith "loading file path error"
        let _ = if !trace then printfn "Loading %s" filename else ()
        let inp = System.IO.File.ReadAllText filename 
        let rec loop n =
            match Parser.sentence inp n (n,"")  with
            | ParserCombinator.Fail (_, (i ,s)) -> printfn "fail(%d): %s" i s; Atom "[]"
            | ParserCombinator.Success (_, Atom "", _) -> Atom "[]"
            | ParserCombinator.Success (n,p,_) -> Pred("[|]",[DB.copy_term p;loop n])
        in  loop 0 

    let read2 m g d s t u =
        let ps = read d (deref (stack_getEnv s)t) in
        uni m s u ps

    let consult d t =
        match solve1 d (Pred("current_predicate",[Pred("/",[Atom"consult";Num 1])])) with
        | Fail d -> 
            let rec loop d = 
                function
                | Atom "[]" -> d
                | Pred("[|]",[p;ps]) -> loop (assertz d (Operator.opconvert p)) ps
            in
                loop d (read d t)
        | Succ(g,d,i,s)->
            match solve1 d (Pred("consult",[t])) with
            | Succ(g,d,i,s) -> d
            | Fail d -> printfn "false"; d

    let not1 g d s = function
        | Fail d -> Succ(g,d,-1,s)
        | Succ(_,_,_,_) -> Fail d

    let univ m s a b =
        let rec list2pred = function
            | [] -> Atom "[]"
            | x::xs -> Pred("[|]",[x;list2pred xs])
        let rec pred2list = function
            | Atom "[]" -> [] 
            | Pred("[|]",[x; xs]) -> x::pred2list xs
        in
            match deref (stack_getEnv s) a, deref (stack_getEnv s) b,m with
            | Var _,Var _, (_,d,_,_) -> Fail d
            | (Var _) as t, Pred("[|]",[Atom a;b]), m -> uni m s t (Pred(a,pred2list b))
            | Pred(a,ts), t, m -> uni m s t (Pred("[|]",[Atom a;list2pred ts]))
            | Atom "[]", t, m -> uni m s t (Pred("[|]",[Atom "[]";Atom"[]"]))
            | _,_,(_,d,_,_) -> Fail d

    VM.builtins := 
        [
            "halt/0"         , (fun [] g d s m -> exit 0; Fail d);
            "true/0"         , (fun [] g d s m -> Succ(g,d,-1,s));
            "!/0"            , (fun [] g d s m -> match s with (g2,e,l,_)::s -> Succ(g, d, -1, (g2, e,l, -2)::s) | _->Fail d);
            ",/2"            , (fun [u;v] g d s m -> Succ(u::v::g, d, -1, s));
            ";/2"            , (fun [u;v] g d s m -> let e,l1=el1 s in Succ(u::g, d, -1, (v::g, e,max(l1-1)1, -1)::s));
            "=/2"            , (fun [u;v] g d s m -> uni m s u v);
            "\\=/2"          , (fun [u;v] g d s m -> uninot m s u v);
            "\\+/1"          , (fun [u] g d s m -> not1 g d s (step(Succ([u], d, -1, []))));
            "is/2"           , (fun [u;v] g d s m -> uni m s u (Num(eval (stack_getEnv s) (deref (stack_getEnv s) v))));
            "assertz/1"      , (fun [t] g d s m -> Succ(g, assertz d (deref (stack_getEnv s) t), -1, s));
            "asserta/1"      , (fun [t] g d s m -> Succ(g, asserta d (deref (stack_getEnv s) t), -1, s));
            "write/1"        , (fun [t] g d s m -> write1 (stack_getEnv s) t; Succ(g,d,-1,s));
            "retract/1"      , (fun [t] g d s m -> Succ(g,retract1 d t (stack_getEnv s),-1,s));
            "retractall/1"   , (fun [t] g d s m -> Succ(g,retractall d t (stack_getEnv s),-1,s));
            "read/2"         , (fun [t;u] g d s m -> read2 m g d s t u);
            "read/1"         , (fun [t] g d s m -> Succ(g, consult d (deref (stack_getEnv s) t), -1, s));
            "integer/1"      , (fun [t] g d s m -> (match deref (stack_getEnv s) t with Num _->Succ(g, d,-1,s)|_->pop m));
            "atom/1"         , (fun [t] g d s m -> (match deref (stack_getEnv s) t with Atom _->Succ(g, d,-1,s)|_->pop m));
            "var/1"          , (fun [t] g d s m -> (match deref (stack_getEnv s) t with Var _->Succ(g, d,-1,s)|_->pop m));
            "op/3"           , (fun [p;m;ls] g d s _ -> opadd s p m ls; Succ(g,d,-1,s));
            "=../2"          , (fun [a;b] g d s m -> univ m s a b);
            "call/1"         , (fun t g d s m -> call t g d (-1) s);
            "call/2"         , (fun t g d s m -> call t g d (-1) s);
            "call/3"         , (fun t g d s m -> call t g d (-1) s);
            "call/4"         , (fun t g d s m -> call t g d (-1) s);
            "call/5"         , (fun t g d s m -> call t g d (-1) s);
            "call/6"         , (fun t g d s m -> call t g d (-1) s);
            "call/7"         , (fun t g d s m -> call t g d (-1) s);
            "call/8"         , (fun t g d s m -> call t g d (-1) s);
            "opconvert/2"    , (fun [u;v] g d s m -> let u = Operator.opconvert (deref (stack_getEnv s) u) in uni m s (deref (stack_getEnv s) u) (deref (stack_getEnv s) v) );
            "current_predicate/1", current_predicate;
        ]
let help () =
    List.iter (fun (k,v) -> printfn "%s\t%s" k v) ["e","exit"; "l","list"; "h","help";]

let rec repl (d:DB.Type) =
    printf("?- ");
    match System.Console.ReadLine() with
    | "e"  -> ()
    | "l"  -> Array.iter (fun (t,_) -> printfn "%s." (Ast.ToString t)) d; repl d
    | "h"  -> help (); repl d
    | "t"  -> VM.trace := not !VM.trace;
              printfn "Tracing %s." (if !VM.trace then "on" else "off");
              repl d
    | line -> match Parser.query line 0 (0, "") with
              | ParserCombinator.Success (_,v,_) -> repl (VM.process d v)
              | ParserCombinator.Fail (_,(i,s))-> printfn "Syntax error (%d): %s" i s; repl d

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

    (* load files *)
    let files = ref []
    let db = ref (Builtin.consult (DB.empty ()) (Ast.Pred("library",[Ast.Atom "initial"]))) 
    in 
        !files|> List.iter (fun x -> db := Builtin.consult !db (Ast.Atom x));
        repl !db;
        0
// https://github.com/hsk/wamo
// https://github.com/hsk/gdis_prolog
