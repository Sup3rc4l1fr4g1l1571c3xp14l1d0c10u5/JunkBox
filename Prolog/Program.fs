
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

// Prologにおける 規則 
// head :- goal 
type Clause = { head : Term; goal : Term list }

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
    match cl.goal with
    | [] -> print_terme cl.head
    | t::q -> sprintf "%s :- %s%s)" (print_terme cl.head) (print_terme t) (string_of_terme_list q)
    
//////////

// 変数 :- 項 を示す代入
type Subst = (string * Term)

// 項 trm に 置換 sub を適用した結果を返す
let rec subst ((x,t) as sub:Subst) (trm:Term) : Term =
    match trm with
    | Var p       -> if p = x then t else trm
    | Term (f, l) -> Term (f, List.map (subst sub) l)

// 置換列 l を 項 term に順に適用する
let rec app_subst (ls :Subst list) (trm:Term) = 
    List.fold (fun s x -> subst x s) trm ls
//    match ls with
//    | []        -> trm
//    | x::xs -> (app_subst xs (subst x trm))

// 置換列 s1 を 置換列 s2 に順に適用する
let rec apply_subst_on_subst (s1:Subst list) (s2:Subst list) =
    List.fold (fun s x -> List.map (fun (p, trm) -> (p, subst x trm)) s) s2 s1
//    match s1 with
//    | [] -> s2
//    | x::xs -> (apply_subst_on_subst xs (List.map (fun (p, trm) -> (p, subst x trm)) s2))

// 置換列 x が置換する変数を抽出する
let vars_subst (x:Subst list) : string list = 
    List.map fst x

// 置換列のまとめ上げを行う
// 置換列 l1 の代入先変数うち置換列 l2 の代入先変数に含まれていないもの と 置換列 l1 を 置換列 l2 に適用した結果を連結する
// 
let rec compose (l1:Subst list) (l2:Subst list)= 
    let subst = vars_subst l2   // 置換列 l2 が置換する変数
    let r1 = List.filter (fun (x,y) -> not (List.contains x subst)) l1  // 置換列l1のうち置換列l2に含まれていないもの
    let r2 = apply_subst_on_subst l1 l2 // 置換列 l1 を 置換列 l2 に適用する　⇒　置換列 l2 中から置換列 l1 に含まれている置換が消える
    in r1 @ r2;;

// 変数 x が 項 y 中に出現するか？
let rec occurence (x:string) (y:Term) : bool =
    match y with
    | Var y -> x = y
    | Term (f, l) -> occurence_list x l

// 変数 x が 項リスト ys 中 に 出現するか？
and occurence_list (x:string) (ys:Term list) = 
    List.exists (fun t -> occurence x t) ys


let rec search_clauses (num:int) (prog:Clause list) (trm:Term) =
    // 項 t1 と 項 t2 の単一化を行い、代入列を生成する 
    let rec unification (t1:Term) (t2:Term) : Subst list option =

        // 項列同士の単一化を行う
        let rec unification_list (l1:Term list) (l2:Term list) : Subst list option =
            match (l1, l2) with
            | [], [] -> Some []
            | [], _  
            | _ , [] -> None
            | t1::q1, t2::q2 ->
                let s1 = unification t1 t2 in
                    if s1.IsSome 
                    then
                        let s2 = (unification_list (List.map (app_subst s1.Value) q1) (List.map (app_subst s1.Value) q2)) 
                        in  if s2.IsSome
                            then Some (compose s2.Value s1.Value)
                            else None
                    else None
        in

            match (t1, t2) with
            | (Var x), (Var y) -> // 変数同士の単一化
                if x = y 
                then Some []           // 同一なので単一化はできるが代入は生成しない
                else Some [x, Var y]   // 同一変数ではないので代入 Var x = Var y を作る
            | (Var x), t
            | t, (Var x) ->         // 変数と項の単一化
                if (occurence x t) 
                then None      // 出現する場合は単一化できない
                else Some [x, t]   // 出現しないので 代入 : Var x = Term t を生成
            | (Term (f1, l1)), (Term (f2, l2)) ->
                if f1=f2 
                then unification_list l1 l2
                else None


    let fresh (num:int) (cl:Clause) : Clause =
        let rec aux = function
            | Var x -> Var (sprintf "%s_%i" x num)
            | Term (f, l) -> Term (f, List.map aux l)
        in
            { head = aux cl.head; goal = List.map aux cl.goal }

    in
        match prog with
        | [] -> []
        | cl::q ->
            let fresh_cl = fresh num cl 
            let s = unification trm fresh_cl.head 
            in
                if s.IsSome 
                then (s.Value, fresh_cl)::(search_clauses num q trm)
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
                          (List.map (app_subst s2) (cl.goal @ q))))
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
            parse_term1.AndL(whitespace).AndL(str ".").Select( fun t -> {head = t; goal = []} );
            parse_term1.AndL(whitespace).AndL(str ":-").And(parse_term_list).AndL(whitespace).AndL(str ".").Select( fun (t,l) -> {head = t; goal = l} );
            parse_term1.AndL(whitespace).AndL(str "<--").And(parse_term_list).AndL(whitespace).AndL(str ".").Select( fun (t,l) -> {head = t; goal = l} )
        ]

    let parse_progs =
        parse_clause1.Many1()

    let parse_goal = 
        parse_term_list

[<EntryPoint>]
let main argv = 
    let prog =  Parser.parse_progs "cat(tom).
                                    mouse(jerry).
                                    fast(X) <-- mouse(X).
                                    stupid(X) <-- cat(X).
                                    " 0 (0, "")
    let trm_list = Parser.parse_goal "stupid(X)." 0 (0, "")
    in
        match (prog, trm_list) with
            | (ParserCombinator.Success (p1,v1,m1),ParserCombinator.Success (p2,v2,m2)) -> prove_goals 10 true v1 v2 |> printfn "%A"; 0
            | _ -> 0
        