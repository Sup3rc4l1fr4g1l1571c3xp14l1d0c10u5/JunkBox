using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCPP
{
    /// <summary>
    /// ‹ó”’î•ñ
    /// </summary>
    public class SpaceInfo
    {
        /// <summary>
        /// ‹ó”’ƒ`ƒƒƒ“ƒN
        /// </summary>
        public class Chunk {
            public Position Pos { get; set; }
            public string Space { get; set; }

            public Chunk Dup() {
                return new Chunk() {
                    Pos = this.Pos,
                    Space = this.Space,
                };
            }
        }

        public List<Chunk> Chunks { get; }
        public int Length { get { return Chunks.Sum(x => x.Space.Length); } }

        public SpaceInfo() {
            Chunks = new List<Chunk>();
        }

        private void AddChunk(Position pos) {
            Chunks.Add(
                new Chunk() {
                    Pos = pos,
                    Space = "",
                }
            );
        }
        public void Append(Position pos, char ch) {
            if (Chunks.Count == 0 || !Chunks.Last().Pos.Equals(pos)) {
                AddChunk(pos);
            }
            Chunks.Last().Space += ch;
        }
        public void Append(Position pos, string str) {
            if (Chunks.Count == 0 || !Chunks.Last().Pos.Equals(pos)) {
                AddChunk(pos);
            }
            Chunks.Last().Space += str;
        }
        public override string ToString() {
            return Chunks.Aggregate(new StringBuilder(), (s,x) => s.Append(x.Space)).ToString(); ;
        }

        public bool Any() {
            return Chunks.Count > 0;
        }
    }
}