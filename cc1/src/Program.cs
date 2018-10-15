using System;
using System.Linq;
using System.Diagnostics;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {
    class Program {

        static void Main(string[] args) {

            //
            // I Do Not Know C. 
            //

            if (Debugger.IsAttached == false) {
                CommonMain(args);
            } else {
                DebugMain(args);
            }
        }

        private class CommandLineOptions {
            public string OutputFile;
            public string AstFile;
            public bool FlagSyntaxOnly;
            public string OutputEncoding = System.Text.Encoding.Default.WebName;
            public string[] Args = new string[0];

            public void Validation(Action<string> act) {
                // check outputEncoding
                var enc = System.Text.Encoding.GetEncodings().FirstOrDefault(x => String.Equals(x.Name, OutputEncoding, StringComparison.OrdinalIgnoreCase));
                if (enc == null) {
                    act($"指定されたエンコーディング名 {OutputEncoding} は利用可能なエンコーディングに一致しません。");
                } else {
                    Console.OutputEncoding = enc.GetEncoding();
                }

                // check input source
                if (Args.Length == 0) {
                    act("コンパイル対象のCソースファイルを１つ指定してください。");
                } else if (Args.Length > 1) {
                    act("コンパイル対象のCソースファイルが２つ以上指定されています。");
                } else if (System.IO.File.Exists(Args[0]) == false) {
                    act($"ファイル {Args[0]} が見つかりません。処理を中止します。");
                }

                // check output
                if (OutputFile == null) {
                    OutputFile = System.IO.Path.ChangeExtension(Args[0], "s");
                }

            }
        }

        static void CommonMain(string[] args) {

            var opts = new CommandLineOptionsParser<CommandLineOptions>()
                .Entry(@"-o", 1, (t, s) => {
                    t.OutputFile = s[0];
                    return true;
                })
                .Entry(@"-ast", 1, (t, s) => {
                    t.AstFile = s[0];
                    return true;
                })
                .Entry(@"-console-output-encoding", 1, (t, s) => {
                    t.OutputEncoding = s[0];
                    return true;
                })
                .Entry(@"-fsyntax-only", 0, (t, s) => {
                    t.FlagSyntaxOnly = true;
                    return true;
                })
                .Default((t, s) => {
                    t.Args = s;
                    return true;
                })
                .Parse(new CommandLineOptions(), args);

            opts.Validation((e) => {
                Logger.Error(e);
                Environment.Exit(-1);
            });

            try {
                var ret = new Parser(System.IO.File.ReadAllText(opts.Args[0]), opts.Args[0]).Parse();
                if (opts.AstFile != null) {
                    using (var o = new System.IO.StreamWriter(opts.AstFile)) {
                        o.WriteLine(ret.Accept(new ToSExprVisitor(), null).ToString());
                    }
                }

                if (opts.FlagSyntaxOnly == false) {
                    using (var o = new System.IO.StreamWriter(opts.OutputFile)) {
                        var compiler = new Compiler();
                        compiler.Compile(ret, o);
                    }
                }
            } catch (CompilerException e) {
                Logger.Error(e.Start, e.End, e.Message);
                Logger.Error(e.StackTrace);
                Environment.Exit(-1);
            } catch (Exception e) {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
                Environment.Exit(-1);
            }

        }

        static void DebugMain(string[] args) {
            var ret = new Parser(System.IO.File.ReadAllText(@"C:\Users\whelp\Documents\Visual Studio 2017\Projects\AnsiCParser\AnsiCParser\huge-table.c"), " <Debug>").Parse();
            return;
            var sexpr = ret.Accept(new ToSExprVisitor(), null);
            //*
            var interpreter = new Schene.SchemeInterpreter();
            interpreter.InterpreterWantsToPrint += (s, e) => Console.Write(e.WhatToPrint);
            interpreter.Evaluate($"(define ast '{new Schene.Writer(false).Write(sexpr)})");
            interpreter.Evaluate(@"(define repl (lambda () (let ((expr (begin (display ""cc1> "") (read (standard-input-port))))) (begin (write (eval expr)) (newline) (repl)))))");
            interpreter.Evaluate(@"
(define pp (lambda (s)
  (define do-indent (lambda (level)
    (dotimes (_ level) (write-char #\space))))
  (define pp-parenl (lambda ()
    (write-char #\()))
  (define pp-parenr (lambda ()
    (write-char #\))))
  (define pp-atom (lambda (e prefix)
    (when prefix (write-char #\space))
    (write e)))
  (define pp-list (lambda (s level prefix)
    (and prefix (do-indent level))
    (pp-parenl)
    (let loop ((s s)
               (prefix #f))
      (if (null? s)
          (pp-parenr)
          (let ((e (car s)))
            (if (list? e)
                (begin (and prefix (newline))
                       (pp-list e (+ level 1) prefix))
                (pp-atom e prefix))
            (loop (cdr s) #t))))))
  (if (list? s)
      (pp-list s 0 #f)
      (write s))
  (newline)))
");
            interpreter.Evaluate(@"(repl)");
            //*/
            using (var o = new System.IO.StreamWriter(@"test.s")) {
                var compiler = new Compiler();
                compiler.Compile(ret, o);
            }

        }
    }
}


/*
 * C89 Features 
 * - Lexer   : Complete
 * - Parser  : Complete
 * - Library : NO (now use glibc)
 * C99 Features
 * - __func__ : Complete
 * - Line comment : Complete
 * - Mix Statement and Declaration : Complete
 * - Flexible array member : Complete
 * - Variable length array : Not Supported. It's nothing, but harmful. 
 * - Complex numbers : Complete
 * - Imaginary numbers : Incomplete.
 * - Bool type : Complete.
 */
