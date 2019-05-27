namespace InterpreterCore

module Type =
    type qual = Lin | Un

    type ltype =
          Bool of qual
        | Pair of qual * ltype * ltype
        | Fn of qual * ltype * ltype

    type context = (string * ltype) list

    let get_qual (t : ltype) : qual =
        match t with
        | Bool q -> q
        | Pair (q,_,_) -> q
        | Fn (q,_,_) -> q

    let qual_of_string (q : qual) : string =
        match q with
        | Lin -> "lin"
        | Un -> "un"

    let rec type_of_string (t : ltype) : string =
        match t with
        | Bool q -> sprintf "%s Bool" (qual_of_string q)
        | Pair (q,t1,t2) -> sprintf "%s (%s,%s)" (qual_of_string q) (type_of_string t1) (type_of_string t2)
        | Fn (q,t1,t2) -> sprintf "%s (%s->%s)" (qual_of_string q) (type_of_string t1) (type_of_string t2)

    let print_context (con : context) : unit =
        let _ = List.map (fun (x,y) -> printfn "(%s,%s)" x (type_of_string y) ) con 
        in  ()


    // Containment Rule
    // Unで修飾されたラムダ抽象の中にLinで修飾された束縛変数が登場してはいけない
    // λx.t は スコープ t 中での 変数 x の宣言とみなせる

    // ATTaPLの例: 
    //   let discard = lin λx:lin bool.(lin λf:un (un bool -> lin bool).lin true) (un λy:un bool.x)
    // 整理: 
    //   let discard = (lin λ(x:lin bool) -> lin λ(f:un (un bool -> lin bool)) -> lin true) (un λ(y:un bool).x)
    // 動作理解のためのOCaml的表現：
    //   let discard = fun (x:bool) -> (fun (f:(bool -> bool)) -> true) (fun (y:bool) -> x)
    //   (引数 x はどこでも使われない)

    // 最初のラムダ抽象 lin λ(x:lin bool) の x は線形型で、xはラムダ抽象 un λy:un bool.x で消費しているように見える。しかし、実際にはλ抽象 un λy:un bool.x は呼び出されないので消費されず、型システム的にはエラーにならなければならない。
    // ところが、Containment Ruleがない場合はエラーにならない
    // なぜ？
    // ATTaPLによると、「非線形型のラムダ抽象中に線形型の変数が登場している」が原因

    /// Containment Ruleをラムダ抽象が持つ修飾子 q と、束縛変数 t が持つ修飾子が満たしているかチェック
    let rec check_qual_contain_type (q : qual) (t : ltype) : bool =
        let check_containment_rule (q : qual) (q' : qual) : bool =
            not (q = Un && q' = Lin)    // ラムダ抽象が Un で、束縛変数が Lin は Containment Rule 違反
        in
            match t with
            | Bool  q'        ->                                                                        check_containment_rule q q'
            | Pair (q',t1,t2) -> (check_qual_contain_type q' t1) && (check_qual_contain_type q' t2) && (check_containment_rule q q')
            | Fn   (q',t1,t2) -> (check_qual_contain_type q' t1) && (check_qual_contain_type q' t2) && (check_containment_rule q q')

    let check_qual_contain_context (q : qual) (con : context) : bool =
        List.forall (function (_,t) -> check_qual_contain_type q t) con

module Ast =
    type term =
          Var of string
        | Boolean of Type.qual * bool
        | If of term * term * term
        | Pair of Type.qual * term * term
        | Split of term * string * string * term
        | Abs of Type.qual * string * Type.ltype * term
        | App of term * term

    type toplevel = (string * term) list

    let rec print_ast (t : term) =
        match t with
            | Var s -> sprintf "Var %s" s
            | Boolean (q,b) -> sprintf "%s %b" (Type.qual_of_string q) b
            | If (t1,t2,t3) -> sprintf "If (%s) (%s) (%s)" (print_ast t1) (print_ast t2) (print_ast t3)
            | Pair (q,t1,t2) -> sprintf "%s (%s,%s)" (Type.qual_of_string q) (print_ast t1) (print_ast t2)
            | Split (t1,x,y,t2) -> sprintf "Split (%s) (%s,%s) (%s)" (print_ast t1) x y (print_ast t2)
            | Abs (q,n,tp,t1) -> sprintf "%s Abs (%s) (%s) (%s)" (Type.qual_of_string q) n (Type.type_of_string tp) (print_ast t1)
            | App (t1,t2) -> sprintf "App (%s) (%s)" (print_ast t1) (print_ast t2)

module TypeCheck =
    type checkT = Type.ltype * Type.context

    exception TypeError of string * string
    exception ContextError
    exception QualError
    exception UnUsedError of string

    /// 二つの型が等しいか判定
    let check_equal (t1 : Type.ltype) (t2 : Type.ltype) : unit =
        if t1 = t2 
        then () 
        else raise (TypeError (Type.type_of_string t1,Type.type_of_string t2))

    /// 型 t1 と 型 t2 の記憶クラス以外が等しいか判定
    /// 記憶クラスの違いを無視するために、記憶クラスをUnに統一している
    let check_equal' (t1 : Type.ltype) (t2 : Type.ltype) : unit =
        let replace_un_qual q_t =
            match q_t with
            | Type.Bool  q            -> Type.Bool  Type.Un
            | Type.Pair (q, t1', t2') -> Type.Pair (Type.Un, t1', t2')
            | Type.Fn   (q, t1', t2') -> Type.Fn   (Type.Un, t1', t2') 
        in
            check_equal (replace_un_qual t1) (replace_un_qual t2)

    /// 一番外側の型が等しいか判定
    let check_equal_const (t1 : Type.ltype) (t2 : Type.ltype) : unit =
        let erase_type t =
            match t with
            | Type.Bool _ -> Type.Bool Type.Un
            | Type.Pair (_,_,_) -> Type.Pair (Type.Un,Type.Bool Type.Un,Type.Bool Type.Un)
            | Type.Fn (_,_,_) -> Type.Fn (Type.Un,Type.Bool Type.Un,Type.Bool Type.Un) 
        in
            check_equal (erase_type t1) (erase_type t2)

    /// 型環境 context1 と 型環境 context2 が一致するか判定
    /// ここでの一致は型環境が持つ束縛名が一致するか？のみを見ている。
    let check_equal_context (context1 : Type.context) (context2 : Type.context) : unit =
        if List.length context1 <> List.length context2 
        then raise ContextError 
        else
            let exists_context c1 c2 = List.forall (fun (n,_) -> List.exists (fun (y,_) -> n = y) c1) c2 
            in
                if exists_context context1 context2 && exists_context context2 context1 
                then () 
                else raise ContextError

    /// 型環境 context の下で 変数項 name の型を得る
    let t_var (name : string) (context : Type.context) : checkT =
        let var_type = match List.tryFind (fun (k,v) -> k = name) context with 
                        | None -> (printfn "Variable %s is already used or not defined\n" name); exit 0 
                        | Some (k,v) -> v
        let qual = Type.get_qual var_type 
        in
            match qual with
            | Type.Un -> (var_type,context)
            | Type.Lin -> (var_type,List.where (fun (k,v) -> k <> name) context)

    /// 型環境 context の下で 項 term の型を型推論する
    let rec type_check (term : Ast.term) (context : Type.context) : checkT =
        match term with
        | Ast.Var     name                           -> t_var name context
        | Ast.Boolean (qual, value)                  -> (Type.Bool qual, context)
        | Ast.If      (term1, term2, term3)          -> t_if term1 term2 term3 context
        | Ast.Pair    (qual, term1, term2)           -> t_pair qual term1 term2 context
        | Ast.Split   (term1, x, y, term_body)       -> t_split term1 x y term_body context
        | Ast.App     (term1, term2)                 -> t_app term1 term2 context
        | Ast.Abs     (qual, name, vtype, term_body) -> t_abs qual name vtype term_body context
    
    /// 型環境 context の下で if項 の型を型推論する
    and t_if (term_cond : Ast.term) (term1 : Ast.term) (term2 : Ast.term) (context : Type.context) : checkT =
        let (cond_t,context2) = type_check term_cond context    // 条件式の型推論結果と型環境
        let (term1_t,context3) = type_check term1 context2      // then部の型推論結果と型環境
        let (term2_t,context3') = type_check term2 context2     // else部の型推論結果と型環境
        let _ = check_equal' cond_t (Type.Bool Type.Un)         // 条件式の型は un Bool でなければならない
        let _ = check_equal term1_t term2_t                     // then部とelse部の型は一致しなければならない
        let _ = check_equal_context context3 context3'          // context3とcontext3' は一致しなければならない
        in  (term1_t,context3)
    
    /// 型環境 context の下で pair項 の型を型推論する
    /// pair項 <x,y> はラムダ計算では λx.λy.<x,y> に対応する。これはラムダ抽象であるため、Containment Ruleを「qual と term1」、「qualとterm2」が満たされているか調べなければならない。
    and t_pair (qual : Type.qual) (term1 : Ast.term) (term2 : Ast.term) (context : Type.context) : checkT =
        let (term1_t,context2) = type_check term1 context   // car部の型推論結果と型環境
        let (term2_t,context3) = type_check term2 context2  // cdr部の型推論結果と型環境
        in  if not ((Type.check_qual_contain_type qual term1_t) && (Type.check_qual_contain_type qual term2_t)) 
            then raise QualError 
            else (Type.Pair (qual,term1_t,term2_t),context3)
    
    /// 型環境 context の下で split項 の型を型推論する
    and t_split (term1 : Ast.term) (x : string) (y : string) (term_body : Ast.term) (context : Type.context) : checkT =
        let (term1_t,context2) = type_check term1 context 
        let _ = check_equal_const term1_t (Type.Pair (Type.Un,Type.Bool Type.Un,Type.Bool Type.Un))
        let (Type.Pair (_, x_t, y_t)) = term1_t 
        let (term_body_t,context3) = type_check term_body ((x,x_t) :: (y,y_t) :: context2) 
        let _ = if Type.get_qual x_t = Type.Lin && List.exists (fun (k,v) -> x = k) context3 then raise (UnUsedError x) else ()
        let _ = if Type.get_qual y_t = Type.Lin && List.exists (fun (k,v) -> y = k) context3 then raise (UnUsedError y) else ()
        in  (term_body_t,List.where (fun (k,v) -> y <> k) (List.where (fun (k,v) -> x <> k) context3))
    
    /// 型環境 context の下で app項（関数適用） の型を型推論する
    and t_app (term1 : Ast.term) (term2 : Ast.term) (context : Type.context) : checkT =
        let (term1_t,context2) = type_check term1 context 
        let _ = check_equal_const term1_t (Type.Fn (Type.Un,Type.Bool Type.Un,Type.Bool Type.Un))
        let (term2_t,context3) = type_check term2 context2
        let (Type.Fn (_,t11,t12)) = term1_t 
        in  check_equal t11 term2_t; (t12,context3)

    /// 型環境 context の下で abs項（ラムダ抽象） の型を型推論する
    and t_abs (qual : Type.qual) (name : string) (vtype : Type.ltype) (term_body : Ast.term) (context : Type.context) : checkT =
        let _ = if Type.check_qual_contain_type (Type.get_qual vtype) vtype then () else raise QualError
        let (term_body_t,context2) = type_check term_body ((name,vtype) :: context) 
        let _ = if Type.check_qual_contain_type qual term_body_t then () else raise QualError
        let _ = if Type.get_qual vtype = Type.Lin && List.exists (fun (k,v) -> name = k) context2 then raise (UnUsedError name) else ()
        let _ = if qual = Type.Un then check_equal_context context (List.where (fun (k,v) -> name = k) context2) else ()
        in  (Type.Fn (qual,vtype,term_body_t),List.where (fun (k,v) -> name = k) context2)
