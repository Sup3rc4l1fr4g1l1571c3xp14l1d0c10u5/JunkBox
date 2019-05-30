module ParserCore

open ParserCombinator
open ParserCombinator.ComputationExpressions
open ParserCombinator.OperatorExtension
open InterpreterCore

let isLower ch = ('a' <= ch) && (ch <= 'z')
let isUpper ch = ('A' <= ch) && (ch <= 'Z')
let isDigit ch = ('0' <= ch) && (ch <= '9')
let isIdHead ch = isLower(ch) || isUpper(ch) || (ch = '_')
let isIdTail ch = isLower(ch) || isUpper(ch) || (ch = '_') || isDigit(ch) 

let isReserveWord ident = 
    match ident with
    | "lin" | "un"
    | "true" | "false"
        -> true
    | _ -> false

let WhiteSpace = ParserCombinator.oneOf " \t\r\n"
    
let ws = WhiteSpace.Many()

let Ident = parser {
    let! _ = ws
    let! head = ParserCombinator.charOf(isIdHead)
    let! tails = ParserCombinator.charOf(isIdTail).Many()
    return head::tails |> System.String.Concat
}

let Symbol = parser {
    let! _ = ws
    let! sym = ParserCombinator.oneOf("=:;,*->").Many1()
    return sym |> System.String.Concat
}

let Bracket = parser {
    let! _ = ws
    let! bracket = ParserCombinator.oneOf("()<>[]{}")
    return bracket.ToString()
}

let ReserveWord p word = p.Where(fun x -> x = word)

let ID = Ident.Where(not << isReserveWord)
let QUAL = Ident.WhereSelect(function | "lin" -> Some Type.qual.Lin | "un" -> Some Type.qual.Un | _ -> None )
let BOOL = Ident.WhereSelect(function | "true" -> Some true | "false" -> Some false | _ -> None )

let LET = ReserveWord Ident "let"
let EQ = ReserveWord Symbol "="
let SEMICOLON = ReserveWord Symbol ";"
let IF = ReserveWord Ident "if"
let THEN = ReserveWord Ident "then"
let ELSE = ReserveWord Ident "else"
let COMMA = ReserveWord Ident ","
let LEFT = ReserveWord Bracket "<"
let RIGHT= ReserveWord Bracket ">"
let DOT = ReserveWord Symbol "."
let LAMBDA = ReserveWord Ident "lambda"
let COLON = ReserveWord Symbol "Colon"
let MULTI = ReserveWord Symbol "*"
let ARROW = ReserveWord Symbol "->"
let IN = ReserveWord Ident "in"
let SPLIT = ReserveWord Ident "split"
let AS = ReserveWord Ident "as"
let BoolT = ReserveWord Ident "bool"
let Eof = ParserCombinator.anyChar() |> ParserCombinator.not |> ParserCombinator.not
let LPAREN = ReserveWord Bracket "("
let RPAREN= ReserveWord Bracket ")"

let rec qualtype     = 
    let qualtype' = ParserCombinator.quote (fun () -> qualtype) 
    in
        ParserCombinator.choice [
            parser {let! q = QUAL in let! _ = BoolT in return Type.Bool q };
            parser {let! q = QUAL in let! _ = LPAREN in let! qt1 = qualtype' in let! _ = MULTI in let! qt2 = qualtype' in let! _ = RPAREN in return Type.Pair (q,qt1,qt2) };
            parser {let! q = QUAL in let! _ = LPAREN in let! qt1 = qualtype' in let! _ = ARROW in let! qt2 = qualtype' in let! _ = RPAREN in return Type.Fn (q,qt1,qt2) };
            parser {let! q = LPAREN in let! qt = qualtype' in let! _ = RPAREN in return qt };
        ]

and simpleterm = 
    let term' = ParserCombinator.quote (fun () -> term) 
    in
        ParserCombinator.choice [
            parser { let! id = ID in return Ast.Var id  };
            parser { let! q = QUAL in let! v = BOOL in return Ast.Boolean (q,v) };
            parser { let! _ = IF in let! cond = term' in let! _ = THEN in let! tt = term' in let! _ = ELSE in let! et = term' in return Ast.If (cond,tt,et) };
            parser { let! q = QUAL  in let! _ = LEFT  in let! lt = term'  in let! _ = COMMA  in let! rt = term'  in let! _ = RIGHT in return Ast.Pair (q,lt,rt) };
            parser { let! _ = SPLIT  in let! t = term'  in let! _ = AS  in let! lt = ID  in let! _ = COMMA  in let! rt = ID  in let! _ = IN  in let! b = term in return Ast.Split (t,lt,rt,b) };
            parser { let! q1 = QUAL  in let! _ = LAMBDA  in let! id = ID  in let! _ = COLON  in let! q2 = qualtype  in let! _ = DOT  in let! b = term in return Ast.Abs (q1,id,q2,b) };
            parser { let! _ = LPAREN  in let! t = term'  in let! _ = RPAREN in return t };
        ]

and term = 
    simpleterm.Many1().Select(fun ts -> List.reduceBack (fun y x ->  Ast.App (y,x)) ts)

and toplevel = 
    parser {
        let! _ = LET 
        let! id = ID
        let! _ = EQ 
        let! t = term
        let! _ = SEMICOLON 
        return (id,t)
    }
and toplevels = 
    toplevel.Many1()

and error reader p = 
    let rec loop p stack =
        let anytok = ParserCombinator.choice [Ident; Symbol; Bracket]
        in  match anytok reader p (p,"") with
            | ParserCombinator.ParserCombinator.Success (p,v,_) ->
                match (v,stack) with
                | "(", xs 
                | "[", xs  
                | "<", xs -> loop p (v::xs)
                | ")", ("("::xs) 
                | "]", ("["::xs) 
                | ">", ("<"::xs) -> loop p xs
                | ")", xs
                | "]", xs 
                | ">", xs -> loop p xs
                | ";", xs when List.length xs = 0 -> p 
                | _  , xs -> loop p xs
            | ParserCombinator.ParserCombinator.Fail (p,(p',f)) ->
                failwith "syntax miss"
    in  loop p []

