using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Parsing;

namespace CParser2
{
    class Program
    {
        static void Main(string[] args)
        {

            foreach (var arg in args) {
//                using (var reader = new System.IO.StringReader(@"
//typedef unsigned long VALUE;
//table = ({ 
//	const VALUE args_to_new_ary[] = {block}; 
//	if (__builtin_constant_p(1)) { 
//		typedef int static_assert_rb_ary_new_from_args_check[1 - 2*!(((int)(sizeof(args_to_new_ary) / sizeof((args_to_new_ary)[0]))) == (1))]; 
//	}
//	rb_ary_new_from_values(((int)(sizeof(args_to_new_ary) / sizeof((args_to_new_ary)[0]))), args_to_new_ary); 
//});
//")) {
//                    var target = new Source("", reader);
//                    var ret = CParser.block_item_list(target, Position.Empty, Position.Empty, new CParser.ParserStatus());
//                }
                using (var reader = new StreamReader(arg)) {
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    var ret = CParser.Parse(reader);
                    sw.Stop();
                    Console.WriteLine($"Result: {(ret.Success ? "Success" : $"Failed at {ret.FailedPosition}")}");
                    Console.WriteLine($"Position: {ret.Position}");
                    Console.WriteLine($"Failed Position: {ret.FailedPosition}");
                    Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                    //Console.ReadLine();
                    //return;
                    //ret?.Value.Save("ast.xml");


                    // 分析
                    var tracedData = new System.Collections.Generic.List<Tuple<string,Tuple<int,int, int, int>,System.Collections.Generic.Dictionary<Position, int[/*3*/]>>>();
                    foreach (var kv1 in Combinator.TraceInfo) {
                        var ruleName = kv1.Key;
                        var ruleTraces = kv1.Value;

                        var histgram = new System.Collections.Generic.Dictionary<Position, int[/*3*/]>();
                        var callcnt = 0;
                        var callsucc = 0;
                        var callfail = 0;
                        foreach (var ruleTrace in ruleTraces) {
                            int[] counts;
                            if (histgram.TryGetValue(ruleTrace.Item1, out counts) == false) {
                                counts = new int[3];
                                histgram[ruleTrace.Item1] = counts;
                            }
                            counts[0]++;
                            callcnt++;
                            if (ruleTrace.Item2) {
                                counts[1]++;
                                callsucc++;
                            } else {
                                counts[2]++;
                                callfail++;
                            }
                        }
                        var maxrecheck = histgram.Any() ? histgram.Select(x => x.Value[0]).Max() : 0;
                        tracedData.Add(Tuple.Create(ruleName, Tuple.Create(callcnt, callsucc, callfail, maxrecheck), histgram));
                    }

                    tracedData = tracedData.OrderByDescending(x => x.Item2.Item1).ToList();
                }



            }

        }
    }
}



