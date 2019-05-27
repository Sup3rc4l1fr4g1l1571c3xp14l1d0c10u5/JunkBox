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

let simpleterm = choice [
        | ID { Ast.Var $1 }
        | QUAL BOOL { Ast.Boolean ($1,$2) }
        | IF term THEN term ELSE term { Ast.If ($2,$4,$6) }
        | QUAL LEFT term COMMA term RIGHT { Ast.Pair ($1,$3,$5) }
        | SPLIT term AS ID COMMA ID IN term { Ast.Split ($2,$4,$6,$8) }
        | QUAL LAMBDA ID COLON qualtype DOT term { Ast.Abs ($1,$3,$5,$7) }
        | LPAREN term RPAREN { $2 }
    ]

let term = simpleterm.Many1().Select(fun ts -> List.reduceBack (fun y x ->  Ast.App (y,x)) ts)

let toplevel = 
    rep {
        let! _ = LET 
        let! id = ID
        let! _ = EQ 
        let! t = term
        let! _ = SEMICOLON 
        return (id,t)
    }
  //  | { [] }
  //  | Eof { [] }
  //  | error
  //  { failwith
  //(Printf.sprintf "parse error near characters %d-%d"
  //   (Parsing.symbol_start ())
  //   (Parsing.symbol_end ()))
  //  }
  //  ;




qualtype:
QUAL BoolT { Type.Bool $1 }
    | QUAL LPAREN qualtype MULTI qualtype RPAREN { Type.Pair ($1,$3,$5) }
    | QUAL LPAREN qualtype ARROW qualtype RPAREN { Type.Fn ($1,$3,$5) }
    | LPAREN qualtype RPAREN { $2 }
;
