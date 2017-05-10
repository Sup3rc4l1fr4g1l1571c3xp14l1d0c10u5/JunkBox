using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace XmlViewer {
    sealed class XmlViewerPanel : Control {
        public XmlViewerPanel() {
            this.DoubleBuffered = true;
            this.Font = new Font(FontFamily.GenericMonospace, this.Font.Size);
            this.Paint += this.DoPaint;
            this.MouseWheel += this.DoMouseWheel;
            this.MouseDown += this.DoMouseDown;
        }

        private class Entry {
            public int Id { get; }
            public int Level { get; }
            public Point Location { get; }
            public Size Size { get; }
            public string Text { get; }

            public Entry(int id, int level, Point location, Size size, string text) {
                this.Id = id;
                this.Level = level;
                this.Location = location;
                this.Size = size;
                this.Text = text;
            }
        }

        private readonly List<Entry> entries = new List<Entry>();

        public class Document
        {
            private readonly List<Node> _nodes = new List<Node>();
            private readonly Dictionary<int, List<int>> _child = new Dictionary<int, List<int>>();
            private readonly Dictionary<int, int> _parent = new Dictionary<int, int>();
            private readonly Dictionary<int, List<int>> _attributes = new Dictionary<int, List<int>>();
            private readonly Dictionary<int, int> _attrOwner = new Dictionary<int, int>();

            private abstract class Node
            {
                public Document Owner { get; }
                public int Id { get; }

                protected Node(Document owner, int id)
                {
                    this.Owner = owner;
                    this.Id = id;
                }
            }

            private class Element : Node
            {
                public string Name { get; }

                public Element(Document owner, int id, string name) : base(owner, id)
                {
                    this.Name = name;
                }
            }

            private class Attribute : Node
            {
                public string Name { get; }
                public string Value { get; }

                public Attribute(Document owner, int id, string name, string value) : base(owner, id)
                {
                    this.Name = name;
                    this.Value = value;
                }
            }

            public int CreateElement(string name) {
                var id = _nodes.Count;
                _nodes.Add(new Element(this, id, name));
                return id;
            }

            public int CreateElement(int parent, string name) {
                var id = _nodes.Count;
                _nodes.Add(new Element(this, id, name));
                List<int> childs;
                if (_child.TryGetValue(parent, out childs) == false) {
                    childs = new List<int>();
                    _child[parent] = childs;
                }
                childs.Add(id);
                _parent[id] = parent;
                return id;
            }

            public int CreateAttribute(int node, string name, string value) {
                var id = _nodes.Count;
                _nodes.Add(new Attribute(this, id, name, value));
                List<int> elements;
                if (_attributes.TryGetValue(node, out elements) == false) {
                    elements = new List<int>();
                    _attributes[node] = elements;
                }
                elements.Add(id);
                _attrOwner[id] = node;
                return id;
            }

            public string StartTagString(int id) {
                var e = _nodes[id] as Element;
                if (e == null) {
                    return null;
                }
                List<int> attr;
                if (_attributes.TryGetValue(id, out attr))
                {
                    return
                        $"<{e.Name}{String.Join("", attr.Select(x => _nodes[x] as Attribute).Where(x => x != null).Select(x => $" {x.Name}=\"{x.Value}\""))}>";
                }
                else
                {
                    return
                        $"<{e.Name}>";
                }
            }

            public string EndTagString(int id) {
                var e = _nodes[id] as Element;
                if (e == null) {
                    return null;
                }
                return $"</{e.Name}>";
            }
        }

        public Document doc = new Document();

        public bool LoadXmlFromFile(string path) {
            entries.Clear();
            Dictionary<int,int> depthlist = new Dictionary<int, int>();
            using (var graphics = this.CreateGraphics())
            using (var reader = XmlReader.Create(path)) {
                //reader.MoveToContent();
                int y = 0;
                int lastdepth = -1;
                while (reader.Read()) {
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                        {
                            var elem = (reader.Depth == 0)
                                ? doc.CreateElement(reader.Name)
                                : doc.CreateElement(depthlist[reader.Depth - 1], reader.Name);
                            if (reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    doc.CreateAttribute(elem, reader.Name, reader.Value);
                                }
                                reader.MoveToElement();
                            }
                            depthlist[reader.Depth] = elem;
                            var e = doc.StartTagString(elem);
                            var size = graphics.MeasureString(e, this.Font, new PointF(0, 0),
                                StringFormat.GenericTypographic);
                            var entry = new Entry(
                                elem,
                                reader.Depth,
                                new Point(0, y),
                                Size.Ceiling(size),
                                e
                            );
                            lastdepth = reader.Depth;
                            y += (int) Math.Round(size.Height);
                            entries.Add(entry);
                            break;
                        }
                        case XmlNodeType.Text:
                            //e = reader.Value;
                            break;
                        case XmlNodeType.CDATA:
                            //e = $"<![CDATA[{reader.Value}]]>";
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            //e = $"<?{reader.Name} {reader.Value}?>";
                            break;
                        case XmlNodeType.Comment:
                            //e = $"<!--{reader.Value}-->";
                            break;
                        case XmlNodeType.XmlDeclaration:
                            //e = $"<?xml version='1.0'?>";
                            break;
                        case XmlNodeType.Document:
                            break;
                        case XmlNodeType.DocumentType:
                            //e = $"<!DOCTYPE {reader.Name} [{reader.Value}]>";
                            break;
                        case XmlNodeType.EntityReference:
                            //e = reader.Name;
                            break;
                        case XmlNodeType.EndElement:
                        {
                            while (lastdepth >= reader.Depth)
                            {
                                var elem = depthlist[lastdepth];
                                var e = doc.EndTagString(elem);
                                var size = graphics.MeasureString(e, this.Font, new PointF(0, 0),
                                    StringFormat.GenericTypographic);
                                var entry = new Entry(
                                    elem,
                                    lastdepth,
                                    new Point(0, y),
                                    Size.Ceiling(size),
                                    e
                                );
                                y += (int) Math.Round(size.Height);
                                entries.Add(entry);
                                lastdepth--;
                            }
                            break;

                        }
                        default:
                            break;
                    }
                }
            }
            return true;
        }


        private int scrollY = 0;
        private int selectedid = -1;

        private void DoMouseWheel(object sender, MouseEventArgs e) {
            scrollY -= (e.Delta / 120) * this.Font.Height;
            if (scrollY < 0) { scrollY = 0; }
            if (scrollY > entries.Last().Location.Y) { scrollY = entries.Last().Location.Y; }
            this.Invalidate();
        }

        private void DoPaint(object sender, PaintEventArgs e) {
            using (var it = entries.SkipWhile(x => x.Location.Y + x.Size.Height < scrollY).GetEnumerator()) {
                if (it.MoveNext() == false) {
                    return;
                }
                int y = it.Current.Location.Y - scrollY;
                do {
                    var entry = it.Current;
                    if (entry.Id == selectedid)
                    {
                        e.Graphics.DrawString(entry.Text, this.Font, Brushes.Red, entry.Level * (int)this.Font.Size * 2, y);
                    } else { 
                        e.Graphics.DrawString(entry.Text, this.Font, Brushes.Black, entry.Level * (int)this.Font.Size * 2, y);
                    }
                    y += entry.Size.Height;
                    if (y >= this.ClientSize.Height) {
                        break;
                    }
                } while (it.MoveNext());
            }
        }

        private void DoMouseDown(object sender, MouseEventArgs e)
        {
            selectedid = -1;
            this.Invalidate();
            using (var it = entries.SkipWhile(x => x.Location.Y + x.Size.Height < scrollY).GetEnumerator()) {
                if (it.MoveNext() == false) {
                    return;
                }
                int y = it.Current.Location.Y - scrollY;
                do {
                    var entry = it.Current;
                    var area = new Rectangle(entry.Level * (int)this.Font.Size * 2, y, entry.Size.Width, entry.Size.Height);
                    if (area.Contains(e.Location))
                    {
                        selectedid = entry.Id;
                        break;
                    }
                    y += entry.Size.Height;
                    if (y >= this.ClientSize.Height) {
                        break;
                    }
                } while (it.MoveNext());
            }
        }


    }
}
