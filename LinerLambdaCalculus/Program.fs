// https://letexpr.hatenablog.com/entry/2018/11/15/222959
// https://github.com/na0214/linear-lambda-calculus/tree/master/src
// http://www.cs.cmu.edu/~fp/courses/linear/handouts/linlam.pdf
// https://keigoimai.info/papers/linocaml-paper.pdf
// http://oleg.fi/gists/posts/2018-09-13-regular-expressions-of-types.html
module LinerLambdaCalculus

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
        let discard = // lin λx:lin bool.(lin λf:lin (un bool -> lin bool).lin true) (lin λy:un bool.x)
            Ast.App (
                Ast.Abs (
                    Type.Lin,
                    "x",
                    Type.Bool Type.Lin, 
                    Ast.Abs (
                        Type.Lin, 
                        "f", 
                        Type.Fn (Type.Un,Type.Bool Type.Un, Type.Bool Type.Lin),
                        Ast.Boolean (Type.Lin, true)
                    )
                ),
                (Ast.Abs (Type.Lin, "y", Type.Bool Type.Un, Ast.Var("x")))
            )
        let context = []
        let ret = TypeCheck.type_check discard context
        in
            printfn "%A" ret;
            0 // 整数の終了コードを返します

[<EntryPoint>]
let main argv = 
    Program.run argv
