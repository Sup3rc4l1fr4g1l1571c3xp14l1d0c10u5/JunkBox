using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Parsing;

namespace CParser2 {




    class Program {

        static void Main(string[] args) {




            foreach (var arg in args) {
                //using (var reader = new System.IO.StringReader(@"<>=")) {
                //    var source = new Source("", reader);
                //    var ret = CParser.symbol(source, Position.Empty, Position.Empty, new CParser.ParserStatus());
                //}
                using (var reader = new StreamReader(arg)) {
                    var sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    var ret = CParser.Parse(reader, (status, result, succpos, failpos) => {
                        sw.Stop();
                        Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms");
                        Console.WriteLine($"Result: {(status ? "Success" : $"Failed")}");
                        Console.WriteLine($"Position: {succpos}");
                        Console.WriteLine($"FailedPosition: {failpos}");
                        if (status) {
                            System.IO.File.WriteAllText("ast.c", new StringWriteVisitor().Write(result));
                        }
                        return result;
                    });
                    Console.ReadLine();
                    return;


                    // 分析
                    var tracedData = Combinator.TraceInfo.Select(kv1 => {
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
                        return new {
                            RuleName = ruleName,
                            TotalCallCount = callcnt,
                            SuccessCallCount = callsucc,
                            FailedCallCount = callfail,
                            Histgram = histgram.OrderByDescending(x => x.Value[0]).ToList()
                        };
                    }).ToList();

                    tracedData = tracedData.OrderByDescending(x => x.TotalCallCount).ToList();
                }



            }

        }

    }
}



