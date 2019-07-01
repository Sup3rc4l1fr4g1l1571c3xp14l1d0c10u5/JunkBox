// https://github.com/kyoDralliam/LinearManipulations

module AbstractSyntaxTree =
    type t = 
        | Base of string
        | Var of string
        | Pro of t
        | App of t * t list

        | Times of t * t
        | Plus of t * t
        | Star of t * t
        | Larr of t * t
        | OfCourse of t 
        | Arr of t * t
        | TypeSym of string
        | TermSym of string
        | TypeDef of string * t
        | TermDef of string * t
        | Abs of (string * t) list * t
        | Des of string * string * t * t
        | Pair of t * t
        | InjLeft of t * t * t
        | InjRight of t * t * t
        | Match of t * (string * t) * (string * t)


module ParserCore =
    open ParserCombinator
    open ParserCombinator.ParserCombinator
    open ParserCombinator.ComputationExpressions
    open ParserCombinator.OperatorExtension
    open AbstractSyntaxTree

    let identHead = charOf (fun x -> ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z') || (x = '_'))
    let identBody = charOf (fun x -> ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z') || (x = '_') || ('0' <= x && x <= '9'))
    let symbolChar = oneOf "\\/!<>=+*-:.;,|"
    let bracketChar = oneOf "(){}[]"

    let ws = oneOf " \r\t\n"

    let sym = parser {
        let! _ = ws.Many()
        let! body = symbolChar.Many1()
        return System.String.Concat(body)
    }
    let symbol s = sym.Where(fun x -> x = s)

    let bra = parser {
        let! _ = ws.Many()
        let! body = bracketChar
        return string body
    }
    let bracket s = bra.Where(fun x -> x = s)

    let id = parser {
        let! _ = ws.Many()
        let! head = identHead
        let! body = identBody.Many()
        return System.String.Concat(head::body)
    }
    let ident s = id.Where(fun x -> x = s)

    let FORALL = symbol "\\/"
    let LAM = symbol "\\"
    let OF_COURSE = symbol "!"
    let LPAR = bracket "("
    let RPAR = bracket ")"
    let LBR = bracket "{"
    let RBR = bracket "}"
    let LSQBR = bracket "["
    let RSQBR = bracket "]"
    let LT = symbol "<"
    let GT = symbol ">"
    let EQ = symbol "="
    let PIPE = symbol "|"
    let PLUS = symbol "+"
    let STAR = symbol "*"
    let ARR = symbol "->"
    let LARR = symbol "-:>"
    let DOT = symbol "."
    let COLON = symbol ":"
    let SEMICOLON = symbol ";"
    let COMMA = symbol ","
    let FUN = ident "fun"
    let DESTRUCT = ident "destruct"
    let MATCH = ident "match"
    let AS = ident "as"
    let IN = ident "in"
    let TYPE = ident "type"
    let LET = ident "let"
    let VAR = id.Where(fun x -> (List.contains x ["fun";"destryct";"match";"as";"in";"type";"let"]) = false)
    let EOF = parser {
        let! _ = ws.Many()
        let! _ = eof()
        return ()
    }
    let linear_type_ = hole ()
    let linear_type = quote (fun () -> !linear_type_)

    let linear_type_term = 
            choice [
                parser { let! s = VAR       in return Base s };
                parser { let! _ = OF_COURSE in let! t = linear_type in return OfCourse t };
                parser { let! _ = LPAR      in let! t = linear_type in let! _ = RPAR    in return t };
                parser { let! _ = LSQBR     in let! v = VAR         in let! _ = RSQBR   in return TypeSym v };
            ]

    let _ = 
        linear_type_ :=         
            parser {
                let! head = linear_type_term
                let! tails = 
                    repeat(
                        parser {
                            let! p = choice[PLUS;STAR;LARR]
                            let! r = linear_type_term 
                            return (p, r)
                        }
                    )
                return List.fold (fun s x -> match x with | ("+", y) -> Plus(s,y) | ("*",y) -> Star(s,y) | ("-:>",y) -> Larr(s,y) ) head tails
            }

    let type_def =
        parser {
            let! _ = TYPE
            let! v = VAR 
            let! _ = EQ 
            let! t = linear_type  
            return TypeDef (v, t)
        }

    let typed_var = 
        choice [
            parser {
                let! _ = LPAR    in let! s = VAR    in let! _ = COLON    in let! t = linear_type    in let! _ = RPAR     in return (s, t)
            };
            parser {
                let! s = VAR    in let! _ = COLON    in let! t = linear_type    in return (s, t)
            };
        ]
    let pair_var = 
        choice [
            parser {
                let! _ = LPAR    in let! x = VAR    in let! _ = COMMA    in let! y = VAR    in let! _ = RPAR     in return (x, y)
            };
            parser {
                let! x = VAR    in let! _ = COMMA    in let! y = VAR    in return (x, y)
            };
        ]

    let term_without_app_ = hole ()
    let term_without_app = quote (fun () -> !term_without_app_)
    let term_ = hole ()
    let term = quote (fun () -> !term_)

    let _ = 
        term_without_app_ :=  
            choice [
                parser { let! s = VAR       in return Var s };
                parser { let! _ = LSQBR     in let! v = VAR                     in let! _ = RSQBR           in return TermSym v };
                parser { let! _ = LAM       in let! args = typed_var.Many1()    in let! _ = DOT             in let! t = term            in return Abs (args,t) };
                parser { let! _ = FUN       in let! args = typed_var.Many1()    in let! _ = ARR             in let! t = term            in return Abs (List.map (fun (x,y) -> (x,OfCourse y)) args, t) };
                parser { let! _ = LPAR      in let! t = term                    in let! _ = RPAR            in return t };
                parser { let! _ = LT        in let! t1 = term                   in let! _ = SEMICOLON       in let! t2 = term           in let! _ = GT   in return Pair(t1, t2) };
                parser { let! _ = DESTRUCT  in let! t = term                    in let! _ = AS              in let! p = pair_var        in let! _ = IN   in let! u = term            in return Des (fst p, snd p, t, u) };
                parser { let! _ = LBR       in let! t = term                    in let! _ = COLON           in let! typ1 = linear_type  in let! _ = PIPE in let! typ2 = linear_type  in let! _ = RBR     in return InjLeft  (t,typ1,typ2) };
                parser { let! _ = LBR       in let! typ1 = linear_type          in let! _ = PIPE            in let! t = term            in let! _ =COLON in let! typ2 = linear_type  in let! _ = RBR     in return InjRight (t,typ1,typ2) };
                parser { let! _ = MATCH     in let! t = term                    in let! _ = AS              in let! _ = PIPE.Option()   in let! x1 = VAR in let! _ = ARR             in let! t1 = term   in let! _ = PIPE     in let! x2 = VAR    in let! _ = ARR in let! t2 = term   in return Match (t,(x1, t1),(x2, t2)) }
                parser { let! _ = OF_COURSE in let! t = term_without_app        in return Pro t }
            ]  


    let _ = term_ :=    
                parser {
                    let! terms = term_without_app.Many1()
                    return match terms with | x::[] -> x | x::xs -> App (x, xs)
                };

    let term_def = 
        parser {
            let! _ = LET 
            let! v = VAR 
            let! _ = EQ 
            let! t = term   
            return TermDef (v,t)
        }


    let defs = choice [term_def;type_def]

    let file = parser {
        let! df = defs.Many1()
        let! _ = EOF
        return df
    }

module LinerType =
    type t =
        | Base of string
        | TypeVar of string
        | Times of t * t
        | Plus of t * t
        | OfCourse of t 
        | Arr of t * t
        | Forall of string * t 
        | TypeAbs of string * t    
    
    let equal t1 t2 =
        match (t1, t2) with 
            | Base s1, Base s2 -> s1 = s2
            | TypeVar x1, TypeVar x2 -> x1 = x2
            | Times (ta1, ta2), Times (tb1, tb2) 
            //| With (ta1, ta2), With (tb1, tb2)
            | Plus (ta1, ta2), Plus (tb1, tb2) 
            | Arr (ta1, ta2), Arr (tb1, tb2) -> System.Object.ReferenceEquals(ta1,tb1) && System.Object.ReferenceEquals(ta2, tb2)
            | OfCourse t1, OfCourse t2 -> System.Object.ReferenceEquals(t1, t2)
            | Forall (x1, t1), Forall (x2, t2) -> x1 = x2 && System.Object.ReferenceEquals(t1, t2)
            | TypeAbs (x1, t1), TypeAbs (x2, t2) -> x1 = x2 && System.Object.ReferenceEquals(t1, t2)
            | _ -> false        

module LambdaType =
    type t =
        | Var of string 
        | App of t * t list
        | Abs of (string * LinerType.t) list * t
        | Des of string * string * t * t
        | Pair of t * t
        | Pro of t
        | InjLeft of t * LinerType.t * LinerType.t
        | InjRight of t * LinerType.t * LinerType.t
        | Match of t * (string * t) * (string * t)

module LinearLambda =
    type context = { term_symbol_table : (string * LambdaType.t) list; type_symbol_table: (string * LinerType.t) list }

    let rec eval_type context ast =
        match ast with
        | AbstractSyntaxTree.Base s -> (context, LinerType.Base s)
        | AbstractSyntaxTree.OfCourse t -> 
            let (context, t) = eval_type context t
            in  (context, LinerType.OfCourse t)
        | AbstractSyntaxTree.TypeSym v -> 
            let (k,v) = List.find (fun (k,_) -> k = v) context.type_symbol_table
            in  (context, v)
        | AbstractSyntaxTree.Plus (l,r) -> 
            let (context, l) = eval_type context l
            let (context, r) = eval_type context r
            in  (context, LinerType.Plus(l,r))
        | AbstractSyntaxTree.Star (l,r) -> 
            let (context, l) = eval_type context l
            let (context, r) = eval_type context r
            in  (context, LinerType.Times(l,r))
        | AbstractSyntaxTree.Larr (l,r) -> 
            let (context, l) = eval_type context l
            let (context, r) = eval_type context r
            in  (context, LinerType.Arr(l,r))


    let rec eval_term context ast =
        match ast with
        | AbstractSyntaxTree.App (lam, args) ->
            let (context, l1) = eval_term context lam
            let (context, l2) = List.fold (fun (con,ret) x -> let (c,r) = eval_term con x in (c,r::ret)) (context,[]) args
            in  (context, LambdaType.App (l1, l2))
        | AbstractSyntaxTree.Var v -> (context, LambdaType.Var v)
        | AbstractSyntaxTree.TermSym v -> 
            let (k,v) = List.find (fun (k,_) -> k = v) context.term_symbol_table
            in  (context, v)
        | AbstractSyntaxTree.Abs (arg,body) -> 
            let (context, a) = 
                List.fold (
                    fun (con,ret) (a,b) -> 
                        let (c,r) = eval_type context b
                        in  (c, (a,r)::ret)
                    ) (context,[]) arg
            let (context, b) = eval_term context body
            in  (context, LambdaType.Abs (a,b))
        | AbstractSyntaxTree.Pair (car,cdr) ->
            let (context, x) = eval_term context car
            let (context, y) = eval_term context cdr
            in  (context, LambdaType.Pair(x, y))
        | AbstractSyntaxTree.Des (f,s,t,u) ->
            let (context, t) = eval_term context t
            let (context, u) = eval_term context u
            in  (context, LambdaType.Des(f,s,t,u))
        | AbstractSyntaxTree.InjLeft (x,y,z) ->
            let (context, x) = eval_term context x
            let (context, y) = eval_type context y
            let (context, z) = eval_type context z
            in  (context, LambdaType.InjLeft(x,y,z))
        | AbstractSyntaxTree.InjRight (x,y,z) ->
            let (context, x) = eval_term context x
            let (context, y) = eval_type context y
            let (context, z) = eval_type context z
            in  (context, LambdaType.InjRight(x,y,z))
        | AbstractSyntaxTree.Match (x,(n,y),(m,z)) ->
            let (context, x) = eval_term context x
            let (context, y) = eval_term context y
            let (context, z) = eval_term context z
            in  (context, LambdaType.Match (x,(n,y),(m,z)))
        | AbstractSyntaxTree.Pro t ->
            let (context, t) = eval_term context t
            in  (context, LambdaType.Pro t)

    let eval_def context ast =
        match ast with
        | AbstractSyntaxTree.TypeDef (name,term) -> 
            let (context, t) = eval_type context term
            in  { context with type_symbol_table = (name,t)::context.type_symbol_table }
        | AbstractSyntaxTree.TermDef (name,term) -> 
            let (context, t) = eval_term context term
            in  { context with term_symbol_table = (name,t)::context.term_symbol_table }
        | _ -> failwith "not def"

    let eval (asts:AbstractSyntaxTree.t list) =
        let context = { term_symbol_table= []; type_symbol_table= [] }
        in
            List.fold (fun con x -> eval_def con x) context asts

module Typing = 
    exception NotWellTyped of string * ((LambdaType.t * LinerType.t) list)
    exception NotLinear of LambdaType.t * LinerType.t

    let rec get_linear_part t = 
        match t with
        | LinerType.OfCourse t -> get_linear_part t
        | _ -> t

    let rec is_contraction t1 t2 = 
        if System.Object.ReferenceEquals(t1, t2) 
        then true
        else match t2 with 
             | LinerType.OfCourse t2' -> is_contraction t1 t2'
             | _ -> false

    let map_diff m1 m2 =
        List.choose (fun v -> let (k1,v1) = v in if List.exists( fun (k2,v2) -> k1 = k2 ) m1 then None else Some v) m2


    let rec type_of_term_aux ctx t =
      match t with
      | LambdaType.Var x -> 
            let typ = List.tryFind( fun (k,v) -> k = x) ctx 
            in  match typ with 
                | Some (_,(LinerType.OfCourse _ as t)) -> (t, ctx)
                | Some (_,t) -> 
                    let ctx = List.choose (fun v -> let (k1,v1) = v in if k1 = x then None else Some v) ctx 
                    in  (t, ctx)
                | None -> failwith "OutOfContext"

      | LambdaType.App (t, args) ->
            let (ctx', types0) = 
                let fold_fun (ctx, acc) t0 = 
                    let typ, ctx' = type_of_term_aux ctx t0 
                    in  ctx', typ :: acc 
                in  List.fold fold_fun (ctx, []) (t::args)
            let (typ_t, types) =
                match List.rev types0 with 
                | typ_t :: types -> (typ_t, types)
                | _ -> failwith "assert"
            let rec cut (largs, t1) (arg, t2) = 
                match t1 with 
                    | LinerType.OfCourse t1' -> cut (largs, t1') (arg, t2)
                    | LinerType.Arr (t1s, t1b) when is_contraction t1s t2 -> (arg :: largs, t1b)
                    | LinerType.Arr _ -> 
                        let t0 = match t with 
                                 | LambdaType.App (t,tl') -> LambdaType.App (t,tl' @ largs)
                                 | _ -> LambdaType.App (t, largs)
                        raise (NotWellTyped ("Argument of wrong type", [t0, t1; arg, t2]))
                    | _ -> 
                        let t0 = match t with 
                                 | LambdaType.App (t,tl') -> LambdaType.App (t,tl' @ largs)
                                 | _ -> LambdaType.App (t, largs)
                        raise (NotWellTyped ("Should have an arrow type", [t0, t1; arg, t2]))
            in  snd (List.fold cut ([],typ_t) (List.zip args types)), ctx'

      | LambdaType.Abs (args, t) -> 
          let aux () =
            let typ_t, ctx' = 
              let ctx = 
                List.fold (fun ctx (x, typ_x) -> (x,typ_x)::ctx) ctx args 
              in type_of_term_aux ctx t in
            let add_arg_type (x,typ) typ0 = 
              match typ with 
                | LinerType.OfCourse _ -> LinerType.Arr (typ, typ0)
                | _ -> 
                    if List.exists (fun (k,v) -> k = x) ctx'
                    then raise (NotLinear (LambdaType.Var x, typ))
                    else LinerType.Arr (typ, typ0)
            in 
            let typ = List.foldBack add_arg_type args typ_t in
      
            typ, ctx'
          in

          variable_shadowing (List.map fst args) ctx aux

      | LambdaType.Des (x1,x2,x,t) -> 
          let typ_x, ctx' = type_of_term_aux ctx x in
          let typ_x1, typ_x2 = 
            match (get_linear_part typ_x) with 
              | LinerType.Times (typ_x1, typ_x2) -> typ_x1, typ_x2
              | _ -> 
                  raise (NotWellTyped ("Should have pair type", [x, typ_x]))
          in
      
          let aux () =
            let ctx'' = (x1,typ_x1)::(x2,typ_x2)::ctx' in
            type_of_term_aux ctx'' t 
          in

          variable_shadowing [x1 ; x2] ctx' aux

      | LambdaType.Pair (t1, t2) -> 
          let typ_t1, ctx' = type_of_term_aux ctx t1 in
          let typ_t2, ctx'' = type_of_term_aux ctx' t2 in
          LinerType.Times (typ_t1,typ_t2), ctx'' 

      | LambdaType.Pro t ->
          let typ_t, ctx' = type_of_term_aux ctx t in
          let env = map_diff ctx ctx' in
          if List.forall (fun (_,t) -> match t with LinerType.OfCourse _ -> true | _ -> false) env
          then LinerType.OfCourse typ_t, ctx'
          else failwith "cannot promote this term"

      | LambdaType.InjLeft (t, typ1, typ2) ->
          let typ_t, ctx' = type_of_term_aux ctx t in
          if System.Object.ReferenceEquals(typ_t,typ1) 
          then LinerType.Plus (typ1, typ2), ctx'
          else failwith "bad type annotation in sum construct"

      | LambdaType.InjRight (t, typ1, typ2) ->
          let typ_t, ctx' = type_of_term_aux ctx t in
          if System.Object.ReferenceEquals(typ_t,typ2) 
          then LinerType.Plus (typ1, typ2), ctx'
          else failwith "bad type annotation in sum construct"

      | LambdaType.Match (t, (x1, t1), (x2, t2)) ->
          let typ_t, ctx' = type_of_term_aux ctx t in
          let typ1, typ2 = 
            match (get_linear_part typ_t) with 
              | LinerType.Plus (typ1, typ2) -> (typ1, typ2)
              | _ -> raise (NotWellTyped ("Should have plus type", [t, typ_t]))
          in 

          let aux () = 
            let ctx' = (x1,typ1)::(x2,typ2)::ctx' in
            let typ_t1, ctx_1 = type_of_term_aux ctx' t1 in
            let typ_t2, ctx_2 = type_of_term_aux ctx' t2 in
            if System.Object.ReferenceEquals(typ_t1,typ_t2) && (List.forall2 (fun x y -> System.Object.ReferenceEquals(x,y)) ctx_1 ctx_2)
            then typ_t1, ctx_1
            else 
              let msg = "Each branch of the match should have the same type and use the same context" in
              raise (NotWellTyped (msg, [t1, typ_t1 ; t2, typ_t2]))
          in
        
          variable_shadowing [ x1 ; x2 ] ctx' aux

    and variable_shadowing var_list ctx f = 
      let has_old_binding = List.filter (fun x -> List.exists (fun (k,v) -> k =x) ctx) var_list in
      let old_binding = List.map (fun x -> (x,List.find (fun (k,v) -> k =x) ctx |> snd)) has_old_binding in
  
      let typ, ctx' = f () in

      let ctx'' = List.fold (fun acc (x,y) -> (x,y):: acc) ctx' old_binding in

      typ, ctx''

    let type_of_term ctx term = 
      let typ, ctx' = type_of_term_aux ctx term in
      if List.forall (fun (_,t) -> match t with LinerType.OfCourse _ -> true | _ -> false) ctx'
      then typ
      else failwith "this derivation is not linear"


module MainProgram =
    [<EntryPoint>]
    let main argv =
        let reader = ParserCombinator.Reader.create(System.Console.In)
        in 
            match ParserCore.file reader ParserCombinator.Position.head (ParserCombinator.Position.head,"") with
            | ParserCombinator.ParserCombinator.Success (p,v,f) ->
                let _ = printfn "%A" v
                let ret = LinearLambda.eval v
                let _ = printfn "%A"  ret
                in  0
            | ParserCombinator.ParserCombinator.Fail (p,f) ->
                let _ = printfn "%A:%A" p f
                in  -1
