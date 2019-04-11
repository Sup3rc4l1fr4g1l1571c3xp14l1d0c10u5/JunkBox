
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
        static member inline AsString(self: Parser<'T list>) = asString self 

type Term =
      Var of string
    | Term of string * (Term list)

type Clause = { pos : Term; neg : Term list }

let rec print_terme (term: Term) = 
    match term with
    | Var id -> id
    | Term (f, l) ->
        match l with
        | [] -> f
        | t::q -> sprintf "%s(%s%s)" f (print_terme t) (string_of_terme_list q)

and string_of_terme_list (l : Term list) =
    match l with
    | [] -> ""
    | t::q -> sprintf ", %s%s" (print_terme t) (string_of_terme_list q)

let print_clause (cl:Clause) =
    match cl.neg with
    | [] -> print_terme cl.pos
    | t::q -> sprintf "%s :- %s%s)" (print_terme cl.pos) (print_terme t) (string_of_terme_list q)
    
//////////

// 項 trm 中の 変数 x を 項 t に置換した結果を返す
let rec subst (x:string) (t:Term) (trm:Term) : Term =
    match trm with
    | Var p       -> if p = x then t else trm
    | Term (f, l) -> Term (f, List.map (subst x t) l)

// 置換列 l を 項 term に順に適用する
let rec app_subst (l:(string * Term) list) (trm:Term) =
    match l with
    | [] -> trm
    | (x, t)::q -> (app_subst q (subst x t trm))

// 置換列 s1 を 置換列 s2 に順に適用する
let rec apply_subst_on_subst s1 s2 =
    match s1 with
    | [] -> s2
    | (x, t)::q -> (apply_subst_on_subst q (List.map (fun (p, trm) -> (p, subst x t trm)) s2))

// 置換列 x が置換する変数を抽出する
let vars_subst (x:(string*Term) list) : string list = 
    List.map fst x

// 置換列のまとめ上げを行う
// 置換列 l1 のうち置換列 l2 に含まれていないもの と 置換列 l1 を 置換列 l2 に適用した結果を連結する
let rec compose (l1:(string*Term) list) (l2:(string*Term) list)= 
    let subst = vars_subst l2   // 置換列 l2 が置換する変数
    let r1 = List.filter (fun (x,y) -> not (List.contains x subst)) l1  // 置換列l1のうち置換列l2に含まれていないもの
    let r2 = apply_subst_on_subst l1 l2 // 置換列 l1 を 置換列 l2 に適用する　⇒　置換列 l2 中から置換列 l1 に含まれている置換が消える
    in r1 @ r2;;

// 変数 x が 項 y 中に出現するか？
let rec occurence (x:string) (y:Term) : bool =
    match y with
    | Var y -> x = y
    | Term (f, l) -> occurence_list x l

// 変数 x が 項リスト y 中に出現するか？
and occurence_list (x:string) (y:Term list) =
    match y with
    | [] -> false
    | t::q -> (occurence x t) || (occurence_list x q)

// 項 t1 と 項 t2 の単一化を行い、置換列を生成する 
let rec unification (t1:Term) (t2:Term) =
    // 項列同士の単一化を行う
    let rec unification_list l1 l2 =
        match (l1, l2) with
        | [], [] -> true, []
        | [], _  
        | _ , [] -> false, []
        | t1::q1, t2::q2 ->
            let (b1, s1) = unification t1 t2 in
                if b1 
                then
                    let (b2, s2) = (unification_list (List.map (app_subst s1) q1) (List.map (app_subst s1) q2)) 
                    in  if b2 
                        then true, (compose s2 s1)
                        else false, []
                else false, []
    in
        match (t1, t2) with
        | (Var x), (Var y) ->   
            if x = y 
            then true, []
            else true, [x, Var y]
        | (Var x), t
        | t, (Var x) ->
            if (occurence x t) 
            then false, []
            else true, [x, t]
        | (Term (f1, l1)), (Term (f2, l2)) ->
            if f1=f2 
            then unification_list l1 l2
            else false, []

let fresh (num:int) (cl:Clause) : Clause =
    let rec aux = function
        | Var x -> Var (sprintf "%s_%i" x num)
        | Term (f, l) -> Term (f, List.map aux l)
    in
        { pos = aux cl.pos; neg = List.map aux cl.neg }

let rec search_clauses num prog trm =
    match prog with
    | [] -> []
    | cl::q ->
        let fresh_cl = fresh num cl 
        let (b, s) = unification trm fresh_cl.pos 
        in
            if b 
            then (s, fresh_cl)::(search_clauses num q trm)
            else (search_clauses num q trm)

let rec yesOrNo mess =
    printf "%s (y/n) [y] : " mess;
    match (System.Console.ReadLine()) with
    | "y" -> true
    | "o" -> true
    | ""  -> true
    | "n" -> false
    | _   -> printfn "\n"; yesOrNo mess

let print_subst l =
    let rec aux = function
        | [] -> ""
        | (x, t)::q -> sprintf ",  %s = %s%s" x (print_terme t) (aux q)
    in
        match l with
        | [] -> "{ }"
        | (x, t)::q ->
            sprintf "{ %s = %s%s }" x (print_terme t) (aux q)

let display_subst s =
    printf "  %s\n" (print_subst s)

let prove_goals (maxoutput:int(*=10*)) (interactive:bool(*=true*)) (prog:Clause list) (trm_list:Term list) =
    let rec prove_goals_rec (maxoutput: int (*=10*)) (interactive: bool (*=true*)) (but:Term list) (num:int) prog s = function
        | [] ->
            display_subst (List.filter (fun (v, _) -> occurence_list v but) s);
            if interactive 
            then if not (yesOrNo "continue ?") 
                 then failwith "end"
            if maxoutput = 0 
            then failwith "end";
        | trm::q ->
            let ssButs = search_clauses num prog trm 
            in
                List.fold
                    (fun _ (s2, cl) ->
                       (prove_goals_rec (maxoutput-1) interactive but (num+1) prog
                          (compose s2 s)
                          (List.map (app_subst s2) (cl.neg @ q))))
                    ()
                    ssButs
    in
        try
            prove_goals_rec maxoutput interactive trm_list 0 prog [] trm_list
        with
            Failure "end" -> ()

module Parser = 
    open ParserCombinator
    open ParserCombinatorExt

    let whitespace = (char (fun ch -> " \r\t\n".IndexOf(ch) <> -1)).Many()

    let ident = 
        let isIdent = fun (ch) -> (('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z'));
        in  whitespace.AndR(char isIdent).Many1().AsString()

    let rec parse_term1 = 
        lazy_ ( fun () ->
            choice [
                ident.AndL(whitespace).AndL(str "(").And(parse_term_list).AndL(whitespace).AndL(str ")").Select(fun (f,t) -> Term (f, t));
                ident.AndL(whitespace).Select(fun (f) -> if ('A' <= f.[0] && f.[0] <= 'Z') then Var (f) else Term (f, []))
            ]
        )

    and parse_term_list = 
        parse_term1.And(whitespace.AndR(str ",").AndR(parse_term1).Many()).Select(fun (x, xs) -> x::(List.rev xs))
            
    let rec parse_clause1 = 
        choice [
            parse_term1.AndL(whitespace).AndL(str ".").Select( fun t -> {pos = t; neg = []} );
            parse_term1.AndL(whitespace).AndL(str ":-").And(parse_term_list).AndL(whitespace).AndL(str ".").Select( fun (t,l) -> {pos = t; neg = l} );
            parse_term1.AndL(whitespace).AndL(str "<--").And(parse_term_list).AndL(whitespace).AndL(str ".").Select( fun (t,l) -> {pos = t; neg = l} )
        ]

    let parse_progs =
        parse_clause1.Many1()

    let parse_goal = 
        parse_term_list

[<EntryPoint>]
let main argv = 
    let prog =  Parser.parse_progs "cat(tom).\nmouse(jerry).\nfast(X) <-- mouse(X).\nstupid(X) <-- cat(X).\n" 0 (0, "")
    let trm_list = Parser.parse_goal "stupid(X)." 0 (0, "")
    in
        match (prog, trm_list) with
            | (ParserCombinator.Success (p1,v1,m1),ParserCombinator.Success (p2,v2,m2)) -> prove_goals 10 true v1 v2 |> printfn "%A"; 0
            | _ -> 0
        