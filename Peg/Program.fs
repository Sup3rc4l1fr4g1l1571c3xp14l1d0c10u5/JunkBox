// 継続方式PEGVM

module String =
    (* 文字エスケープ *)
    let escape c =
        match c with
        | '\\'      -> "\\\\"
        | '"'       -> "\\\""
        | '\u0000'  -> "\\0"
        | '\b'      -> "\\b"
        | '\t'      -> "\\t"
        | '\n'      -> "\\n"
        | '\v'      -> "\\v"
        | '\f'      -> "\\f"
        | '\r'      -> "\\r"
        | _         -> if '\u0020' <= c && c <= '\u007E'
                       then c.ToString()
                       else sprintf "\\u%04x" (System.Char.ConvertToUtf32(c.ToString(),0))

    (* 文字列エスケープ *)
    let escapeString c =
        match c with
        | '\\'      -> "\\\\"
        | '"'       -> "\\\""
        | _         -> escape c

module VM =

    (* 命令 *)
    module Instruction =
        type t  = Char  of char
                | Any
                | Set   of char list
                | Jmp   of int
                | Call  of int ref
                | Match
                | Split of int * int
                | Join  of int
                | Over
                | Roll
                | Fail
                | End

        let to_s inst =
            match inst with
            | t.Char  c             -> sprintf "(Char \"%s\")" (String.escapeString c)
            | t.Any                 -> "(Any)"
            | t.Set   cs            -> sprintf "(Set %s)" (List.map (fun c -> (sprintf "\"%s\"" (String.escapeString c))) cs |> String.concat " ")
            | t.Jmp   x             -> sprintf "(Jmp %d)" x
            | t.Call  {contents=x}  -> sprintf "(Call %d)" x
            | t.Match               -> "(Match)"
            | t.Split (x,y)         -> sprintf "(Split %d %d)" x y
            | t.Join  x             -> sprintf "(Join %d)" x
            | t.Over                -> "(Over)"
            | t.Roll                -> "(Roll)"
            | t.Fail                -> "(Fail)"
            | t.End                 -> "(End)"
        


    (* キャプチャ環境 *)
    module Environment = 
        type t = Group of (* 規則アドレス *) int * (* 開始文字位置 *) int * (* 終了文字位置 *) int * (* 入れ子環境 *) t * (* リンクリスト *) t 
               | Empty

        (* リンクリスト部のみ反転 *)
        let reverse env =
            let rec loop e r =
                match e with
                | Empty -> r
                | Group (rule, sp, ep, e1, e2) -> loop e2 (Group (rule,sp,ep,e1,r))
            in loop env Empty

    (* 継続 *)
    module Continuation =
        type t = Fail of (* 再試行の開始命令位置 *) int * (* 文字ポインタ *) int * (* キャプチャ環境 *) Environment.t * (* 継続 *) t 
               | Succ of (* 再試行の開始命令位置 *) int * (* 文字ポインタ *) int * (* キャプチャ環境 *) Environment.t * (* 継続 *) t 
               | Call of (* リターンする命令位置 *) int * (* 文字ポインタ *) int * (* キャプチャ環境 *) Environment.t * (* 継続 *) t 
               | Empty

    (* コンテキスト *)
    module Context =
        type t 
            = (* 命令ポインタ / Fail *) int option * (* 文字ポインタ *) int * (* キャプチャ環境 *) Environment.t * (* 継続 *) Continuation.t
    
    (* 仮想マシンの実行 *)
    let apply (ops : Instruction.t list) ip str sp =
        let step (context : Context.t) =
            let (op, ip, sp, env, cont) = 
                match context with
                | (Some ip, sp, env, cont) -> (        ops.[ip], ip, sp, env, cont)
                | (None   , sp, env, cont) -> (Instruction.Fail, -1, sp, env, cont)
            in
                match op with
                | Instruction.Char ch -> 
                    if sp >= (String.length str) || (str.[sp] <> ch) 
                    then (None       , sp  , env, cont) 
                    else (Some (ip+1), sp+1, env, cont)
                | Instruction.Any -> 
                    if sp >= (String.length str) 
                    then (None       , sp  , env, cont) 
                    else (Some (ip+1), sp+1, env, cont)
                | Instruction.Set chs -> 
                    if sp >= (String.length str) || (List.contains str.[sp] chs) = false 
                    then (None       , sp  , env, cont) 
                    else (Some (ip+1), sp+1, env, cont)
                | Instruction.Jmp x ->
                    (Some (ip+x), sp, env, cont)
                | Instruction.Call {contents=x} ->
                    (Some (x), sp, Environment.Empty, Continuation.Call (ip+1, sp, env, cont))
                | Instruction.Match -> 
                    match cont with
                    | Continuation.Call (ip',sp',env',cont')  -> 
                        match env with
                        | Environment.Group(_x,_sp,_ep,_e1,_e2) -> (Some ip', sp,  Environment.Group (_x, sp', sp, Environment.reverse env, env'), cont')
                        | Environment.Empty                     -> (Some ip', sp,  Environment.Group (-1, sp', sp, Environment.Empty      , env'), cont')
                    | _                                         -> failwith "Current continuation is not Continuation.Call."
                | Instruction.Split (x,y) ->
                    (Some (ip+x), sp, env, Continuation.Fail (ip+y, sp, env, cont))
                | Instruction.Join x -> 
                    match cont with
                    | Continuation.Fail (_,_,_,cont') -> (Some (ip+x), sp, env, cont')
                    | _                               -> failwith "Current continuation is not Continuation.Fail."
                | Instruction.Over ->
                    (Some (ip+1), sp, env, Continuation.Succ (ip+1, sp, env, cont))
                | Instruction.Roll -> 
                    match cont with
                    | Continuation.Succ (ip',sp',env',cont') -> (Some (ip+1), sp', env', cont')
                    | Continuation.Fail (ip',sp',env',cont') -> (None       , sp , env , cont')
                    | _                                      -> failwith "Current continuation is not Continuation.Fail or Continuation.Succ."
                | Instruction.Fail -> 
                    match cont with
                    | Continuation.Fail (ip',sp',env',cont') -> (Some ip', sp', env', cont')
                    | Continuation.Succ (ip',sp',env',cont') -> (None    , sp , env , cont')
                    | Continuation.Call (ip',sp',env',cont') -> (None    , sp , env , cont')
                    | Continuation.Empty                     -> (None    , sp , env , cont )
                | Instruction.End ->
                    (Some ip, sp, env, Continuation.Empty)

        let rec loop (context : Context.t) = 
            match context with
            | (_, _, _, Continuation.Empty)  -> context
            | _                              -> step context |> loop

        in  loop (Some ip, sp, Environment.Empty, Continuation.Call ((List.length ops)-1, sp, Environment.Empty, Continuation.Empty))


(* 構文木 *)
module Ast =
    (* 要素式 *)
    type Expr 
        = Char of char
        | Str of string
        | Seq of Expr list
        | Choice of Expr list
        | Option of Expr
        | ZeroOrMany of Expr
        | OneOrMany of Expr
        | TryTrue of Expr
        | TryFalse of Expr
        | Call of string

    (* ルール *)
    type Rule 
        = string * Expr

    (* 文法 *)
    type Grammer 
        = Rule list

(* ルール表 *)
module RuleTable =
    type t = ((* name *) string * (* address *) int ref * (* size *) int ref) list
    let search_by_address table address = List.tryFind (fun (_,address',_) -> !address' = address) table
    let search_by_name    table name    = List.tryFind (fun (name',_,_) -> name' = name) table
    let create names = List.map (fun name -> (name, ref 0, ref 0)) names
    let update table name address size =
        match search_by_name table name with
        | Some (_, address', size') -> address' := address; size':=size
        | None -> failwithf "rule %s is not found." name

(* 文字列化 *)
module Dumper =
    let dump_op (table:RuleTable.t) bc =
        match bc with
        | VM.Instruction.t.Char  c             -> sprintf "(Char \"%s\")" (String.escapeString c)
        | VM.Instruction.t.Any                 -> "(Any)"
        | VM.Instruction.t.Set   cs            -> sprintf "(Set %s)" (List.map (fun c -> (sprintf "\"%s\"" (String.escapeString c))) cs |> String.concat " ")
        | VM.Instruction.t.Jmp   x             -> sprintf "(Jmp %d)" x
        | VM.Instruction.t.Call  {contents=x}  -> match RuleTable.search_by_address table x with
                                                  | Some (key,_,_) -> sprintf "(Call \"%s\")" key
                                                  | None -> sprintf "(Call %d)" x
        | VM.Instruction.t.Match               -> "(Match)"
        | VM.Instruction.t.Split (x,y)         -> sprintf "(Split %d %d)" x y
        | VM.Instruction.t.Join  x             -> sprintf "(Join %d)" x
        | VM.Instruction.t.Over                -> "(Over)"
        | VM.Instruction.t.Roll                -> "(Roll)"
        | VM.Instruction.t.Fail                -> "(Fail)"
        | VM.Instruction.t.End                 -> "(End)"
        
    let dump ((table, bytecodes):(RuleTable.t * (VM.Instruction.t list))) =
        let dump_rule s (name,{contents=address},{contents=size}) =
            let s = (sprintf "(Rule \"%s\" " name)::s
            let s = Seq.fold (fun s x -> ((dump_op table bytecodes.[x] |> sprintf "\t%s")::s)) s (seq {address .. address+size-1})
            let s = ")"::s
            in  s
        in  (List.fold dump_rule [] table) |> List.rev |> String.concat "\n"

(* コンパイラ *)
module Compiler =

    let rec compile_expr (table:RuleTable.t) (expr:Ast.Expr) = 
        match expr with
            | Ast.Expr.Char c    -> [VM.Instruction.t.Char c]
            | Ast.Expr.Str s     -> List.ofSeq s |> List.map (fun x -> Ast.Expr.Char x) |> Ast.Expr.Seq |> compile_expr table
            | Ast.Expr.Seq es    -> List.collect (compile_expr table) es
            | Ast.Expr.Choice es ->
                let rec gen es = 
                    match es with
                    | x::[] -> compile_expr table x
                    | x::xs ->
                        let e1 = compile_expr table x 
                        let e2 = gen xs
                        let l1 = 1
                        let l2 = l1 + (List.length e1) + 1
                        let l3 = (List.length e2) + 1
                        in  (VM.Instruction.t.Split (l1,l2))::e1@((VM.Instruction.t.Join l3)::e2)
                    | _ -> failwith "Choice rule is empty."
                in  gen es
            | Ast.Expr.Option e -> 
                let e1 = compile_expr table e 
                let l1 = 1
                let l2 = l1 + (List.length e1) + 1
                let l3 = 1
                in  (VM.Instruction.t.Split (l1,l2))::e1@[VM.Instruction.t.Join l3]
            | Ast.Expr.ZeroOrMany e -> 
                let e1 = compile_expr table e 
                let l1 = -((List.length e1) + 1)
                let l2 = 1
                let l3 = (List.length e1) + 2
                in  (VM.Instruction.t.Split (l2,l3))::e1@[VM.Instruction.t.Join l1]
            | Ast.Expr.OneOrMany e -> 
                compile_expr table (Ast.Expr.Seq [e; Ast.Expr.ZeroOrMany e])
            | Ast.Expr.TryTrue e -> 
                let e1 = compile_expr table e 
                in  (VM.Instruction.t.Over)::e1@[VM.Instruction.t.Roll]
            | Ast.Expr.TryFalse e -> 
                let e1 = compile_expr table e 
                let l1 = 1
                let l2 = l1 + (List.length e1) + 1
                let l3 = 1
                in  (VM.Instruction.t.Split (l1,l2))::e1@[VM.Instruction.t.Roll]
            | Ast.Expr.Call s ->
                match List.tryFind (fun (name,off,len) -> name = s) table with
                | Some (key,off,len) -> [VM.Instruction.t.Call off]
                | None -> failwithf "rule %s is not found." s

    let compile_rule (table:RuleTable.t) (rule:Ast.Rule) =
        let (_, expr) = rule
        in  (compile_expr table expr)@[VM.Instruction.t.Match]

    let compile_grammer (grammer:Ast.Grammer) =
        let table = List.map (fun (name, _) -> name) grammer |> RuleTable.create
        let build_and_update s rule =
            let (name, expr) = rule
            let bytecode = compile_rule table rule
            let address = List.length s
            let size = List.length bytecode
            let _ = RuleTable.update table name address size
            in  s @ bytecode
        let bytecode = List.fold build_and_update [] grammer
        in  (table, bytecode@[VM.Instruction.t.End])

[<EntryPoint>]
let main argv = 
    let r1 = ("e1 e2",Ast.Expr.Seq [Ast.Expr.Str "e1";Ast.Expr.Str "e2"])
    let _ = Compiler.compile_rule [] r1 |> printfn "%A" 
    let r2 = ("e1 / e2",Ast.Expr.Choice [Ast.Expr.Str "e1";Ast.Expr.Str "e2"])
    let _ = Compiler.compile_rule [] r2 |> printfn "%A" 
    let r3 = ("e?",Ast.Expr.Option (Ast.Expr.Str "e"))
    let _ = Compiler.compile_rule [] r3 |> printfn "%A" 
    let r4 = ("e*",Ast.Expr.ZeroOrMany (Ast.Expr.Str "e"))
    let _ = Compiler.compile_rule [] r4 |> printfn "%A" 
    let r5 = ("e+",Ast.Expr.OneOrMany (Ast.Expr.Str "e"))
    let _ = Compiler.compile_rule [] r5 |> printfn "%A" 
    let r6 = ("&e",Ast.Expr.TryTrue (Ast.Expr.Str "e"))
    let _ = Compiler.compile_rule [] r6 |> printfn "%A" 
    let r7 = ("!e",Ast.Expr.TryFalse (Ast.Expr.Str "e"))
    let _ = Compiler.compile_rule [] r7 |> printfn "%A" 
    let r8 = ("S <- e",Ast.Expr.Call "e")
    let _ = Compiler.compile_rule [("e",ref 0,ref 0)] r8 |> printfn "%A" 
    let grammer = [
        ("csv", Ast.Expr.Seq [Ast.Expr.Call "digits"; Ast.Expr.Seq [Ast.Expr.Char ','; Ast.Expr.Call "digits"]]);
        ("digits", Ast.Expr.OneOrMany (Ast.Expr.Call "digit"));
        ("digit", Ast.Expr.Choice ("0123456789" |> List.ofSeq |> List.map Ast.Expr.Char)) 
    ]
    let (env,bytecode) = Compiler.compile_grammer grammer 
    let _ = (env,bytecode) |> Dumper.dump |> printfn "%s"
    let _ = VM.apply bytecode 0 "12345,67890" 0 |> printfn "%A" 
    let grammer = [
        ("csv", Ast.Expr.Seq [Ast.Expr.Call "&a"; Ast.Expr.Call "abc"]);
        ("&a", Ast.Expr.TryTrue (Ast.Expr.Char 'a')); 
        ("abc", Ast.Expr.Seq (("abc" |> List.ofSeq |> List.map Ast.Expr.Char) )) 
    ]
    let (env,bytecode) = Compiler.compile_grammer grammer 
    let _ = (env,bytecode) |> Dumper.dump |> printfn "%s"
    let _ = VM.apply bytecode 0 "xabc" 0 |> printfn "%A" 
    in  0
