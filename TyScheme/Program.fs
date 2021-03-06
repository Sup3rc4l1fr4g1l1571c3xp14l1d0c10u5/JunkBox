﻿open System
open System.Linq.Expressions
open System.Text.RegularExpressions;
open Microsoft.FSharp.Core;

module Number =
    type Type =
          Int      of value : int
        | Real     of value : double
        //| Complex  of real:NumberV * imaginary:NumberV
        //| Fraction of numerator:int * denominator:int

    let rec ToString v =
        match v with
        //| Complex (r,i) -> (valueToString r) + "+" + (valueToString i) + "i" 
        //| Fraction (n,d) -> n.ToString() + "/" + d.ToString()
        | Int v -> v.ToString()
        | Real v -> v.ToString()

    //let toComplex v =
    //    match v with
    //    | Complex _ -> v
    //    | Fraction _ -> Complex (v, Int 0)
    //    | Int _ -> Complex (v, Int 0)
    //    | Real _  -> Complex (v, Int 0)

    //let toFraction v =
    //    match v with
    //    | Complex _ -> failwith "cannt convert"
    //    | Fraction _ -> v
    //    | Int v -> Fraction (v, 1)
    //    | Real v  -> Fraction (int (v * 10000.0), 10000)

    let toInt v =
        match v with
        //| Complex _ -> failwith "cannt convert"
        //| Fraction _ -> failwith "cannt convert"
        | Int _ -> v
        | Real _  -> failwith "cannt convert"

    let toReal v =
        match v with
        //| Complex _ -> failwith "cannt convert"
        //| Fraction _ -> v
        | Int v -> Real (float v)
        | Real _  -> v

    let rec add lhs rhs =
        match (lhs, rhs) with
        //| Complex (r1,i1), Complex (r2,i2)  -> Complex (add r1 r2, add i1 i2)
        //| Complex _, _  -> add lhs (toComplex rhs)
        //| _, Complex  _  -> add (toComplex lhs) rhs

        //| Fraction (n1,d1), Fraction (n2,d2) -> Fraction (n1*d2+n2*d1,d1*d2)
        //| Fraction _,_ -> add lhs (toFraction rhs)
        //| _, Fraction _ -> add (toFraction lhs) rhs

        | Real v1, Real v2 -> Real (v1 + v2)
        | Real  _,       _  -> add lhs (toReal rhs)
        |        _, Real _  -> add (toReal lhs) rhs

        | Int v1, Int v2 -> Int (v1 + v2)
        | Int _ ,_        -> add lhs (toInt rhs)
        |       _, Int _  -> add (toInt lhs) rhs

        | _ -> failwith "cannot add"

    let rec sub lhs rhs =
        match (lhs, rhs) with
        //| Complex (r1,i1), Complex (r2,i2)  -> Complex (add r1 r2, add i1 i2)
        //| Complex _, _  -> add lhs (toComplex rhs)
        //| _, Complex  _  -> add (toComplex lhs) rhs

        //| Fraction (n1,d1), Fraction (n2,d2) -> Fraction (n1*d2+n2*d1,d1*d2)
        //| Fraction _,_ -> add lhs (toFraction rhs)
        //| _, Fraction _ -> add (toFraction lhs) rhs

        | Real v1, Real v2 -> Real (v1 - v2)
        | Real  _,       _  -> sub lhs (toReal rhs)
        |        _, Real _  -> sub (toReal lhs) rhs

        | Int v1, Int v2 -> Int (v1 - v2)
        | Int _ ,_        -> sub lhs (toInt rhs)
        |       _, Int _  -> sub (toInt lhs) rhs

        | _ -> failwith "cannot sub"

    let neg v =
        match v with
        //| Complex _ -> failwith "cannot neg" 
        //| Fraction (n,d) -> Fraction (-n,d)
        | Int v -> Int (-v)
        | Real v -> Real (-v)

