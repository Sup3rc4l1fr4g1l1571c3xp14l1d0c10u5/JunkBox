// https://letexpr.hatenablog.com/entry/2018/11/15/222959
// https://github.com/na0214/linear-lambda-calculus/tree/master/src
// http://www.cs.cmu.edu/~fp/courses/linear/handouts/linlam.pdf
// https://keigoimai.info/papers/linocaml-paper.pdf
// http://oleg.fi/gists/posts/2018-09-13-regular-expressions-of-types.html
namespace LinerLambdaCalculus

module Program =
    open InterpreterCore
    open ParserCore

    let run argv =
        //let discard = // lin λx:lin bool.(lin λf:un (un bool -> lin bool).lin true) (un λy:un bool.x)
        //    Ast.App (
        //        Ast.Abs (
        //            Type.Lin,
        //            "x",
        //            Type.Bool Type.Lin, 
        //            Ast.Abs (
        //                Type.Lin, 
        //                "f", 
        //                Type.Fn (Type.Un,Type.Bool Type.Un, Type.Bool Type.Lin),
        //                Ast.Boolean (Type.Lin, true)
        //            )
        //        ),
        //        (Ast.Abs (Type.Un, "y", Type.Bool Type.Un, Ast.Var("x")))
        //    )
        //let discard = // lin λx:lin bool.(lin λf:lin (un bool -> lin bool).lin true) (lin λy:un bool.x)
        //    Ast.App (
        //        Ast.Abs (
        //            Type.Lin,
        //            "x",
        //            Type.Bool Type.Lin, 
        //            Ast.Abs (
        //                Type.Lin, 
        //                "f", 
        //                Type.Fn (Type.Un,Type.Bool Type.Un, Type.Bool Type.Lin),
        //                Ast.Boolean (Type.Lin, true)
        //            )
        //        ),
        //        (Ast.Abs (Type.Lin, "y", Type.Bool Type.Un, Ast.Var("x")))
        //    )
        //let ret = TypeCheck.type_check discard context
        let reader = ParserCombinator.Reader.create System.Console.In

        let ast = ParserCore.toplevels reader ParserCombinator.Position.head (ParserCombinator.Position.head ,"")
        let _ = printfn "%A" ast
        match ast with
        | ParserCombinator.ParserCombinator.Success (_,ast,_) ->
            let rec loop ast con =
                match ast with
                | [] -> con
                | (n,t)::xs ->
                    let (t',c) = TypeCheck.type_check t con
                    let _ = printfn "%A : %A" t ((n,t')::c)
                    let l = loop xs ((n,t')::c)
                    in  match List.tryFind (fun (k,v) -> n = k) c with  
                        | Some _ -> l @ con
                        | None -> con @ (List.where (fun (k,v) -> n <> k) l)
            let con = loop ast [] 
            let unused = List.filter (fun (s,x) -> Type.get_qual x = Type.Lin && s <> "main") con
            in  
                if List.length unused = 0 
                then Type.print_context con 
                else raise (TypeCheck.UnUsedError (fst (List.head unused)))

        | ParserCombinator.ParserCombinator.Fail (_,(p',s)) -> 
            let _ = printfn "failed %A: %s" p' s
            in  ()

        //let rec loop context p =
        //    let ast = ParserCore.toplevel reader p (ParserCombinator.Position.head ,"")
        //    let _ = printfn "%A" ast
        //    let (p,context) = 
        //        match ast with  
        //            | ParserCombinator.ParserCombinator.Success (p,(id,v),_) -> 
        //                let (t',c) = TypeCheck.type_check v context
        //                let _ = printfn "%A" t'
        //                in  (p,(id,t')::context)
        //            | ParserCombinator.ParserCombinator.Fail (_,(p',s)) -> 
        //                let _ = printfn "failed %A: %s" p' s
        //                in  (ParserCore.error reader p ,context)

        //    let (reader, p) = ParserCombinator.Reader.trunc reader p
        //    in  loop context p
        //in  loop [] ParserCombinator.Position.head
                
    [<EntryPoint>]
    let main argv = 
        run argv;
        0
