using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSCPP {
    public struct Char {
        public Position position { get; }
        public int Value { get; }
        public override string ToString() {
            return $"{((char)Value)}";
        }
        public Char(Position pos, int value) {
            position = pos;
            Value = value;
        }

        public bool IsEof() {
            return Value == -1;
        }
    }
}