module Scheme =

    type DeBruijnIndex = ((*i*)int * (*j*)int)

    type Register = {
            s: Value list; 
            e: Value list; 
            c: Inst list; 
            d: (Value list * Value list * Inst list) list; 
        }
    and Value  = 
          Symbol        of value : string
        | Nil
        | Undef
        | Array         of value : Value list
        | Cell          of car: (Value ref) * cdr: (Value ref)
        | String        of value : string
        | Char          of value : char
        | Number        of value : Number.Type
        | Boolean       of value : bool
        | Primitive     of value : (Value -> Value)
        | Closure       of body:Inst list * env:Value list
        | Macro         of value : Value // append to implements macro
        | Error         of subtype: string * message : string 
        | Continuation  of Register // append to implements call/cc

    and Inst =
          Ld     of index : DeBruijnIndex
        | Ldc    of value : Value
        (*  before: append to implements appendix#1 *)
        //| Ldg    of symbol : Value
        (*  after: append to implements appendix#1 *)
        | Ldg    of entry: EnvEntry
        (*  end: append to implements appendix#1 *)
        | Ldf    of body : Inst list
        | Join
        | Rtn
        | App
        | TApp                                                  // append to implements Tail-call optimization
        | Pop
        | Sel    of t_clause:(Inst list) * f_clause:(Inst list)
        | SelR   of t_clause:(Inst list) * f_clause:(Inst list) // append to implements Tail-call optimization
        (*  before: append to implements appendix#1 *)
        //| Def    of symbol : Value
        //| Defm   of symbol : Value                              // append to implements macro
        (*  after: append to implements appendix#1 *)
        | Def    of entry : EnvEntry
        | Defm   of entry : EnvEntry                            // append to implements macro
        (*  end: append to implements appendix#1 *)
        | Args   of argc : int
        | Lset   of index : DeBruijnIndex                       // append to implements set!
        (*  before: append to implements appendix#1 *)
        //| Gset   of symbol : Value                              // append to implements set!
        (*  after: append to implements appendix#1 *)
        | Gset   of entry : EnvEntry                            // append to implements set!
        (*  end: append to implements appendix#1 *)
        | LdCt   of next : Inst list                            // append to implements call/cc
        | ArgsAp of argc : int                                  // append to implements call/cc
        | Stop

    and EnvEntry = Value * (Value ref)
    and Env = EnvEntry list

    type Context = { reg:Register; halt: bool }

    let create_context () : Context =
        { reg={s=[]; e=[]; c=[Stop]; d=[]}; halt = true }

    let update_context(context: Context) (expr:Inst list) : Context =
        { context with reg={context.reg with c=expr}; halt = false }

    let nil    = Nil
    let true'  = Boolean true
    let false' = Boolean false
    let undef  = Undef

    let genSym =
        let dic = ref Map.empty
        in fun (str : string) -> 
            match Map.tryFind str !dic with
            | Some x -> x
            | None   -> let v = Symbol str
                        in dic := (!dic).Add(str, v); v;

    let toBool (value : bool) : Value = 
        if value then true' else false'

    let (|Cons|_|) (value: Value) : (Value*Value) option =
        match value with
        | Cell (x,y) -> Some(!x, !y)
        | _ -> None

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
                | '"'  :: xs -> loop xs ("\""::result)
                |    x :: xs -> loop xs (string(x)::result)
            in loop (str.ToCharArray() |> Array.toList) []
        let rec loop (value:Value) (isCdr : bool) =
            match value with 
            | Symbol value -> value
            | Nil -> "()"
            | Undef -> "#<undef>"
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
            | Number value -> Number.ToString value
            | Boolean false -> "#f"
            | Boolean true  -> "#t"
            | Primitive value -> sprintf "<Primitive: %s>" (value.ToString ())
            | Closure (value, env) -> sprintf "<Closure: %A>" (value)
            | Macro (value) -> sprintf "<Macro: %s>" (value.ToString ())
            | Error (subtype, message) -> sprintf "<%s: %s>" subtype message
            | Continuation {s=s; e=e; c=c; d=d} -> sprintf "<Continuation: (%A,%A,%A,%A)>" s e c d
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
    let caar = car << car
    let cdar = cdr << car

    let set_car (c : Value) (v : Value) =
        match c with
        | Cell (x, y) -> x := v
        | _ -> failwith "cannot change"

    let set_cdr (c : Value) (v : Value) =
        match c with
        | Cell (x, y) -> y := v
        | _ -> failwith "cannot change"

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

    let list_rev (list:Value) : Value = 
        list_reduce (fun x s -> Cell (ref x, ref s)) nil list

    let list_copy (list:Value) : Value = 
        list |> list_rev |> list_rev

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
        | (Undef            , Undef            ) -> true
        | (                _,                 _) -> false

    and ieq (x:Inst) (y:Inst) : bool =
        match (x,y) with
        | (Ld   (i1,j1), Ld   (i2,j2)) -> (i1=i2) && (j1=j2)
        | (Ldc     (v1), Ldc     (v2)) -> ceq v1 v2
        (*  before: append to implements appendix#1 *)
        //| (Ldg     (v1), Ldg     (v2)) -> ceq v1 v2
        (*  after: append to implements appendix#1 *)
        | (Ldg  (s1,v1), Ldg  (s2,v2)) -> ceq s1 s2 && ceq !v1 !v2
        (*  end: append to implements appendix#1 *)
        | (Ldf     (i1), Ldf     (i2)) -> List.forall2 ieq i1 i2
        | (Join        , Join        ) -> true
        | (Rtn         , Rtn         ) -> true
        | (App         , App         ) -> true
        | (TApp        , TApp        ) -> true
        | (Pop         , Pop         ) -> true
        | (Sel  (t1,f1), Sel  (t2,f2)) -> (List.forall2 ieq t1 t2) && (List.forall2 ieq f1 f2)
        | (SelR (t1,f1), SelR (t2,f2)) -> (List.forall2 ieq t1 t2) && (List.forall2 ieq f1 f2) // append to implements Tail-call optimization
        (*  before: append to implements appendix#1 *)
        //| (Def       v1, Def       v2) -> ceq v1 v2
        //| (Defm      v1, Defm      v2) -> ceq v1 v2 // append to implements macro
        (*  after: append to implements appendix#1 *)
        | (Def  (s1,v1), Def  (s2,v2)) -> ceq s1 s2 && ceq !v1 !v2
        | (Defm (s1,v1), Defm (s2,v2)) -> ceq s1 s2 && ceq !v1 !v2 // append to implements macro
        (*  end: append to implements appendix#1 *)
        | (Args      i1, Args      i2) -> (i1=i2) 
        | (Lset (i1,j1), Lset (i2,j2)) -> (i1=i2) && (j1=j2) // append to implements set!
        (*  before: append to implements appendix#1 *)
        //| (Gset    (v1), Gset    (v2)) -> ceq v1 v2 // append to implements set!
        (*  after: append to implements appendix#1 *)
        | (Gset (s1,v1), Gset (s2,v2)) -> ceq s1 s2 && ceq !v1 !v2 // append to implements set!
        (*  end: append to implements appendix#1 *)
        | (LdCt    (i1), LdCt    (i2)) -> List.forall2 ieq i1 i2    // append to implements call/cc
        | (ArgsAp    i1, ArgsAp    i2) -> (i1=i2)   // append to implements call/cc
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

    let global_environment: Env ref =
        ref [
                (genSym "#t"     , ref (true')) ;
                (genSym "#f"     , ref (false'));
                (genSym "undef"  , ref (undef));

                (genSym "car"    , ref (Primitive caar));
                (genSym "cdr"    , ref (Primitive cdar));
                (genSym "cons"   , ref (Primitive (fun xs -> Cell (ref (car xs), ref (cadr xs)))));
                (genSym "eq?"    , ref (Primitive (fun xs -> toBool (eq  (car xs) (cadr xs)))));
                (genSym "eqv?"   , ref (Primitive (fun xs -> toBool (eqv (car xs) (cadr xs)))));
                (genSym "pair?"  , ref (Primitive (fun xs -> toBool (ispair (car xs)))));

                (genSym "display", ref (Primitive (fun xs -> printf "%s" (toString (car xs)); nil)));
                
                (genSym "+"      , ref (Primitive (list_fold (fun x s -> match s,x with | Number l, Number r -> Number (Number.add l r) | _ -> failwith "bad argument" ))));
                (genSym "-"      , ref (Primitive (list_fold (fun x s -> match s,x with | Number l, Number r -> Number (Number.sub l r) | _ -> failwith "bad argument" ))));
            ]

    let assoc (sym: Value) (dic: Env ref) : EnvEntry option =
        let rec loop (dic: (Value*(Value ref)) list) =
            match dic with
            | [] -> None
            | ((key, value) as entry) :: d -> if (eq key sym) then Some entry else loop d
        in loop !dic        
        
    (*  before: append to implements appendix#1 *)
    //let get_gvar (sym:Value) : Value =
    //    match assoc sym global_environment with
    //    | Some (key, value) -> !value
    //    | _ -> Error ("runtime error", String.Format("unbound variable:{0}",toString sym))
    (*  after: append to implements appendix#1 *)
    let location_gvar (expr : Value) : EnvEntry =
        match assoc expr global_environment with
        | None -> let cell = ( expr, ref(undef)) in global_environment := cell :: !global_environment; cell
        | Some cell -> cell

    let get_gvar (expr:Value) = 
        match location_gvar(expr) with
        | (car, cdr) -> !cdr
    (*  end: append to implements appendix#1 *)

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

        let location (sym:Value) (ls:Value list) : DeBruijnIndex option  =
            let rec loop (ls:Value list) (i:int) =
                match ls with
                | [] -> None
                | a::d ->
                    match position_var sym a with
                    | Some j -> Some(i, j)
                    | None   -> loop d (i + 1)
            in loop ls 0

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

        let rec comp (expr: Value) (env: Value list) (code: Inst list) (* start: append to implements Tail-call optimization *) (tail:bool) (* end: append to implements Tail-call optimization *) : Inst list =
            match expr with
            | Symbol _ ->
                match location expr env with
                | Some (i, j) -> Ld(i, j)  :: code
                (*  before: append to implements appendix#1 *)
                //| None        -> Ldg(expr) :: code
                (*  after: append to implements appendix#1 *)
                | None        -> Ldg(location_gvar expr) :: code
                (*  end: append to implements appendix#1 *)
            | Cons(Symbol("quote"), Cons(v, Nil)) -> 
                Ldc(v) :: code
            | Cons(Symbol("if"), Cons(cond, Cons(t, Nil))) -> 
                (* before: append to implements Tail-call optimization *)
                //let t_clause = comp t env [Join]
                //let f_clause = [Ldc undef; Join] 
                //in  comp cond env (Sel (t_clause, f_clause) :: code)
                (* after: append to implements Tail-call optimization *)
                if tail 
                then
                    let t_clause = comp t env [Rtn] true
                    let f_clause = [Ldc undef; Rtn]
                    in    comp cond env (SelR(t_clause, f_clause)::code.Tail) false
                else
                    let t_clause = comp t env [Join] false
                    let f_clause = [Ldc undef; Join]
                    in    comp cond env (Sel(t_clause, f_clause)::code) false
                (* end: append to implements Tail-call optimization *)
            | Cons(Symbol("if"), Cons(cond, Cons(t, Cons(e, Nil)))) -> 
                (* before: append to implements Tail-call optimization *)
                //let t_clause = comp t env [Join]
                //let f_clause = comp e env [Join]
                //in  comp cond env (Sel (t_clause, f_clause) :: code)
                (* after: append to implements Tail-call optimization *)
                if tail 
                then
                    let t_clause = comp t env [Rtn] true
                    let f_clause = comp e env [Rtn] true
                    in    comp cond env (SelR(t_clause, f_clause)::code.Tail) false
                else
                    let t_clause = comp t env [Join] false
                    let f_clause = comp e env [Join] false
                    in    comp cond env (Sel (t_clause, f_clause)::code) false
                (* end: append to implements Tail-call optimization *)
            | Cons((Symbol "lambda"), Cons(name, body)) ->
                let body = comp_body body (name :: env) [Rtn]
                in  Ldf(body) :: code
            | Cons((Symbol "define"), Cons((Symbol _) as sym, Cons(body, Nil))) -> 
                (*  before: append to implements appendix#1 *)
                //comp body env (Def(sym)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                (*  after: append to implements appendix#1 *)
                comp body env (Def(location_gvar sym)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                (*  end: append to implements appendix#1 *)
            (*  start: append to implements macro *)
            | Cons((Symbol "define-macro"), Cons((Symbol _) as sym, Cons(body, Nil))) -> 
                (*  before: append to implements appendix#1 *)
                //comp body env (Defm(sym)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                (*  after: append to implements appendix#1 *)
                comp body env (Defm(location_gvar sym)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                (*  end: append to implements appendix#1 *)
            (*  end: append to implements macro *)
            (*  start: append to implements set! *)
            | Cons((Symbol "set!"), Cons(sym,Cons(body, _))) -> 
                (*  before: append to implements appendix#1 *)
                //let pos = location sym env
                //match pos with
                //| Some pos -> comp body env (Lset (pos)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                //| None -> comp body env (Gset (sym)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                (*  after: append to implements appendix#1 *)
                let pos = location sym env
                match pos with
                | Some pos -> comp body env (Lset (pos)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                | None -> comp body env (Gset (location_gvar sym)::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
                (*  end: append to implements appendix#1 *)
            (*  end: append to implements set! *)
            (*  start: append to implements call/cc *)
            | Cons((Symbol "call/cc"), Cons(body,_)) -> 
                [LdCt code; Args 1] @ (comp body env (App::code)) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
            | Cons((Symbol "apply"), Cons(func, args)) -> 
                complis args env (ArgsAp (list_length args)::(comp func env (App::code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *))) 
            (*  end: append to implements call/cc *)
            (*  start: append to implements macro *)
            | Cons(fn, args) when ismacro fn -> 
                let context = { reg={s= []; e= [args] ; c= get_macro_code fn; d= [([], [], [Stop])]}; halt= false }
                let context = run context
                in  printfn "macro(%s) => %s" (toString fn) (toString context.reg.s.Head); comp context.reg.s.Head env code (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
            (*  end: append to implements macro *)
            | Cons(fn, args) -> 
                (* before: append to implements Tail-call optimization *)
                //complis args env ((Args (list_length args))::(comp fn env (App :: code)
                (* after: append to implements Tail-call optimization *)
                complis args env ((Args (list_length args))::(comp fn env ((if tail then TApp else App) :: code) false  ))
                (* end: append to implements Tail-call optimization *)
            | Cons _ -> failwith "syntax error"
            | _ -> Ldc(expr) :: code
            
        and comp_body (body: Value) (env: Value list) (code: Inst list) : Inst list =
            match body with
            | Cons(x, Nil) -> comp x env code (* start: append to implements Tail-call optimization *) true (* end: append to implements Tail-call optimization *)
            | Cons(x, xs)  -> comp x env (Pop :: (comp_body xs env code )) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
            | _            -> failwith "syntax error"
        and complis (expr: Value) (env: Value list) (code: Inst list): Inst list =
            match (expr) with 
            | Nil       -> code 
            | Cons(a,d) -> comp a env (complis d env code) (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)
            | _         -> failwith "syntax error"
        in
            if iserror(expr)
            then [Ldc expr; Stop]
            else comp expr [] [Stop] (* start: append to implements Tail-call optimization *) false (* end: append to implements Tail-call optimization *)


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
            | None -> failwithf "unbound variable: %A" sym

        in
            if context.halt 
            then context
            else 
                match (context.reg.c, context.reg.s, context.reg.e, context.reg.d) with
                | (Ld(i,j)    ::c',                                             s , e,                 d ) -> { context with reg={s = (get_lvar e i j) :: s ; e = e        ; c = c'; d = d }}
                | (Ldc(v)     ::c',                                             s , e,                 d ) -> { context with reg={s = v :: s                ; e = e        ; c = c'; d = d }}
                (*  before: append to implements appendix#1 *)
                //| (Ldg(v)     ::c',                                             s , e,                 d ) -> { context with reg={s = (get_gvar v) :: s     ; e = e        ; c = c'; d = d }}
                (*  after: append to implements appendix#1 *)
                | (Ldg(Symbol(sym), v) ::c',                                    s , e,                 d ) -> if (sym <> "undef") && (eq !v undef)
                                                                                                              then failwithf "unbound variable:%A" v
                                                                                                              else { context with reg={s = !v :: s     ; e = e        ; c = c'; d = d }}
                (*  end: append to implements appendix#1 *)
                | (Ldf(v)     ::c',                                             s , e,                 d ) -> { context with reg={s = Closure (v, e) :: s   ; e = e        ; c = c'; d = d }}
                | (App        ::c',                      Primitive(f) :: arg :: s', e,                 d ) -> { context with reg={s = (f arg) :: s'         ; e = e        ; c = c'; d = d }}
                (*  start: append to implements call/cc *)
                | (App        ::_ , Continuation{s=s';e=e';c=c';d=d'} :: arg :: _ , _,                 _ ) -> { context with reg={s = (arg) :: s'           ; e = e'       ; c = c'; d = d'}}
                (*  end: append to implements call/cc *)
                | (App        ::c',                     Closure(f,e') :: arg :: s', e,                 d ) -> { context with reg={s = []                    ; e = arg :: e'; c = f ; d = (s', e, c') :: d }}
                | (Rtn        ::_ ,                                       s1 :: [], e, (s2, e', c') :: d') -> { context with reg={s = s1 :: s2              ; e = e'       ; c = c'; d = d'}}
                | (Sel(_, f)  ::c',                          (Boolean false) :: s', e,                 d ) -> { context with reg={s = s'                    ; e = e        ; c = f ; d = ([],[],c') :: d }}
                | (Sel(t, _)  ::c',                                        _ :: s', e,                 d ) -> { context with reg={s = s'                    ; e = e        ; c = t ; d = ([],[],c') :: d }}
                | (Join       ::_ ,                                             s , e,   (_, _, c') :: d') -> { context with reg={s = s                     ; e = e        ; c = c'; d = d'}}
                | (Pop        ::c',                                        _ :: s', e,                 d ) -> { context with reg={s = s'                    ; e = e        ; c = c'; d = d }}
                | (Args(v)    ::c',                                             s , e,                 d ) -> let (a'',s') = List.splitAt v s
                                                                                                              let a' = List.fold (fun s x -> Cell(ref x,ref s))  nil a''
                                                                                                              in { context with reg={s = a' :: s'; e = e; c = c'; d = d }}
                (*  before: append to implements appendix#1 *)
                //| (Def(sym)   ::c',                                     body :: s', e,                 d ) -> global_environment := (sym, ref body) :: !global_environment;
                //                                                                                              { context with reg={s = sym :: s'; e = e; c = c'; d = d }}
                (*  after: append to implements appendix#1 *)
                | (Def(sym,v) ::c',                                     body :: s', e,                 d ) -> v := body;
                                                                                                              { context with reg={s = sym :: s'; e = e; c = c'; d = d }}
                (*  end: append to implements appendix#1 *)
                (*  start: append to implements macro *)
                (*  before: append to implements appendix#1 *)
                //| (Defm(sym)  ::c',                       (Closure _ as body):: s', e,                 d ) -> global_environment := (sym, ref (Macro body)) :: !global_environment;
                //                                                                                              { context with reg={s = sym :: s'; e = e; c = c'; d = d }}
                (*  before: append to implements appendix#1 *)
                (*  after: append to implements appendix#1 *)
                | (Defm(sym,v)::c',                       (Closure _ as body):: s', e,                 d ) -> v := Macro body;
                                                                                                              { context with reg={s = sym :: s'; e = e; c = c'; d = d }}
                (*  end: append to implements appendix#1 *)
                (*  end: append to implements macro *)
                (*  start: append to implements set! *)
                | (Lset(i,j)  ::c',                                    value :: s', e,                 d ) -> set_lvar e i j value; 
                                                                                                              { context with reg={s = context.reg.s; e = e; c = c'; d = d }}
                (*  before: append to implements appendix#1 *)
                //| (Gset(sym)  ::c',                                    value :: s', e,                 d ) -> set_gvar sym value;
                //                                                                                              { context with reg={s = context.reg.s; e = e; c = c'; d = d }}
                (*  after: append to implements appendix#1 *)
                | (Gset(sym,v)::c',                                    value :: s', e,                 d ) -> if eq !v undef
                                                                                                              then failwithf "unbound variable:%A" v
                                                                                                              else v := value;
                                                                                                                   { context with reg={s = context.reg.s; e = e; c = c'; d = d }}
                (*  end: append to implements appendix#1 *)
                (*  end: append to implements set! *)
                (*  start: append to implements call/cc *)
                | (LdCt(code) ::c',                                             s , e,                 d ) -> let cont = Continuation {s=s; e=e; c=code; d= d}
                                                                                                              in { context with reg={s = cont :: s; e = e; c = c'; d = d }}
                | (ArgsAp(v)  ::c',                                    value :: s', e,                 d ) -> let rec loop n a s =
                                                                                                                  match n,s with
                                                                                                                  | 0,[] -> { context with reg={s = a :: s; e = e; c = c'; d = d }}
                                                                                                                  | n,x :: xs when n > 0-> loop (n - 1) (Cell(ref x, ref a)) xs
                                                                                                                  | _ -> failwith "bad arg"
                                                                                                              in loop (v - 1) (list_copy value) s'
                (*  end: append to implements call/cc *)
                (*  start: append to implements Tail-call optimization *)
                | (SelR(_, f) ::_ ,                          (Boolean false) :: s', e,                 d ) -> { context with reg={s = s'                    ; e = e        ; c = f ; d = d }}
                | (SelR(t, _) ::_ ,                                        _ :: s', e,                 d ) -> { context with reg={s = s'                    ; e = e        ; c = t ; d = d }}
                | (TApp       ::c',                      Primitive(f) :: arg :: s', e,                 d ) -> { context with reg={s = (f arg) :: s'         ; e = e        ; c = c'; d = d }}
                | (TApp       ::_ , Continuation{s=s';e=e';c=c';d=d'} :: arg :: _ , _,                 _ ) -> { context with reg={s = (arg) :: s'           ; e = e'       ; c = c'; d = d'}}
                | (TApp       ::c',                     Closure(f,e') :: arg :: s', e,                 d ) -> { context with reg={s = s'                    ; e = arg :: e'; c = f ; d = d }}
                (*  end: append to implements Tail-call optimization *)
                | (Stop       ::c',                                             s , e,                 d ) -> { context with halt= true }
                | (_, _, _, _)                                                                             -> failwith "bad context"
    and run (context:Context) :Context =
        let rec loop context =
            let context = vm context
            in  if context.halt then context else loop context
        in loop context

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

        let whitespace = 
            let space = (ParserCombinator.char isspace).Select(fun _ -> " ")
            let comment = (ParserCombinator.char (fun x -> x = ';')).AndR((ParserCombinator.char (fun x -> x <> '\n')).Many()).Select(fun _ -> " ");
            in space.Or(comment).Many()
    
        let char = 
            let space     = (str "#\\space").Select(fun _ -> ' ')
            let newline   = (str "#\\newline").Select(fun _ -> '\n')
            let character = (str "#\\").AndR(any())
            in  whitespace.AndR(space.Or(newline).Or(character)).Select(Char)

        let eof = not(any())

        let bool = 
            let trueV  = (str "#t").Select(fun _ -> true)
            let falseV = (str "#f").Select(fun _ -> false)
            in  whitespace.AndR(trueV.Or(falseV)).Select(toBool)

        let string = 
            let ch = 
                let dquote    = (str "\\\"").Select(fun _ -> '"')
                let cr        = (str "\\r" ).Select(fun _ -> '\r')
                let lf        = (str "\\n" ).Select(fun _ -> '\n')
                let tab       = (str "\\t" ).Select(fun _ -> '\t')
                let formfeed  = (str "\\f" ).Select(fun _ -> '\f')
                let notescape = ParserCombinator.char (fun x -> x <> '\\' && x <> '"')
                in  dquote.Or(cr).Or(lf).Or(tab).Or(formfeed).Or(notescape)
            in whitespace.AndR(ParserCombinator.char (fun x -> x = '"')).AndR(ch.Many()).AndL(ParserCombinator.char (fun x -> x = '"')).AsString().Select(String)

        let number = 
            let digit = 
                 let digit_prefix = (str "#d").Option()
                 let accuracy_prefix = (str "#i").Or(str "#e").Option()
                 let prefix = digit_prefix.AndR(accuracy_prefix).Or(accuracy_prefix.AndL(digit_prefix))
                 let sign = (str "+").Or(str "-").Option()
                 let digit_num = ParserCombinator.char isdigit
                 let digit_usint = digit_num.Many1().AndL((str "#").Many()).AsString().Select(int)
                 let digit_decimals = 
                    let d1 = (str ".").AndR(digit_num).AndL((str "#").Many()).Select(fun d -> ("0."+d.ToString()) |> double |> Number.Real )
                    let d2 = (digit_num.Many1()).AndL(str ".").And(digit_num).AndL((str "#").Many()).Select(fun (i,d) -> (i.ToString()+"."+d.ToString()) |> double |> Number.Real )
                    let d3 = (digit_num.Many1()).AndL((str "#").Many1()).AndL(str ".").AndL((str "#").Many()).Select(fun i -> i.ToString() |> double |> Number.Real )
                    let d4 = (digit_num.Many1()).AsString().Select(Number.Int << int)
                    in  d1.Or(d2).Or(d3).Or(d4) 
                 let digit_usreal = 
                    //let faction = digit_usint.AndL(whitespace.And(str "/").And(whitespace)).And(digit_usint).Select(fun v -> Fraction v)
                    //in  faction.Or(digit_decimals).Or(digit_usint.Select(Int))
                    digit_decimals.Or(digit_usint.Select(Number.Int))
                 let digit_real = sign.And(digit_usreal).Select(fun (s,v) -> match s with Some("+") -> v | Some("-") -> Number.neg v | _ -> v)
                 //let digit_complex = 
                 //   // digit_real.AndL(str "@").AndR(digit_real) <- ?
                 //   let p1 = digit_real.AndL(str "+i").Select(fun real -> Complex (real, Real 1.0))
                 //   let p2 = digit_real.AndL(str "+" ).And(digit_usreal).AndL(str "i").Select(fun (real,imaginary) -> Complex (real, imaginary))
                 //   let p3 = digit_real.AndL(str "-i").Select(fun real -> Complex (real, Real -1.0))
                 //   let p4 = digit_real.AndL(str "-" ).And(digit_usreal).AndL(str "i").Select(fun (real,imaginary) -> Complex (real, NumberVOp.neg imaginary))
                 //   in p1.Or(p2).Or(p3).Or(p4)
                 //in prefix.And(digit_complex.Or(digit_real)).Select(fun (a,v) -> Number v )
                 in prefix.And(digit_real).Select(fun (a,v) -> Number v )
            in  whitespace.AndR(digit)

        let ident = 
             let normal = 
                let head = ParserCombinator.char ishead
                let tail = ParserCombinator.char istail
                in head.And(tail.Many().AsString()).Select(fun (x,xs) -> x.ToString() + xs)
             let plus  = str "+"
             let minus = str "-"
             let dots  = str "..."
             in whitespace.AndR(normal.Or(plus).Or(minus).Or(dots)).Select(genSym)

        let atom = whitespace.AndR(string.Or(char).Or(bool).Or(number).Or(ident))

        let rec expr = 
            lazy_ (fun () -> whitespace.AndR(quoted.Or(list_).Or(vector).Or(atom)))

        and quoted = 
            let quote = (str "'").AndR(expr).Select(fun x -> list [genSym "quote"; x])
            let quasiquote = (str "`").AndR(expr).Select(fun x -> list [genSym "quasiquote"; x])
            let unquote_splicing = (str ",@").AndR(expr).Select(fun x -> list [genSym "unquote-splicing"; x])
            let unquote = (str ",").AndR(expr).Select(fun x -> list [genSym "unquote"; x])
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

    
        let start = whitespace.AndR(eof.Select(fun _ -> None).Or(expr.Select(Some).AndL(whitespace)))

        let test () =
            let check (p:Parser<'a>) (s:string) (ret:ParserState<'a>) (comp:('a -> 'a -> bool)) =
                let max = (0, "")
                let result = 
                    let r1 = (p s 0 max)
                    in 
                        match r1, ret with
                        | Fail (p1, _), Fail (p2, _) -> p1 = p2
                        | Success (p1, v1, (m1p,_)), Success  (p2, v2, (m2p,_)) -> p1 = p2 && comp v1 v2 //&& m1p = m2p
                        | _ -> false
                in  (if result then "." else "!") |> printfn "%s"
            in  
                check char "#\\space" (Success(7, Char ' ',(0,""))) ceq;
                check char "#\\newline" (Success(9, Char '\n',(0,""))) ceq;
                check char "#\\a" (Success(3, Char 'a',(0,""))) ceq;
                check char "#\\&" (Success(3, Char '&',(0,""))) ceq;
                check char "#\\1" (Success(3, Char '1',(0,""))) ceq;

                check bool "#t" (Success(2, true',(0,""))) ceq;
                check bool "#f" (Success(2, false',(0,""))) ceq;

                check string "\"\"" (Success(2, String "",(0,""))) ceq;
                check string "\"abc\"" (Success(5, String "abc",(0,""))) ceq;
                check string "\"xy\\rz\"" (Success(7, String "xy\rz",(0,""))) ceq;
                check string "\"ab\\ncd\"" (Success(8, String "ab\ncd",(0,""))) ceq;
                check string "\"\\t123\"" (Success(7, String "\t123",(0,""))) ceq;
                check string "\"\\f\\f\"" (Success(6, String "\f\f",(0,""))) ceq;
                check string "\"1\\\"23\"" (Success(7, String "1\"23",(0,""))) ceq;

                check quoted "'a" (Success(2, list [genSym "quote"; genSym "a"],(0,""))) ceq;
                check quoted "`a" (Success(2, list [genSym "quasiquote"; genSym "a"],(0,""))) ceq;
                check quoted ",@a" (Success(3, list [genSym "unquote-splicing"; genSym "a"],(0,""))) ceq;
                check quoted ",a" (Success(2, list [genSym "unquote"; genSym "a"],(0,""))) ceq;

                check list_ "(1 2 3 4)" (Success(9, list [Number (Number.Int 1); Number (Number.Int 2); Number (Number.Int 3); Number (Number.Int 4)],(0,""))) ceq;

                check vector "#(1 2 3)" (Success(8, Array [Number (Number.Int 1); Number (Number.Int 2); Number (Number.Int 3)],(0,""))) ceq

[<EntryPoint>]
let main argv =
    let test (s:string) (expr:Scheme.Value) (ret:Scheme.Value) (ctx:Scheme.Context): Scheme.Context = 
        printfn "Test:";
        printfn "  Input Code: %s" s;
        match Scheme.Parser.expr s 0 (0,"") with
        | Scheme.ParserCombinator.Fail    (pos, (mp, m)) -> printfn "Parse Failed at (%d): %s" mp m; ctx;
        | Scheme.ParserCombinator.Success (pos, value, _) -> 
            printfn "  Parsed   AST: %A" value;
            printfn "  Expected AST: %A" expr;
            if (Scheme.ceq value expr) = false 
            then (printfn "  -> AST Not Match"); ctx
            else
                let code = value |> Scheme.compile
                let ctx' = Scheme.update_context ctx code
                let result = Scheme.run ctx'
                in  
                    result.reg.s.[0] |> Scheme.toString |> printfn "  Execute  Result: %s";
                    ret                  |> Scheme.toString |> printfn "  Expected Result: %s";
                    (if Scheme.ceq ret result.reg.s.[0] then printfn "  All Match" else printfn "  -> Result Not Match");
                    ctx'
    let eval (s:string) (ctx:Scheme.Context): Scheme.Context = 
        printfn "Eval: %s" s;
        let rec loop pos ctx = 
            match Scheme.Parser.start s pos (0, "") with
            | Scheme.ParserCombinator.Fail    (pos, (mp, m)) -> printfn "Parse Failed at (%d): %s" mp m; ctx;
            | Scheme.ParserCombinator.Success (pos, Some value, _) -> 
                //let code = value |> Scheme.compile
                let code = value |> Scheme.toString |> printfn "%s";value |> printfn "%A"; value |> Scheme.compile
                let ctx' = Scheme.update_context ctx code
                let result = Scheme.run ctx'
                in  
                    result.reg.s.[0] |> Scheme.toString |> printfn "%s";
                    System.GC.Collect();
                    loop pos ctx' 
            | Scheme.ParserCombinator.Success (pos, None, _) -> ctx
        in loop 0 ctx
    let repl (ctx:Scheme.Context) : Scheme.Context =
        let rec loop ctx =
            let s = System.Console.ReadLine()
            in  match Scheme.Parser.start s 0 (0,"")with
                | Scheme.ParserCombinator.Fail    (pos, (mp, m)) -> printfn "Parse Failed at (%d): %s" mp m; ctx;
                | Scheme.ParserCombinator.Success (pos, Some value, _) -> 
                    let ctx' = value |> Scheme.compile |> Scheme.update_context ctx
                    let result = Scheme.run ctx'
                    in  
                        result.reg.s.[0] |> Scheme.toString |> printfn "%s";
                        System.GC.Collect();
                        loop ctx' 
                | Scheme.ParserCombinator.Success (pos, None, _) -> ctx
        in loop ctx
        
    in
        Scheme.Parser.test();
        Scheme.create_context () |>
        eval "((lambda (x) (set! x 2) x) 1)" |>
        test "(quote a)" (Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]) (Scheme.genSym "a")|>
        test "(if #t 'a 'b)" (Scheme.list [Scheme.genSym("if");  Scheme.true'; Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]; Scheme.list [Scheme.genSym("quote"); Scheme.genSym("b")]]) (Scheme.genSym "a")|>
        test "(if #f 'a 'b)" (Scheme.list [Scheme.genSym("if"); Scheme.false'; Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]; Scheme.list [Scheme.genSym("quote"); Scheme.genSym("b")]]) (Scheme.genSym "b")|>

        test "(car '(a b c))" (Scheme.list [Scheme.genSym("car"); Scheme.list [Scheme.genSym("quote"); Scheme.list[Scheme.genSym("a");Scheme.genSym("b");Scheme.genSym("c")]]]) (Scheme.genSym "a")|>
        test "(cdr '(a b c))" (Scheme.list [Scheme.genSym("cdr"); Scheme.list [Scheme.genSym("quote"); Scheme.list[Scheme.genSym("a");Scheme.genSym("b");Scheme.genSym("c")]]]) (Scheme.list[Scheme.genSym("b");Scheme.genSym("c")])|>
        test "(cons 'a 'b)" (Scheme.list [Scheme.genSym("cons"); Scheme.list [Scheme.genSym("quote");Scheme.genSym("a")]; Scheme.list [Scheme.genSym("quote");Scheme.genSym("b")]]) (Scheme.Cell (ref (Scheme.genSym "a"), ref (Scheme.genSym "b")))|>
        test "(eq? 'a 'a)" (Scheme.list [Scheme.genSym("eq?"); Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]; Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]]) (Scheme.true')|>
        test "(eq? 'a 'b)" (Scheme.list [Scheme.genSym("eq?"); Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]; Scheme.list [Scheme.genSym("quote"); Scheme.genSym("b")]]) (Scheme.false')|>
        test "(pair? '(a b c))" (Scheme.list [Scheme.genSym("pair?"); Scheme.list [Scheme.genSym("quote"); Scheme.list [Scheme.genSym("a"); Scheme.genSym("b"); Scheme.genSym("c")]]]) (Scheme.true')|>
        test "(pair? 'a)" (Scheme.list [Scheme.genSym("pair?"); Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]]) Scheme.false' |>

        test "(define a 'b)" (Scheme.list [Scheme.genSym("define");Scheme.genSym("a");Scheme.list[Scheme.genSym("quote");Scheme.genSym("b")]]) (Scheme.genSym "a") |>
        test "a" (Scheme.genSym "a") (Scheme.genSym "b")|>
        test "(lambda (x) x)" (Scheme.list [Scheme.genSym "lambda"; Scheme.list [Scheme.genSym "x"]; Scheme.genSym "x"]) (Scheme.Closure ([Scheme.Ld (0, 0); Scheme.Rtn], [])) |>
        test "((lambda (x) x) 'a)" (Scheme.list [Scheme.list [Scheme.genSym "lambda"; Scheme.list [Scheme.genSym "x"]; Scheme.genSym "x"]; Scheme.list[Scheme.genSym "quote"; Scheme.genSym "a"]]) (Scheme.genSym "a") |>
        test "(define Scheme.list (lambda x x))" (Scheme.list [Scheme.genSym "define"; Scheme.genSym "Scheme.list"; Scheme.list [Scheme.genSym "lambda"; Scheme.genSym "x"; Scheme.genSym "x"]]) (Scheme.genSym "Scheme.list") |>
        test "(Scheme.list 'a 'b 'c 'd 'e)" (Scheme.list [Scheme.genSym "Scheme.list"; Scheme.list [Scheme.genSym "quote";Scheme.genSym "a"]; Scheme.list [Scheme.genSym "quote";Scheme.genSym "b"]; Scheme.list [Scheme.genSym "quote";Scheme.genSym "c"]; Scheme.list [Scheme.genSym "quote";Scheme.genSym "d"]; Scheme.list [Scheme.genSym "quote";Scheme.genSym "e"]]) (Scheme.list [Scheme.genSym "a";Scheme.genSym "b";Scheme.genSym "c";Scheme.genSym "d";Scheme.genSym "e"]) |> 

        test "(define x 'a)" (Scheme.list [Scheme.genSym("define"); Scheme.genSym("x"); Scheme.list [Scheme.genSym("quote"); Scheme.genSym("a")]]) (Scheme.genSym "x")|>
        test "x" (Scheme.genSym "x") (Scheme.genSym "a")|>
        test "(define foo (lambda () x))" (Scheme.list [Scheme.genSym("define"); Scheme.genSym("foo"); Scheme.list [Scheme.genSym("lambda"); Scheme.nil; Scheme.genSym("x") ]]) (Scheme.genSym "foo")|>
        test "(foo)" (Scheme.list [Scheme.genSym("foo")]) (Scheme.genSym "a")|>
        test "(define bar (lambda (x) (foo)))"  (Scheme.list [Scheme.genSym("define"); Scheme.genSym("bar"); Scheme.list [Scheme.genSym("lambda"); Scheme.list[Scheme.genSym("x")]; Scheme.list[Scheme.genSym("foo")] ]]) (Scheme.genSym "bar")|>
        test "(bar 'b)" (Scheme.list [Scheme.genSym("bar"); Scheme.list [Scheme.genSym("quote"); Scheme.genSym("b")]]) (Scheme.genSym "a")|>

        (*  before: append to implements appendix#1 *)
        //test "foo" (Scheme.genSym("foo")) (Scheme.Closure ([Scheme.Ldg (Scheme.genSym "x"); Scheme.Rtn], [])) |>
        //test "bar" (Scheme.genSym("bar")) (Scheme.Closure ([Scheme.Args 0; Scheme.Ldg (Scheme.genSym "foo"); Scheme.App; Scheme.Rtn], [] )) |>
        (*  after: append to implements appendix#1 *)
        test "foo" (Scheme.genSym("foo")) (Scheme.Closure ([Scheme.Ldg (((Scheme.genSym "x"), ref (Scheme.genSym "a"))); Scheme.Rtn], [])) |>
        test "bar" (Scheme.genSym("bar")) (Scheme.Closure ([Scheme.Args 0; Scheme.Ldg (((Scheme.genSym "foo"), ref (Scheme.Closure ([Scheme.Ldg (((Scheme.genSym "x"), ref (Scheme.genSym "a"))); Scheme.Rtn], [])))); Scheme.App; Scheme.Rtn], [] )) |>
        (*  end: append to implements appendix#1 *)

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
      `(let ,(map (lambda (x) `(,x ,undef)) vars)
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
        `((lambda () ,undef))
      `((lambda () ,@args)))))        
" |>

        eval "(begin)" |>
        eval "(begin 1 2 3 4 5)" |>

        eval "
(define-macro cond
  (lambda args
    (if (null? args)
        undef
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
        undef
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

        eval "(apply cons '(1 2))" |> // (1 . 2)
        eval "(apply cons 1 '(2))" |> // (1 . 2)
        eval "(define a '(1 2 3 4 5))" |> // a
        eval "(define foo (lambda (a b . c) (set! a 10) (set! b 20) (set! c 30)))" |> // foo
        eval "(apply foo a)" |> // 30
        eval "a" |>     // (1 2 3 4 5)        

        eval "(define list (lambda x x))" |> // list
        eval "(define a #f)" |> // a
        eval "(list 'a 'b (call/cc (lambda (k) (set! a k) 'c)) 'd)" |> // (a b c d)
        eval "a" |> // (continuation (b a) () (ldc d args 4 ldg list app stop) ())
        eval "(a 'e)" |> // (a b e d)
        eval "(a 'f)" |> // (a b f d)

        eval "(define bar1 (lambda (cont) (display \"call bar1\\n\")))" |>
        eval "(define bar2 (lambda (cont) (display \"call bar2\\n\") (cont #f)))" |>
        eval "(define bar3 (lambda (cont) (display \"call bar3\\n\")))" |>
        eval "(define test (lambda (cont) (bar1 cont) (bar2 cont) (bar3 cont)))" |>

        eval "(call/cc (lambda (cont) (test cont)))" |> // call bar1\ncall bar2\n#f

        eval "
(define fact
  (lambda (n a)
    (if (= n 0)
        a
      (fact (- n 1) (* a n)))))" |>

        eval "fact" |>

        eval "
(define sum
  (lambda (x)
    (if (eqv? x 0)
        0
      (+ x (sum (- x 1))))))" |>

        eval "
(define sum
  (lambda (x)
    (if (eqv? x 0)
        0
      (+ x (sum (- x 1))))))" |>
        eval "(sum  100000)" |>
                  
        eval "
(define sum1
  (lambda (x a)
    (if (eqv? x 0)
        a
      (sum1 (- x 1) (+ a x)))))" |>
        eval "(sum1 100000 0)" |>

        repl |>

        //eval "" |>
        ignore;

        0

// http://www.ccs.neu.edu/home/dorai/mbe/mbe-imps.html
