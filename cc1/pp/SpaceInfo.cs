using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCPP
{
    public class SpaceInfo
    {
        public class Chunk {
            public Position Pos { get; set; }
            public string Space { get; set; }

            public Chunk Dup() {
                return new Chunk() {
                    //File = this.File,
                    //Line = this.Line,
                    //Column = this.Column,
                    Pos = this.Pos,
                    Space = this.Space,
                };
            }
        }
        public List<Chunk> chunks { get; }
        public int Length => chunks.Sum(x => x.Space.Length);

        public SpaceInfo()
        {
            chunks = new List<Chunk>();
        }

        private void AddChunk(Position p) {
            chunks.Add(
                new Chunk() {
                    //File = f,
                    //Line = f.Line,
                    //Column = f.Column,
                    Pos = p,
                    Space = "",
                }
            );
        }
        public void Append(Position p, char p0) {
            if (!chunks.Any() || !chunks.Last().Pos.Equals(p)) {
                AddChunk(p);
            }
            chunks.Last().Space += p0;
        }
        public void Append(Position p, string p0) {
            if (!chunks.Any() || !chunks.Last().Pos.Equals(p)) {
                AddChunk(p);
            }
            chunks.Last().Space += p0;
        }
        public override string ToString() {
            return chunks.Aggregate(new StringBuilder(), (s,x) => s.Append(x.Space)).ToString(); ;
        }

        public bool Any()
        {
            return chunks.Any();
        }
    }
}