using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace X86Asm.generator {
    using X86Asm.ast.statement;
    using X86Asm.ast.operand;
    using X86Asm.model;

    public abstract class DirectivePattern {
        public string Name { get; }
        public DirectivePattern(string name) {
            this.Name = name;
        }
        public abstract bool IsMatch(DirectiveStatement statement);
        public abstract void OnComputeLabelOffsets(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            Dictionary<int, uint> sectionSizeTable,
            List<Tuple<Section,string>> globalLabels
            );
        public abstract void OnAssembleBytes(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            uint[] sectionSizeTable,
            IDictionary<string, Symbol> symbolTable
            );

    }

    public class SectionDirectivePattern : DirectivePattern {
        public SectionDirectivePattern(string name) : base(name) { }
        public override bool IsMatch(DirectiveStatement statement) {
            return statement.Name == this.Name && statement.Arguments.Count == 0;
        }
        public override void OnComputeLabelOffsets(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            Dictionary<int, uint> sectionSizeTable,
            List<Tuple<Section, string>> globalLabels
        ) {
            if (currentSectionIndex != -1) {
                sectionSizeTable[currentSectionIndex] = offset;
            }
            currentSectionIndex = sections.FindIndex(x => x.name == directive.Name);
            if (currentSectionIndex == -1) {
                currentSectionIndex = sections.Count();
                sections.Add(new Section() { name = directive.Name, index = (uint)currentSectionIndex });
            }
            if (sectionSizeTable.ContainsKey(currentSectionIndex) == false) {
                sectionSizeTable[currentSectionIndex] = 0;
            }
            offset = sectionSizeTable[currentSectionIndex];
        }
        public override void OnAssembleBytes(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            uint[] sectionSizeTable,
            IDictionary<string, Symbol> symbolTable
        ) {
            if (currentSectionIndex != -1) {
                sectionSizeTable[currentSectionIndex] = offset;
            }
            currentSectionIndex = sections.FindIndex(x => x.name == directive.Name);
            if (currentSectionIndex == -1) {
                throw new Exception("辻褄が合わない");
            }
            offset = sectionSizeTable[currentSectionIndex];
        }
    }

    public class GlobalDirectivePattern : DirectivePattern {
        public GlobalDirectivePattern(string name) : base(name) { }
        public override bool IsMatch(DirectiveStatement statement) {
            return statement.Name == this.Name && statement.Arguments.Count == 1 && statement.Arguments[0] is Label;
        }
        public override void OnComputeLabelOffsets(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            Dictionary<int, uint> sectionSizeTable,
            List<Tuple<Section, string>> globalLabels
            ) {
            globalLabels.Add(Tuple.Create(sections[currentSectionIndex],((ast.operand.Label)directive.Arguments[0]).Name));
        }
        public override void OnAssembleBytes(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
           uint[] sectionSizeTable,
            IDictionary<string, Symbol> symbolTable
        ) {
            // skip
        }
    }

    public class ConstantDirectivePattern : DirectivePattern {
        public ConstantDirectivePattern(string name, uint size) : base(name) { this.Size = size; }

        public uint Size { get; private set; }

        public override bool IsMatch(DirectiveStatement statement) {
            return statement.Name == this.Name && statement.Arguments.Count > 0;
        }
        public override void OnComputeLabelOffsets(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            Dictionary<int, uint> sectionSizeTable,
            List<Tuple<Section, string>> globalLabels
            ) {
            offset += (uint)directive.Arguments.Count * Size;
        }
        public override void OnAssembleBytes(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            uint[] sectionSizeTable,
            IDictionary<string, Symbol> symbolTable
        ) {
            switch (this.Size) {
                case 1:
                    sections[currentSectionIndex].data.Write(directive.Arguments.SelectMany(x => ((IImmediate)x).GetValue(symbolTable).To1Byte()).ToArray(), 0, directive.Arguments.Count * 1);
                    offset += (uint)directive.Arguments.Count * 1;
                    break;
                case 2:
                    sections[currentSectionIndex].data.Write(directive.Arguments.SelectMany(x => ((IImmediate)x).GetValue(symbolTable).To2Bytes()).ToArray(), 0, directive.Arguments.Count * 1);
                    offset += (uint)directive.Arguments.Count * 2;
                    break;
                case 4:
                    sections[currentSectionIndex].data.Write(directive.Arguments.SelectMany(x => ((IImmediate)x).GetValue(symbolTable).To4Bytes()).ToArray(), 0, directive.Arguments.Count * 1);
                    offset += (uint)directive.Arguments.Count * 4;
                    break;
                default:
                    throw new Exception("サイズが不正。");
            }
        }
    }
    public class AsciiDirectivePattern : DirectivePattern {
        public AsciiDirectivePattern(string name) : base(name) { }
        public override bool IsMatch(DirectiveStatement statement) {
            return statement.Name == this.Name && statement.Arguments.Count == 1 && statement.Arguments[0] is StringLiteral;
        }
        public override void OnComputeLabelOffsets(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
            Dictionary<int, uint> sectionSizeTable,
            List<Tuple<Section, string>> globalLabels
            ) {
            offset += (uint)(directive.Arguments[0] as StringLiteral).Bytes.Length;
        }
        public override void OnAssembleBytes(
            DirectiveStatement directive,
            List<Section> sections,
            ref int currentSectionIndex,
            ref uint offset,
           uint[] sectionSizeTable,
            IDictionary<string, Symbol> symbolTable
        ) {
            sections[currentSectionIndex].data.Write((directive.Arguments[0] as StringLiteral).Bytes, 0, (directive.Arguments[0] as StringLiteral).Bytes.Length);
        }
    }

}
