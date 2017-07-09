using System;

namespace MiniMAL
{
    namespace Interpreter
    {

        /// <summary>
        /// SECDマシンインタプリタ
        /// </summary>
        public static partial class SecdMachineInterpreter
        {
            /// <summary>
            /// Dumpレジスタ型
            /// </summary>
            public class DumpInfo
            {
                public LinkedList<ExprValue> Stack { get; }
                public LinkedList<LinkedList<ExprValue>> Env { get; }
                public LinkedList<Instructions> Code { get; }

                public DumpInfo(
                    LinkedList<ExprValue> stack,
                    LinkedList<LinkedList<ExprValue>> env,
                    LinkedList<Instructions> code

                )
                {
                    Stack = stack;
                    Env = env;
                    Code = code;
                }
            }

            /// <summary>
            /// 命令列の実行
            /// </summary>
            /// <param name="code">仮想マシン命令列</param>
            /// <param name="env"></param>
            /// <param name="builtin"></param>
            /// <returns></returns>
            public static Tuple<ExprValue, LinkedList<LinkedList<ExprValue>>> Run(LinkedList<Instructions> code,
                LinkedList<LinkedList<ExprValue>> env, Environment<ExprValue> builtin)
            {
                if (code == null)
                {
                    throw new ArgumentNullException(nameof(code));
                }
                if (env == null)
                {
                    throw new ArgumentNullException(nameof(env));
                }
                LinkedList<ExprValue> currentstack = LinkedList<ExprValue>.Empty;
                LinkedList<LinkedList<ExprValue>> currentenv = env;
                LinkedList<Instructions> currentcode = code;
                LinkedList<DumpInfo> currentdump = LinkedList<DumpInfo>.Empty;


                for (;;)
                {
                    LinkedList<ExprValue> nextstack;
                    LinkedList<LinkedList<ExprValue>> nextenv;
                    LinkedList<Instructions> nextcode;
                    LinkedList<DumpInfo> nextdump;

                    if (currentcode.Value is Instructions.Ld)
                    {
                        // ld <i> <j>
                        // E レジスタの <i> 番目のフレームの <j> 番目の要素をスタックに積む
                        var ld = currentcode.Value as Instructions.Ld;
                        var frame = LinkedList.At(ld.Frame, currentenv);
                        var val = LinkedList.At(ld.Index, frame);

                        nextstack = LinkedList.Extend(val, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Ldc)
                    {
                        // ldc <const>
                        // 定数 <const> をスタックに積む
                        var ldc = currentcode.Value as Instructions.Ldc;

                        nextstack = LinkedList.Extend(ldc.Value, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Ldext)
                    {
                        // ldext <symbol>
                        // シンボル <symbol> に対応する値を組み込み環境 builtin から探してスタックに積む
                        var ldext = currentcode.Value as Instructions.Ldext;

                        nextstack = LinkedList.Extend(Environment.LookUp(ldext.Symbol, builtin), currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Ldf)
                    {
                        // ldf <code>
                        // code からクロージャを生成してスタックに積む
                        var ldf = currentcode.Value as Instructions.Ldf;
                        ExprValue closure;
                        if (ldf.Function is Instructions.Ldf.Closure)
                        {
                            var c = ldf.Function as Instructions.Ldf.Closure;
                            closure = new ExprValue.ProcV("", c.Body, currentenv);
                        }
                        else if (ldf.Function is Instructions.Ldf.Primitive)
                        {
                            var p = ldf.Function as Instructions.Ldf.Primitive;
                            closure = new ExprValue.BProcV(p.Proc);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        nextstack = LinkedList.Extend(closure, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;

                    }
                    else if (currentcode.Value is Instructions.App)
                    {
                        // app <n>
                        // スタックに積まれているクロージャと引数を取り出して関数呼び出しを行う
                        var app = currentcode.Value as Instructions.App;
                        var closure = LinkedList.At(0, currentstack);
                        currentstack = currentstack.Next;

                        if (closure is ExprValue.ProcV)
                        {
                            var val = LinkedList<ExprValue>.Empty;
                            for (int i = 0; i < app.Argn; i++)
                            {
                                val = LinkedList.Extend(currentstack.Value, val);
                                currentstack = currentstack.Next;
                            }

                            var newdump = new DumpInfo(currentstack, currentenv, currentcode.Next);

                            nextstack = LinkedList<ExprValue>.Empty;
                            nextenv = LinkedList.Extend(val, (closure as ExprValue.ProcV).Env);
                            nextcode = (closure as ExprValue.ProcV).Body;
                            nextdump = LinkedList.Extend(newdump, currentdump);
                        }
                        else if (closure is ExprValue.BProcV)
                        {
                            System.Diagnostics.Debug.Assert(app.Argn == 1);

                            var arg = currentstack.Value;
                            currentstack = currentstack.Next;
                            var ret = (closure as ExprValue.BProcV).Proc(arg);

                            nextstack = LinkedList.Extend(ret, currentstack);
                            nextenv = currentenv;
                            nextcode = currentcode.Next;
                            nextdump = currentdump;
                        }
                        else
                        {
                            throw new Exception.RuntimeErrorException($"App cannot eval: {closure}.");
                        }
                    }
                    else if (currentcode.Value is Instructions.Tapp)
                    {
                        // tapp <n>
                        // スタックに積まれているクロージャと引数を取り出して関数呼び出しを行う
                        // 末尾再帰するため、appと違いDumpを作らない
                        var tapp = currentcode.Value as Instructions.Tapp;
                        var closure = LinkedList.At(0, currentstack);
                        currentstack = currentstack.Next;

                        if (closure is ExprValue.ProcV)
                        {

                            var val = LinkedList<ExprValue>.Empty;
                            for (int i = 0; i < tapp.Argn; i++)
                            {
                                val = LinkedList.Extend(currentstack.Value, val);
                                currentstack = currentstack.Next;
                            }

                            nextstack = currentstack;
                            nextenv = LinkedList.Extend(val, (closure as ExprValue.ProcV).Env);
                            nextcode = (closure as ExprValue.ProcV).Body;
                            nextdump = currentdump;
                        }
                        else if (closure is ExprValue.BProcV)
                        {
                            System.Diagnostics.Debug.Assert(tapp.Argn == 1);

                            var arg = currentstack.Value;
                            currentstack = currentstack.Next;
                            var ret = (closure as ExprValue.BProcV).Proc(arg);

                            nextstack = LinkedList.Extend(ret, currentstack);
                            nextenv = currentenv;
                            nextcode = currentcode.Next;
                            nextdump = currentdump;
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else if (currentcode.Value is Instructions.Ent)
                    {
                        // ent <n>
                        // スタックに積まれている引数を取り出して環境を作る
                        var app = currentcode.Value as Instructions.Ent;

                        var val = LinkedList<ExprValue>.Empty;
                        for (int i = 0; i < app.Argn; i++)
                        {
                            val = LinkedList.Extend(currentstack.Value, val);
                            currentstack = currentstack.Next;
                        }

                        nextstack = currentstack;
                        nextenv = LinkedList.Extend(val, currentenv);
                        nextcode = currentcode.Next;
                        nextdump = currentdump;

                    }
                    else if (currentcode.Value is Instructions.Rtn)
                    {
                        // rtn
                        // 関数呼び出しから戻る
                        //var rtn = code.Value as Instructions.Rtn;
                        var val = LinkedList.At(0, currentstack);
                        var prevdump = currentdump.Value;

                        nextstack = LinkedList.Extend(val, prevdump.Stack);
                        nextenv = prevdump.Env;
                        nextcode = prevdump.Code;
                        nextdump = currentdump.Next;
                    }
                    else if (currentcode.Value is Instructions.Sel)
                    {
                        // sel <ct> <cf>
                        // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                        var sel = currentcode.Value as Instructions.Sel;
                        var val = LinkedList.At(0, currentstack);
                        var cond = (val as ExprValue.BoolV).Value;
                        var newdump = new DumpInfo(null, null, currentcode.Next);

                        nextstack = currentstack.Next;
                        nextenv = currentenv;
                        nextcode = (cond ? sel.TrueClosure : sel.FalseClosure);
                        nextdump = LinkedList.Extend(newdump, currentdump);
                    }
                    else if (currentcode.Value is Instructions.Selr)
                    {
                        // selr <ct> <cf>
                        // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                        // dumpを更新しない
                        var selr = currentcode.Value as Instructions.Selr;
                        var val = LinkedList.At(0, currentstack);
                        var cond = (val as ExprValue.BoolV).Value;
                        //var newdump = new DumpInfo(null, null, code.Next);

                        nextstack = currentstack.Next;
                        nextenv = currentenv;
                        nextcode = (cond ? selr.TrueClosure : selr.FalseClosure);
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Join)
                    {
                        // join
                        // 条件分岐(sel)から合流する 
                        //var join = code.Value as Instructions.Join;
                        var prevdump = currentdump.Value;

                        nextstack = currentstack;
                        nextenv = currentenv;
                        nextcode = prevdump.Code;
                        nextdump = currentdump.Next;
                    }
                    else if (currentcode.Value is Instructions.Pop)
                    {
                        // pop <ct> <cf>
                        // スタックトップの値を取り除く
                        //var pop = code.Value as Instructions.Pop;

                        nextstack = currentstack.Next;
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Stop)
                    {
                        // stop
                        // スタックトップの値が真ならば <ct> を実行する。偽ならば <cf> を実行する
                        break;

                        //_nextstack = stack;
                        //_nextenv = env;
                        //_nextcode = code.Next;
                        //_nextdump = dump;
                        //_nextglobal = global;
                    }
                    else if (currentcode.Value is Instructions.Halt)
                    {
                        // halt
                        // エラーを生成して停止する
                        throw new Exception.HaltException((currentcode.Value as Instructions.Halt).Message);
                        //break;

                        //_nextstack = stack;
                        //_nextenv = env;
                        //_nextcode = code.Next;
                        //_nextdump = dump;
                        //_nextglobal = global;
                    }
                    else if (currentcode.Value is Instructions.Tuple)
                    {
                        // tuple
                        // スタックに積まれている値をn個取り出してタプルを作る
                        var tuple = currentcode.Value as Instructions.Tuple;
                        var val = new ExprValue[tuple.Size];
                        for (int i = 0; i < tuple.Size; i++)
                        {
                            val[i] = currentstack.Value;
                            currentstack = currentstack.Next;
                        }
                        var ret = new ExprValue.TupleV(val);

                        nextstack = LinkedList.Extend(ret, currentstack);
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Dum)
                    {
                        // dum
                        // 空環境を積む
                        //var dum = code.Value as Instructions.Dum;

                        nextstack = currentstack;
                        nextenv = LinkedList.Extend(LinkedList<ExprValue>.Empty, currentenv);
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Rap)
                    {
                        // rap <n>
                        // 空環境を引数で置き換える
                        var rap = currentcode.Value as Instructions.Rap;
                        var closure = LinkedList.At(0, currentstack);
                        currentstack = currentstack.Next;

                        if (closure is ExprValue.ProcV)
                        {
                            var val = LinkedList<ExprValue>.Empty;
                            for (int i = 0; i < rap.Argn; i++)
                            {
                                val = LinkedList.Extend(currentstack.Value, val);
                                currentstack = currentstack.Next;
                            }
                            var newdump = new DumpInfo(currentstack, currentenv, currentcode.Next);
                            currentenv.Replace(val);
                            nextstack = LinkedList<ExprValue>.Empty;
                            nextenv = currentenv;
                            nextcode = (closure as ExprValue.ProcV).Body;
                            nextdump = LinkedList.Extend(newdump, currentdump);
                        }
                        else if (closure is ExprValue.BProcV)
                        {
                            System.Diagnostics.Debug.Assert(rap.Argn == 1);

                            var arg = currentstack.Value;
                            currentstack = currentstack.Next;
                            var ret = (closure as ExprValue.BProcV).Proc(arg);

                            nextstack = LinkedList.Extend(ret, currentstack);
                            nextenv = currentenv;
                            nextcode = currentcode.Next;
                            nextdump = currentdump;
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else if (currentcode.Value is Instructions.Rent)
                    {
                        // rent <n>
                        // 空環境を引数で置き換える
                        var rap = currentcode.Value as Instructions.Rent;

                        var val = LinkedList<ExprValue>.Empty;
                        for (int i = 0; i < rap.Argn; i++)
                        {
                            val = LinkedList.Extend(currentstack.Value, val);
                            currentstack = currentstack.Next;
                        }

                        currentenv.Replace(val);
                        nextstack = currentstack;
                        nextenv = currentenv;
                        nextcode = currentcode.Next;
                        nextdump = currentdump;
                    }
                    else if (currentcode.Value is Instructions.Opti)
                    {
                        // opti <isnull>
                        // isnullが真の場合はNone値をスタックに積む
                        // isnullが偽の場合はスタックから値を取り出して Some で包んでスタックに積む
                        var opti = currentcode.Value as Instructions.Opti;

                        if (opti.None)
                        {
                            nextstack = LinkedList.Extend(ExprValue.OptionV.None, currentstack);
                            nextenv = currentenv;
                            nextcode = currentcode.Next;
                            nextdump = currentdump;
                        }
                        else
                        {
                            var val = currentstack.Value;
                            currentstack = currentstack.Next;
                            nextstack = LinkedList.Extend(new ExprValue.OptionV(val), currentstack);
                            nextenv = currentenv;
                            nextcode = currentcode.Next;
                            nextdump = currentdump;
                        }

                    }
                    else
                    {
                        throw new NotSupportedException($"instruction {currentcode.Value} is not supported");
                    }

                    currentstack = nextstack;
                    currentenv = nextenv;
                    currentcode = nextcode;
                    currentdump = nextdump;

                }
                return Tuple.Create(currentstack.Value, currentenv);
            }
        }
    }

}