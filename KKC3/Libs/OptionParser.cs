using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public class OptionParser {

        private interface IOptionEntry {
            string name { get; }
            int Fire(string[] args, int i);
        }

        private class SimpleOptionEntry : IOptionEntry {
            public string name { get; }
            public int argc { get; }
            public Action<string[]> action { get; }
            public Func<string[], bool> validation { get; }

            public SimpleOptionEntry(string name, int argc, Action<string[]> action, Func<string[], bool> validation) {
                this.name = name;
                this.argc = argc;
                this.action = action;
                this.validation = validation;
            }

            public int Fire(string[] args, int i) {
                if (args.Length < i + 1 + argc) {
                    Console.Error.WriteLine($"引数{name}には{argc}個の引数が必要ですが、{argc}個しか指定されていません。");
                    return -1;
                }
                var argv = new string[argc];
                Array.Copy(args, i + 1, argv, 0, argc);
                if (this.validation != null && this.validation(argv) == false) {
                    Console.Error.WriteLine($"引数{name}の値が不正です。");
                    return -1;
                }
                this.action(argv);
                return argc;
            }
        }

        private Dictionary<string, IOptionEntry> table = new Dictionary<string, IOptionEntry>();

        public OptionParser Regist(string name, Action action) {
            this.Regist(name, 0, (_) => action(), null);
            return this;
        }
        public OptionParser Regist(string name, int argc, Action<string[]> action) {
            this.Regist(name, argc, action, null);
            return this;
        }
        public OptionParser Regist(string name, int argc, Action<string[]> action, Func<string[], bool> validation) {
            this.table[name] = new SimpleOptionEntry(name, argc, action, validation);
            return this;
        }

        public string[] Parse(string[] args) {
            for (var i = 0; i < args.Length; i++) {
                IOptionEntry entry;
                if (table.TryGetValue(args[i], out entry)) {
                    var ret = entry.Fire(args, i);
                    if (ret < 0) {
                        return null;
                    } else {
                        i += ret;
                    }
                } else {
                    var rest = new string[args.Length - i];
                    Array.Copy(args, i, rest, 0, rest.Length);
                    return rest;
                }
            }
            return new string[0];
        }
    }
}
