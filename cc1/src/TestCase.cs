using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    public class TestCase {
        List<Func<bool>> TestTask = new List<Func<bool>>();
        public void RunTest() {
            int failed = 0;
            foreach (var task in TestTask) {
                if (!task.Invoke()) {
                    failed++;
                }
            }
            Console.WriteLine($"Total / Success / Failed: {TestTask.Count} / {TestTask.Count - failed} / {failed}");
        }
        public bool AddTest(string path) {
            if (System.IO.File.Exists(path) == false) {
                Console.Error.WriteLine($"ファイル {path} が見つかりません。");
                return false;
            }
            var src = System.IO.File.ReadAllText(path);
            var match = Regex.Match(src, @"^/\*\*@(.*?)@\*\*/", RegexOptions.Singleline);
            if (match.Success == false) {
                Console.Error.WriteLine($"ファイル {path} にはテスト記述が含まれません。");
            }
            var descriptions = Regex.Split(match.Groups[0].Value, "\r?\n").Where(x => !String.IsNullOrWhiteSpace(x));

            Dictionary<string, string> settings = new Dictionary<string, string>();
            foreach (var description in descriptions) {
                var index = description.IndexOf(":");
                if (index == -1) {
                    continue;
                }
                var key = description.Substring(0, index).Trim();
                var value = description.Substring(index + 1).Trim();
                if (string.IsNullOrWhiteSpace(key)) {
                    continue;
                }
                settings[key] = value;
            }

            var needKey = new[] { "spec", "assertion" };
            if (!needKey.All(x => settings.ContainsKey(x))) {
                Console.Error.WriteLine($"ファイル {path} のspec記述が不完全です。");
                return false;
            }

            TestTask.Add(() => {
                var p = new Parser(src);
                if (string.IsNullOrWhiteSpace(settings["assertion"])) {
                    try {
                        p.Parse();
                        return true;
                    } catch (Exception e) {
                        Console.Error.WriteLine($"");
                        Console.Error.WriteLine($"{path}:");
                        Console.Error.WriteLine($"例外 {e.GetType().Name } が発生しました。");
                        if (e is CompilerException) {
                            var ce = e as CompilerException;
                            Console.Error.WriteLine($"({ce.Start}, {ce.End}): {ce.Message}");
                        } else {
                            Console.Error.WriteLine($"{e.Message}");
                            Console.Error.WriteLine($"{e.StackTrace}");
                        }
                        return false;
                    }
                } else {
                    try {
                        p.Parse();
                        Console.Error.WriteLine($"");
                        Console.Error.WriteLine($"{path}:");
                        Console.Error.WriteLine($"例外 {settings["assertion"]} が発生しませんでした。");
                        return false;
                    } catch (Exception e) {
                        if (e.GetType().Name.ToUpper() == settings["assertion"].ToUpper()) {
                            // ok.
                            return true;
                        } else {
                            Console.Error.WriteLine($"");
                            Console.Error.WriteLine($"{path}:");
                            Console.Error.WriteLine($"例外 {settings["assertion"]} が発生せず、 例外 {e.GetType().Name } が発生しました。{e.Message}");
                            if (e is CompilerException) {
                                var ce = e as CompilerException;
                                Console.Error.WriteLine($"({ce.Start}, {ce.End}): {ce.Message}");
                            } else {
                                Console.Error.WriteLine($"{e.Message}");
                                Console.Error.WriteLine($"{e.StackTrace}");
                            }
                            return false;
                        }
                    }
                }
            });

            return true;
        }
    }

}