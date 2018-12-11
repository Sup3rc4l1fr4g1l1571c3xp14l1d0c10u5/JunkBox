/// <reference path="typings/pegjs.d.ts" />
/// <reference path="typings/generated-parser.d.ts" />

interface Array<T> {
    peek(): T;
}
Array.prototype.peek = function () {
    return this[this.length - 1];
};

function parseStr(str: string): string {
    let ret = "";
    for (let i = 0; i < str.length; i++) {
        if (str[i] !== "\\") {
            ret += str[i];
        } else {
            switch (str[i + 1]) {
                case "t": ret += "\t"; i += 1; break;
                case "r": ret += "\r"; i += 1; break;
                case "n": ret += "\n"; i += 1; break;
                default: ret += str[i + 1]; i += 1; break;
            }
        }
    }
    return ret;
}
function escapeStr(str: string): string {
    let ret = "";
    for (let i = 0; i < str.length; i++) {
        switch (str[i + 1]) {
            case "\t": ret += "\\t"; break;
            case "\r": ret += "\\r"; break;
            case "\n": ret += "\\n"; break;
            case "\\": ret += "\\\\"; break;
            case "\"": ret += "\\\""; break;
            default: ret += str[i]; break;
        }
    }
    return ret;
}

type AssignmentOperator = "*=" | "/=" | "%=" | "+=" | "-=" | "<<=" | ">>=" | ">>>=" | "<<<=" | "&=" | "^=" | "|=" | "**=" | "=";
type LogicalOperator = "&&" | "||";
type BitwiseOperator = "&" | "|" | "^";
type EqualityOperator = "==" | "!=";
type RelationalOperator = ">=" | ">" | "<=" | "<";
type ShiftOperator = "<<<" | "<<" | ">>>" | ">>";
type AdditiveOperator = "+" | "-";
type MultiplicativeOperator = "*" | "/" | "%";
type ExponentiationOperator = "**";
type UnaryOperator = "+" | "-" | "~" | "!";
type ConditionalOperator = EqualityOperator | RelationalOperator;
type BinaryOperator = BitwiseOperator | ShiftOperator | AdditiveOperator | MultiplicativeOperator | ExponentiationOperator;

function assignmentOperatorToBinaryOperator(op: AssignmentOperator): BinaryOperator {
    switch (op) {
        case "*=": return "*";
        case "/=": return "/";
        case "%=": return "%";
        case "+=": return "+";
        case "-=": return "-";
        case "<<=": return "<<";
        case ">>=": return ">>";
        case ">>>=": return ">>>";
        case "<<<=": return "<<<";
        case "&=": return "&";
        case "^=": return "^";
        case "|=": return "|";
        case "**=": return "**";
        default: throw new Error(`${op} is not AssignmentOperator.`);
    }
}

interface IStatement { }

interface IExpression { }

class Program {
    constructor(public statements: IStatement[]) { }
}

class EmptyStatement implements IStatement {
    constructor() { }
}

class IfStatement implements IStatement {
    constructor(public condExpr: IExpression, public thenStmt: IStatement, public elseStmt: IStatement) { }
}

class DoStatement implements IStatement {
    constructor(public condExpr: IExpression, public bodyStmt: IStatement) { }
}

class WhileStatement implements IStatement {
    constructor(public condExpr: IExpression, public bodyStmt: IStatement) { }
}

class ForStatement implements IStatement {
    constructor(public initExpr: IExpression, public condExpr: IExpression, public updateExpr: IExpression, public bodyStmt: IStatement) { }
}

class LexicalDeclaration implements IStatement {
    constructor(public binds: LexicalBinding[]) { }
}

class LexicalBinding {
    constructor(public ident: IdentifierLiteral, public initExpr: IExpression) { }
}

class SwitchStatement implements IStatement {
    constructor(public expr: IExpression, public clauses: CaseBlock[]) { }
}

class CaseBlock {
    constructor(public clauses: IClause[], public stmt: IStatement) { }
}

interface IClause { }

class CaseClause implements IClause {
    constructor(public expr: IExpression) { }
}

class DefaultClause implements IClause {
    constructor() { }
}

class BreakStatement implements IStatement {
    constructor() { }
}

class ContinueStatement implements IStatement {
    constructor() { }
}

class ReturnStatement implements IStatement {
    constructor(public expr: IExpression) { }
}

class BlockStatement implements IStatement {
    constructor(public statements: IStatement[]) { }
}

class ExpressionStatement implements IStatement {
    constructor(public expr: IExpression) { }
}

class CommaExpression implements IExpression {
    constructor(public expressions: IExpression[]) { }
}

class AssignmentExpression implements IExpression {
    constructor(public op: AssignmentOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class ConditionalExpression implements IExpression {
    constructor(public condExpr: IExpression, public thenExpr: IExpression, public elseExpr: IExpression) { }
}

class LogicalOrExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class LogicalAndExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class BitwiseOrExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class BitwiseXorExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class BitwiseAndExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class EqualityExpression implements IExpression {
    constructor(public op: EqualityOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class RelationalExpression implements IExpression {
    constructor(public op: RelationalOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class ShiftExpression implements IExpression {
    constructor(public op: ShiftOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class AdditiveExpression implements IExpression {
    constructor(public op: AdditiveOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class MultiplicativeExpression implements IExpression {
    constructor(public op: MultiplicativeOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class ExponentiationExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class UnaryExpression implements IExpression {
    constructor(public op: UnaryOperator, public rhs: IExpression) { }
}

class CallExpression implements IExpression {
    constructor(public lhs: IExpression, public args: IExpression[]) { }
}

class ArrayIndexExpression implements IExpression {
    constructor(public lhs: IExpression, public index: IExpression) { }
}

class ObjectMemberExpression implements IExpression {
    constructor(public lhs: IExpression, public member: IdentifierLiteral) { }
}

class EnclosedInParenthesesExpression implements IExpression {
    constructor(public expr: IExpression) { }
}

class ArrayLiteral implements IExpression {
    constructor(public values: IExpression[]) { }
}

class ObjectLiteral implements IExpression {
    constructor(public values: PropertyDefinition[]) { }
}

class PropertyDefinition {
    constructor(public name: IdentifierLiteral, public value: IExpression) { }
}

class FunctionExpression implements IExpression {
    constructor(public params: FunctionParameter[], public body: IStatement) { }
}

class FunctionParameter implements IExpression {
    constructor(public ident: IdentifierLiteral, public init: IExpression, public spread: boolean) { }
}

class NullLiteral implements IExpression {
    constructor() { }
}

class BooleanLiteral implements IExpression {
    constructor(public value: boolean) { }
}

class NumericLiteral implements IExpression {
    constructor(public value: number) { }
}

class StringLiteral implements IExpression {
    constructor(public value: string) { }
}

class IdentifierLiteral implements IExpression {
    constructor(public value: string) { }
}

type Instruction =
    LabelInstruction |
    JumpInstruction |
    BranchInstruction |
    BindInstruction |
    BindArgInstruction |
    ReturnInstruction |
    EnterInstruction |
    LeaveInstruction |
    PopInstruction |
    SimpleAssignmentInstruction |
    MemberAssignmentInstruction |
    ConditionalInstruction |
    CallInstruction |
    MemberCallInstruction |
    ObjectMemberInstruction |
    ArrayLiteralInstruction |
    ObjectLiteralInstruction |
    StringLiteralInstruction |
    FunctionInstruction |
    NumericLiteralInstruction |
    NullLiteralInstruction |
    BooleanLiteralInstruction |
    IdentifierLiteralInstruction |
    LoadInstruction;

type LabelInstruction = {
    kind: "LabelInstruction";
    pc: number;
}
type JumpInstruction = {
    kind: "JumpInstruction";
    label: LabelInstruction;
}
type BranchInstruction = {
    kind: "BranchInstruction";
    thenLabel: LabelInstruction;
    elseLabel: LabelInstruction;
}
type BindInstruction = {
    kind: "BindInstruction";
    ident: string;
}
type BindArgInstruction = {
    kind: "BindArgInstruction";
    ident: string;
    spread: boolean;
}
type ReturnInstruction = {
    kind: "ReturnInstruction";
    hasValue: boolean;
}
type EnterInstruction = {
    kind: "EnterInstruction";
}
type LeaveInstruction = {
    kind: "LeaveInstruction";
    level: number;
}
type PopInstruction = {
    kind: "PopInstruction";
}
type SimpleAssignmentInstruction = {
    kind: "SimpleAssignmentInstruction";
    op: AssignmentOperator;
}
type MemberAssignmentInstruction = {
    kind: "MemberAssignmentInstruction";
    op: AssignmentOperator;
}
type ConditionalInstruction = {
    kind: "ConditionalInstruction";
    op: ConditionalOperator;
}
type CallInstruction = {
    kind: "CallInstruction";
}
type MemberCallInstruction = {
    kind: "MemberCallInstruction";
}
type ObjectMemberInstruction = {
    kind: "ObjectMemberInstruction";
}
type ArrayLiteralInstruction = {
    kind: "ArrayLiteralInstruction";
    count: number;
}
type ObjectLiteralInstruction = {
    kind: "ObjectLiteralInstruction";
    count: number;
}
type StringLiteralInstruction = {
    kind: "StringLiteralInstruction";
    value: string;
}
type FunctionInstruction = {
    kind: "FunctionInstruction";
    instructions: number;
}
type NumericLiteralInstruction = {
    kind: "NumericLiteralInstruction";
    value: number;
}
type NullLiteralInstruction = {
    kind: "NullLiteralInstruction";
}
type BooleanLiteralInstruction = {
    kind: "BooleanLiteralInstruction";
    value: boolean;
}
type IdentifierLiteralInstruction = {
    kind: "IdentifierLiteralInstruction";
    value: string;
}
type LoadInstruction = {
    kind: "LoadInstruction";
}

type CompilerContext = {
    instructions: Instruction[];
    breakTarget: { level: number, target: LabelInstruction }[];
    continueTarget: { level: number, target: LabelInstruction }[];
    blockLevel: number;
}

class Compiler {
    instructionBlocks: Instruction[][];
    contexts: CompilerContext[];

    get instructions() { return this.contexts.peek().instructions; }
    get breakTarget() { return this.contexts.peek().breakTarget; }
    get continueTarget() { return this.contexts.peek().continueTarget; }
    get blockLevel() { return this.contexts.peek().blockLevel; }
    set blockLevel(value) { this.contexts.peek().blockLevel = value; }

    constructor() {
        this.instructionBlocks = [];
        this.contexts = [];
        this.pushContext();
    }
    pushContext() {
        const context: CompilerContext = { instructions: [], breakTarget: [], continueTarget: [], blockLevel: 0 };

        this.contexts.push(context);
        this.instructionBlocks.push(context.instructions);
    }
    popContext() {
        this.contexts.pop();
    }
    accept(ast: any, ...args: any[]): void {
        const callName = "on" + ast.__proto__.constructor.name;
        const method = (this as any)[callName];
        if (method instanceof Function) {
            method.call(this, ast, ...args);
        } else {
            throw new Error(`${callName} is not found`);
        }
    }
    onProgram(self: Program): void {
        self.statements.forEach((x) => this.accept(x));
    }
    onEmptyStatement(self: EmptyStatement): void { }
    onIfStatement(self: IfStatement): void {
        const thenLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const elseLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const joinLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };

        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        thenLabel.pc = this.instructions.length;
        this.instructions.push(thenLabel);
        if (self.thenStmt) {
            this.accept(self.thenStmt);
        }
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        elseLabel.pc = this.instructions.length;
        this.instructions.push(elseLabel);
        if (self.elseStmt) {
            this.accept(self.elseStmt);
        }
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        joinLabel.pc = this.instructions.length;
        this.instructions.push(joinLabel);
    }
    onDoStatement(self: DoStatement): void {
        const headLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const continueLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const breakLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        this.breakTarget.push({ level: this.blockLevel, target: breakLabel });
        this.continueTarget.push({ level: this.blockLevel, target: continueLabel });

        headLabel.pc = this.instructions.length;
        this.instructions.push(headLabel);
        this.accept(self.bodyStmt);

        continueLabel.pc = this.instructions.length;
        this.instructions.push(continueLabel);
        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: headLabel, elseLabel: breakLabel });

        breakLabel.pc = this.instructions.length;
        this.instructions.push(breakLabel);
    }
    onWhileStatement(self: WhileStatement): void {
        const headLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const continueLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const breakLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        this.breakTarget.push({ level: this.blockLevel, target: breakLabel });
        this.continueTarget.push({ level: this.blockLevel, target: continueLabel });

        continueLabel.pc = this.instructions.length;
        this.instructions.push(continueLabel);
        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: headLabel, elseLabel: breakLabel });

        headLabel.pc = this.instructions.length;
        this.instructions.push(headLabel);
        this.accept(self.bodyStmt);
        this.instructions.push({ kind: "JumpInstruction", label: continueLabel });

        breakLabel.pc = this.instructions.length;
        this.instructions.push(breakLabel);
    }
    onForStatement(self: ForStatement): void {
        const headLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const continueLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const breakLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const bodyLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        this.breakTarget.push({ level: this.blockLevel, target: breakLabel });
        this.continueTarget.push({ level: this.blockLevel, target: continueLabel });

        if (self.initExpr) {
            this.accept(self.initExpr);
        }

        headLabel.pc = this.instructions.length;
        this.instructions.push(headLabel);
        if (self.condExpr) {
            this.accept(self.condExpr);
            this.instructions.push({ kind: "BranchInstruction", thenLabel: bodyLabel, elseLabel: breakLabel });
        }

        bodyLabel.pc = this.instructions.length;
        this.instructions.push(bodyLabel);
        if (self.bodyStmt) {
            this.accept(self.bodyStmt);
        }

        continueLabel.pc = this.instructions.length;
        this.instructions.push(continueLabel);
        if (self.updateExpr) {
            this.accept(self.updateExpr);
            this.instructions.push({ kind: "PopInstruction" });
        }
        this.instructions.push({ kind: "JumpInstruction", label: headLabel });

        breakLabel.pc = this.instructions.length;
        this.instructions.push(breakLabel);
    }
    onLexicalDeclaration(self: LexicalDeclaration): void {
        self.binds.map(x => this.accept(x));
    }
    onLexicalBinding(self: LexicalBinding): void {
        this.accept(self.initExpr);
        this.instructions.push({ kind: "BindInstruction", ident: self.ident.value });
    }
    onSwitchStatement(self: SwitchStatement): void {

        this.accept(self.expr);

        const breakLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        this.breakTarget.push({ level: this.blockLevel, target: breakLabel });
        let defaultLabel = breakLabel;

        let labels: [LabelInstruction, IStatement][] = [];

        const elseLabel = self.clauses.reduce<LabelInstruction>((s, x) => {
            s.pc = this.instructions.length;
            this.instructions.push(s);
            const matchLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
            labels.push([matchLabel, x.stmt]);
            return x.clauses.reduce<LabelInstruction>((s, y) => {
                if (y instanceof CaseClause) {
                    s.pc = this.instructions.length;
                    this.instructions.push(s);
                    s = <LabelInstruction>{ kind: "LabelInstruction", pc: 0 };
                    this.accept(y.expr);
                    this.instructions.push({ kind: "BranchInstruction", thenLabel: matchLabel, elseLabel: s });
                } else {
                    defaultLabel = matchLabel;
                }
                return s;
            }, s);
        }, <LabelInstruction>{ kind: "LabelInstruction", pc: 0 });

        elseLabel.pc = this.instructions.length;
        this.instructions.push(elseLabel);
        this.instructions.push({ kind: "JumpInstruction", label: defaultLabel });

        labels.forEach(([matchLabel, stmt]) => {
            matchLabel.pc = this.instructions.length;
            this.instructions.push(matchLabel);
            this.accept(stmt);
        });
        breakLabel.pc = this.instructions.length;
        this.instructions.push(breakLabel);
    }
    onBreakStatement(self: BreakStatement): void {
        const info = this.breakTarget.peek();
        if (this.blockLevel > info.level) {
            this.instructions.push({ kind: "LeaveInstruction", level: this.blockLevel - info.level });
        }
        this.instructions.push({ kind: "JumpInstruction", label: info.target });
    }
    onContinueStatement(self: ContinueStatement): void {
        const info = this.continueTarget.peek();
        if (this.blockLevel > info.level) {
            this.instructions.push({ kind: "LeaveInstruction", level: this.blockLevel - info.level });
        }
        this.instructions.push({ kind: "JumpInstruction", label: info.target });
    }
    onReturnStatement(self: ReturnStatement): void {
        if (self.expr) {
            this.accept(self.expr);
            this.instructions.push({ kind: "ReturnInstruction", hasValue: true });
        } else {
            this.instructions.push({ kind: "ReturnInstruction", hasValue: false });
        }
    }
    onBlockStatement(self: BlockStatement): void {
        this.instructions.push({ kind: "EnterInstruction" });
        this.blockLevel += 1;
        for (const statement of self.statements) {
            this.accept(statement);
        }
        this.blockLevel -= 1;
        this.instructions.push({ kind: "LeaveInstruction", level: 1 });
    }
    onExpressionStatement(self: ExpressionStatement): void {
        this.accept(self.expr);
        this.instructions.push({ kind: "PopInstruction" });
    }
    onCommaExpression(self: CommaExpression): void {
        self.expressions.forEach((expr, i) => {
            if (i !== 0) {
                this.instructions.push({ kind: "PopInstruction" });
            }
            this.accept(expr);
        });
    }
    onAssignmentExpression(self: AssignmentExpression): void {
        this.accept(self.rhs);
        if (self.lhs instanceof IdentifierLiteral) {
            this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.lhs.value });
            this.instructions.push({ kind: "SimpleAssignmentInstruction", op: self.op });
        } else if (self.lhs instanceof ArrayIndexExpression) {
            this.accept(self.lhs.index);
            this.instructions.push({ kind: "NumericLiteralInstruction", value: 2 });
            this.accept(self.lhs.lhs);
            this.instructions.push({ kind: "IdentifierLiteralInstruction", value: "[]=" });
            this.instructions.push({ kind: "MemberCallInstruction" });
        } else if (self.lhs instanceof ObjectMemberExpression) {
            this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.lhs.member.value });
            this.accept(self.lhs.lhs);
            this.instructions.push({ kind: "MemberAssignmentInstruction", op: self.op });
        } else {
            throw new Error();
        }
    }
    onConditionalExpression(self: ConditionalExpression): void {
        const thenLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const elseLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const joinLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };

        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        thenLabel.pc = this.instructions.length;
        this.instructions.push(thenLabel);
        this.accept(self.thenExpr);
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        elseLabel.pc = this.instructions.length;
        this.instructions.push(elseLabel);
        this.accept(self.elseExpr);
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        joinLabel.pc = this.instructions.length;
        this.instructions.push(joinLabel);
    }
    onLogicalOrExpression(self: LogicalOrExpression): void {
        const thenLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const elseLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const joinLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };

        this.accept(self.lhs);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        thenLabel.pc = this.instructions.length;
        this.instructions.push(thenLabel);
        this.instructions.push({ kind: "BooleanLiteralInstruction", value: true });
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        elseLabel.pc = this.instructions.length;
        this.instructions.push(elseLabel);
        this.accept(self.rhs);
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        joinLabel.pc = this.instructions.length;
        this.instructions.push(joinLabel);
    }
    onLogicalAndExpression(self: LogicalAndExpression): void {
        const thenLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const elseLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };
        const joinLabel: LabelInstruction = { kind: "LabelInstruction", pc: 0 };

        this.accept(self.lhs);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        thenLabel.pc = this.instructions.length;
        this.instructions.push(thenLabel);
        this.accept(self.rhs);
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        elseLabel.pc = this.instructions.length;
        this.instructions.push(elseLabel);
        this.instructions.push({ kind: "BooleanLiteralInstruction", value: false });
        this.instructions.push({ kind: "JumpInstruction", label: joinLabel });

        joinLabel.pc = this.instructions.length;
        this.instructions.push(joinLabel);
    }
    onBitwiseOrExpression(self: BitwiseOrExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: "|" });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onBitwiseXorExpression(self: BitwiseXorExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: "^" });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onBitwiseAndExpression(self: BitwiseAndExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: "&" });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onEqualityExpression(self: EqualityExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "ConditionalInstruction", op: self.op });
    }
    onRelationalExpression(self: RelationalExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "ConditionalInstruction", op: self.op });
    }
    onShiftExpression(self: ShiftExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.op });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onAdditiveExpression(self: AdditiveExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.op });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onMultiplicativeExpression(self: MultiplicativeExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.op });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onExponentiationExpression(self: ExponentiationExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: "**" });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onUnaryExpression(self: UnaryExpression): void {
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 0 });
        this.accept(self.rhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.op + "@" });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onCallExpression(self: CallExpression): void {
        self.args.forEach(x => this.accept(x));
        this.instructions.push({ kind: "NumericLiteralInstruction", value: self.args.length });
        let lhs = self.lhs;
        for (; ;) {
            if (lhs instanceof ObjectMemberExpression) {
                this.accept(lhs.lhs);
                this.instructions.push({ kind: "IdentifierLiteralInstruction", value: lhs.member.value });
                this.instructions.push({ kind: "MemberCallInstruction" });
                break;
            } else if (lhs instanceof EnclosedInParenthesesExpression) {
                lhs = lhs.expr;
            } else {
                this.accept(lhs);
                this.instructions.push({ kind: "CallInstruction" });
                break;
            }
        }
    }
    onArrayIndexExpression(self: ArrayIndexExpression): void {
        this.accept(self.index);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: 1 });
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: "[]" });
        this.instructions.push({ kind: "MemberCallInstruction" });
    }
    onObjectMemberExpression(self: ObjectMemberExpression): void {
        this.accept(self.lhs);
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.member.value });
        this.instructions.push({ kind: "ObjectMemberInstruction" });
    }
    onEnclosedInParenthesesExpression(self: EnclosedInParenthesesExpression): void {
        this.accept(self.expr);
    }
    onArrayLiteral(self: ArrayLiteral): void {
        self.values.forEach(expr => {
            this.accept(expr);
        });
        this.instructions.push({ kind: "ArrayLiteralInstruction", count: self.values.length });
    }
    onObjectLiteral(self: ObjectLiteral): void {
        self.values.forEach(expr => {
            this.accept(expr.value);
            this.instructions.push({ kind: "IdentifierLiteralInstruction", value: expr.name.value });
        });
        this.instructions.push({ kind: "ObjectLiteralInstruction", count: self.values.length });
    }
    onFunctionExpression(self: FunctionExpression): void {
        const index = this.instructionBlocks.length;
        this.pushContext();
        this.instructions.push({ kind: "BindArgInstruction", ident: "this", spread: false });
        self.params.forEach((param, i) => {
            this.instructions.push({ kind: "BindArgInstruction", ident: self.params[i].ident.value, spread: self.params[i].spread });
        });
        this.accept(self.body);
        this.popContext();
        this.instructions.push({ kind: "FunctionInstruction", instructions: index });
    }
    onNullLiteral(self: NullLiteral): void {
        this.instructions.push({ kind: "NullLiteralInstruction" });
    }
    onBooleanLiteral(self: BooleanLiteral): void {
        this.instructions.push({ kind: "BooleanLiteralInstruction", value: self.value });
    }
    onNumericLiteral(self: NumericLiteral): void {
        this.instructions.push({ kind: "NumericLiteralInstruction", value: self.value });
    }
    onStringLiteral(self: StringLiteral): void {
        this.instructions.push({ kind: "StringLiteralInstruction", value: self.value });
    }
    onIdentifierLiteral(self: StringLiteral): void {
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.value });
        this.instructions.push({ kind: "LoadInstruction" });
    }
}

class Dictionary {
    items: { key: Value, value: Value }[] = [];

    constructor(src?: Dictionary) {
        if (src) {
            for (const item of src.items) {
                this.items.push({ key: item.key, value: item.value });
            }
        }
    }
    keys() {
        return this.items.map(x => x.key);
    }
    values() {
        return this.items.map(x => x.value);
    }
    has(key: Value) {
        const item = this.items.find(x => equalValue(x.key, key));
        return (item != null);
    }
    get(key: Value) {
        const item = this.items.find(x => equalValue(x.key, key));
        if (item != null) {
            return item.value;
        } else {
            return null;
        }
    }
    set(key: Value, value: Value) {
        const item = this.items.find(x => equalValue(x.key, key));
        if (item != null) {
            item.value = value;
        } else {
            this.items.push({ key: key, value: value });
        }
    }
    remove(key: Value) {
        const index = this.items.findIndex(x => equalValue(x.key, key));
        if (index !== -1) {
            this.items.splice(index, 1);
        }
    }
    static equal(lhs: Dictionary, rhs: Dictionary) {
        if (lhs.items.length !== rhs.items.length) {
            return false;
        }
        for (const key of lhs.keys()) {
            if (!lhs.has(key) || !rhs.has(key)) {
                return false;
            }
            const x = lhs.get(key);
            const y = rhs.get(key);
            if (equalValue(x, y) === false) {
                return false;
            }
        }
        return true;
    }
    toString() {
        return "{ " + this.items.map(x => `${toStr(x.key)}: ${toStr(x.value)}`).join(",\r\n") + "}";
    }
}

class ArrayDictionary {
    arrayItems: { key: Value, value: Value }[] = [];

    constructor(src?: ArrayDictionary) {
        if (src) {
            src.arrayItems.forEach((item, i) => {
                this.arrayItems[i] = ({ key: item.key, value: item.value });
            });
        }
    }
    length() {
        return this.arrayItems.length;
    }
    keys() {
        return this.arrayItems.map(x => x.key);
    }
    values() {
        return this.arrayItems.map(x => x.value);
    }
    push(value: Value) {
        this.arrayItems[this.arrayItems.length] = ({ key: createNumberValue(this.arrayItems.length), value: value });
    }
    pop() {
        const ret = this.arrayItems.pop();
        return ret.value;
    }
    reverse() {
        this.arrayItems.reverse();
    }
    has(key: Value) {
        if (key.kind === "Number") {
            const item = this.arrayItems.find(x => x != null && equalValue(x.key, key));
            return (item != null);
        } else {
            return false;
        }
    }
    get(key: Value) {
        if (key.kind === "Number") {
            const item = this.arrayItems.find(x => x != null && equalValue(x.key, key));
            if (item != null) {
                return item.value;
            } else {
                return null;
            }
        } else {
            return null;
        }
    }
    set(key: Value, value: Value) {
        if (key.kind === "Number") {
            this.arrayItems[toNumber(key)] = { key: key, value: value };
        }
    }
    remove(key: Value) {
        if (key.kind === "Number") {
            const item = this.arrayItems[toNumber(key)];
            if (item != null) {
                this.arrayItems.splice(toNumber(key), 1);
            }
        }
    }
    static equal(lhs: ArrayDictionary, rhs: ArrayDictionary) {
        if (lhs.arrayItems.length !== rhs.arrayItems.length) {
            return false;
        }
        for (const key of lhs.keys()) {
            if (!lhs.has(key) || !rhs.has(key)) {
                return false;
            }
            const x = lhs.get(key);
            const y = rhs.get(key);
            if (equalValue(x, y) === false) {
                return false;
            }
        }
        return true;
    }
    toString() {
        return "[ " + this.arrayItems.map(x => `${toStr(x.key)}: ${toStr(x.value)}`).join(",\r\n") + "]";
    }
}

type Scope = { kind: "Scope", value: { [key: string]: Value }, extend: Scope | null }

function createScope(parent: Scope = null): Scope {
    return { kind: "Scope", value: {}, extend: parent };
}
function findScope(self: Scope, key: string): Scope | null {
    for (let s: Scope = self; s != null; s = s.extend) {
        if (key in s.value) {
            return s;
        }
    }
    return null;
}
function getScope(self: Scope, key: string): Value | null {
    for (let s: Scope = self; s != null; s = s.extend) {
        if (key in s.value) {
            return s.value[key];
        }
    }
    return null;
}

const objectClass: ObjectValue = { kind: "Object", value: new Dictionary(), extend: null };
const nativeFuncClass: ObjectValue = createObjectValue();
const numberClass: ObjectValue = createObjectValue();
const booleanClass: ObjectValue = createObjectValue();
const stringClass: ObjectValue = createObjectValue();
const closureClass: ObjectValue = createObjectValue();
const arrayClass: ObjectValue = createObjectValue();
const nullClass: ObjectValue = createObjectValue();

type Value = NumberValue | BooleanValue | StringValue | ClosureValue | NativeFuncValue | ArrayValue | ObjectValue | NullValue;
type ObjectValue = { kind: "Object", value: Dictionary, extend: ObjectValue };
type NumberValue = { kind: "Number", value: number, extend: ObjectValue };
type BooleanValue = { kind: "Boolean", value: boolean, extend: ObjectValue };
type StringValue = { kind: "String", value: string, extend: ObjectValue };
type ClosureValue = { kind: "Closure", value: { func: number, scope: Scope }, extend: ObjectValue };
type NativeFuncValue = { kind: "Native", value: (self: Instruction, context: Context, receiver: Value, args: Value[]) => void, extend: ObjectValue };
type ArrayValue = { kind: "Array", value: ArrayDictionary, extend: ObjectValue };
type NullValue = { kind: "Null", value: null, extend: ObjectValue };

function createObjectValue(value?: Dictionary, extend?: ObjectValue): ObjectValue {
    return { kind: "Object", value: new Dictionary(value), extend: extend ? extend : objectClass };
}
function createNumberValue(value: number): NumberValue {
    return { kind: "Number", value: value, extend: numberClass };
}
function createBooleanValue(value: boolean): BooleanValue {
    return { kind: "Boolean", value: value, extend: booleanClass };
}
function createStringValue(value: string): StringValue {
    return { kind: "String", value: value, extend: stringClass };
}
function createClosureValue(func: number, scope: Scope): ClosureValue {
    return { kind: "Closure", value: { func: func, scope: scope }, extend: stringClass };
}
function createNativeFuncValue(value: (self: Instruction, context: Context, receiver: Value, args: Value[]) => void): NativeFuncValue {
    return { kind: "Native", value: value, extend: nativeFuncClass };
}
function createArrayValue(value: ArrayDictionary): ArrayValue {
    return { kind: "Array", value: new ArrayDictionary(value), extend: arrayClass };
}
function createNullValue(): NullValue {
    return { kind: "Null", value: null, extend: nullClass };
}

function getField(name: Value, value: Value) {
    if (value.extend === objectClass) {
        const objValue = <ObjectValue>value;
        if (objValue.value.has(name)) {
            return objValue.value.get(name);
        }
    }
    for (let extend = value.extend; extend != null; extend = extend.extend) {
        if (extend.value.has(name)) {
            return extend.value.get(name);
        }
    }
    return null;
}

function equalValue(lhs: Value, rhs: Value) {
    switch (lhs.kind) {
        case "Number": return (lhs.kind === rhs.kind) && lhs.value === rhs.value;
        case "Boolean": return (lhs.kind === rhs.kind) && lhs.value === rhs.value;
        case "String": return (lhs.kind === rhs.kind) && lhs.value === rhs.value;
        case "Closure": return (lhs.kind === rhs.kind) && lhs.value.func === rhs.value.func;
        case "Native": return (lhs.kind === rhs.kind) && lhs.value === rhs.value;
        case "Array": return (lhs.kind === rhs.kind) && ArrayDictionary.equal(lhs.value, rhs.value);
        case "Object": return (lhs.kind === rhs.kind) && Dictionary.equal(lhs.value, rhs.value);
        case "Null": return (lhs.kind === rhs.kind);
        default: throw new Error();
    }
}

function copyValue(self: Value): Value {
    return <Value>{ kind: self.kind, value: self.value, extend: self.extend };
}

function toBoolean(self: Value) {
    switch (self.kind) {
        case "Number": return self.value !== 0;
        case "Boolean": return self.value;
        case "String": return true;
        case "Closure": return true;
        case "Native": return true;
        case "Array": return true;
        case "Object": return true;
        case "Null": return false;
        default: throw new Error();
    }
}

function toStr(self: Value) {
    switch (self.kind) {
        case "Number": return self.value.toString();
        case "Boolean": return self.value.toString();
        case "String": return `"${escapeStr(self.value.toString())}"`;
        case "Closure": return "<Closure>";
        case "Native": return "<Native>";
        case "Array": return self.value.toString();
        case "Object": return self.value.toString();
        case "Null": return "<Null>";
        default: throw new Error();
    }
}

function toNumber(self: Value) {
    switch (self.kind) {
        case "Number": return self.value;
        case "Boolean": return self.value ? 1 : 0;
        case "String":
        case "Closure":
        case "Native":
        case "Array":
        case "Object":
        case "Null":
        default: throw new Error();
    }
}
function applyUnaryOperator(op: string, rhs: Value): Value {
    switch (op) {
        case "+@": return createNumberValue(toNumber(rhs));
        case "-@": return createNumberValue(-toNumber(rhs));
        case "~@": return createNumberValue(~toNumber(rhs));
        case "!@": return createBooleanValue(!toBoolean(rhs));
        default:
            throw new Error();
    }
}
function applyBinaryOperator(op: BinaryOperator, lhs: Value, rhs: Value): Value {
    switch (op) {
        case "+": return createNumberValue(toNumber(lhs) + toNumber(rhs));
        case "-": return createNumberValue(toNumber(lhs) - toNumber(rhs));
        case "*": return createNumberValue(toNumber(lhs) * toNumber(rhs));
        case "/": return createNumberValue(toNumber(lhs) / toNumber(rhs));
        case "%": return createNumberValue(toNumber(lhs) % toNumber(rhs));
        case "<<": return createNumberValue(toNumber(lhs) << toNumber(rhs));
        case "<<<": return createNumberValue(toNumber(lhs) << toNumber(rhs));
        case ">>": return createNumberValue(toNumber(lhs) >> toNumber(rhs));
        case ">>>": return createNumberValue(toNumber(lhs) >>> toNumber(rhs));
        case "&": return createNumberValue(toNumber(lhs) & toNumber(rhs));
        case "^": return createNumberValue(toNumber(lhs) ^ toNumber(rhs));
        case "|": return createNumberValue(toNumber(lhs) | toNumber(rhs));
        case "**": return createNumberValue(toNumber(lhs) ** toNumber(rhs));
        default: throw new Error();
    }
}
function applyConditionalOperator(op: ConditionalOperator, lhs: Value, rhs: Value): BooleanValue {
    switch (op) {
        case "==": return createBooleanValue((lhs.kind === rhs.kind) && (lhs.value === rhs.value));
        case "!=": return createBooleanValue(!((lhs.kind === rhs.kind) && (lhs.value === rhs.value)));
        case ">=": return createBooleanValue(toNumber(lhs) >= toNumber(rhs));
        case "<=": return createBooleanValue(toNumber(lhs) <= toNumber(rhs));
        case ">": return createBooleanValue(toNumber(lhs) > toNumber(rhs));
        case "<": return createBooleanValue(toNumber(lhs) < toNumber(rhs));
        default: throw new Error();
    }
}

objectClass.value.set(createStringValue("to_s"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    context.stack.push(createStringValue("to_s"));
}));
objectClass.value.set(createStringValue("[]="), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (args.length !== 2) {
        throw new Error();
    }
    if (receiver.kind !== "Object") {
        throw new Error();
    }
    const key = args[1];
    const value = args[0];
    receiver.value.set(key, value);
    context.stack.push(value);
}));
objectClass.value.set(createStringValue("[]"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (args.length !== 1) {
        throw new Error();
    }
    const key = args[0];
    context.stack.push(getField(key, receiver) || createNullValue());
}));

numberClass.value.set(createStringValue("abs"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    context.stack.push(createNumberValue(Math.abs(toNumber(receiver))));
}));
(<BinaryOperator[]>["*", "/", "%", "+", "-", "<<", ">>", ">>>", "<<<", "&", "^", "|", "**"]).forEach(v => {
    numberClass.value.set(createStringValue(v), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
        if (args.length !== 1) {
            throw new Error();
        }
        context.stack.push(applyBinaryOperator(v, receiver, args[0]));
    }));
});
["+@", "-@", "~@", "!@"].forEach(v => {
    numberClass.value.set(createStringValue(v), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
        if (args.length !== 0) {
            throw new Error();
        }
        context.stack.push(applyUnaryOperator(v, receiver));
    }));
});

stringClass.value.set(createStringValue("length"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (receiver.kind !== "String") {
        throw new Error();
    }
    context.stack.push(createNumberValue(receiver.value.length));
}));
stringClass.value.set(createStringValue("+"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (receiver.kind !== "String") {
        throw new Error();
    }
    if (args.length !== 1) {
        throw new Error();
    }
    context.stack.push(createStringValue(receiver.value + toStr(args[0])));
}));
arrayClass.value.set(createStringValue("length"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (receiver.kind !== "Array") {
        throw new Error();
    }
    if (args.length !== 0) {
        throw new Error();
    }
    context.stack.push(createNumberValue(receiver.value.length()));
}));
arrayClass.value.set(createStringValue("[]="), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (args.length !== 2) {
        throw new Error();
    }
    if (receiver.kind !== "Array") {
        throw new Error();
    }
    const key = args[1];
    const value = args[0];
    if (key.kind !== "Number") {
        throw new Error();
    }
    receiver.value.set(key, value);
    context.stack.push(value);
}));
arrayClass.value.set(createStringValue("[]"), createNativeFuncValue((self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
    if (args.length !== 1) {
        throw new Error();
    }
    if (receiver.kind !== "Array") {
        throw new Error();
    }
    const key = args[0];
    if (key.kind === "Number") {
        context.stack.push(receiver.value.get(key) || createNullValue());
    } else {
        context.stack.push(getField(key, receiver) || createNullValue());
    }
}));
class Context {
    instructionBlocks: Instruction[][];
    instructions: number;
    pc: number;
    stack: Value[];
    scope: Scope;
    callStack: Context[];
    constructor(context?: Context) {
        if (context) {
            this.instructionBlocks = context.instructionBlocks;
            this.instructions = context.instructions;
            this.pc = context.pc;
            this.stack = context.stack;
            this.scope = context.scope;
            this.callStack = context.callStack;
        } else {
            this.instructionBlocks = null;
            this.instructions = 0;
            this.pc = null;
            this.stack = null;
            this.scope = null;
            this.callStack = null;
        }
    }
}

class Vm {
    static accept(context: Context): Context {
        const block = context.instructionBlocks[context.instructions];
        if (block == null) {
            return context;
        }
        const inst = block[context.pc];
        if (inst == null) {
            return context;
        }
        const callName = "on" + inst.kind;
        const method = (Vm as any)[callName];
        if (method instanceof Function) {
            return method.call(null, inst, context);
        } else {
            throw new Error(`${callName} is not found`);
        }
    }

    static onLabelInstruction(self: LabelInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.pc += 1;
        return ctx;
    }
    static onJumpInstruction(self: JumpInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.pc = self.label.pc + 1;
        return ctx;
    }
    static onBranchInstruction(self: BranchInstruction, context: Context): Context {
        const ctx = new Context(context);
        if (toBoolean(context.stack.pop())) {
            ctx.pc = self.thenLabel.pc + 1;
        } else {
            ctx.pc = self.elseLabel.pc + 1;
        }
        return ctx;
    }
    static onBindInstruction(self: BindInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.scope.value[self.ident] = context.stack.pop();
        ctx.pc += 1;
        return ctx;
    }
    static onBindArgInstruction(self: BindArgInstruction, context: Context): Context {
        const ctx = new Context(context);
        if (self.spread) {
            const ad: ArrayDictionary = context.stack.slice().reverse().reduce((s, v, i) => { s.set(createNumberValue(i), v); return s; }, new ArrayDictionary());
            const spread: ArrayValue = createArrayValue(ad);
            ctx.scope.value[self.ident] = spread;
            context.stack.length = 0;
        } else {
            ctx.scope.value[self.ident] = context.stack.pop();
        }
        ctx.pc += 1;
        return ctx;
    }
    static onReturnInstruction(self: ReturnInstruction, context: Context): Context {
        const ctx = context.callStack.pop();
        ctx.stack.push(context.stack.pop());
        ctx.pc += 1;
        return ctx;
    }
    static onEnterInstruction(self: EnterInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.scope = createScope(ctx.scope);
        ctx.pc += 1;
        return ctx;
    }
    static onLeaveInstruction(self: LeaveInstruction, context: Context): Context {
        const ctx = new Context(context);
        for (let i = 0; i < self.level; i++) {
            ctx.scope = ctx.scope.extend;
        }
        ctx.pc += 1;
        return ctx;
    }
    static onPopInstruction(self: PopInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.pop();
        ctx.pc += 1;
        return ctx;
    }
    static onSimpleAssignmentInstruction(self: SimpleAssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const symbol = ctx.stack.pop();
        if (symbol.kind !== "String") {
            throw new Error();
        }
        const scope = findScope(ctx.scope, symbol.value);
        if (scope == null) {
            throw new Error();
        }
        const lhs = scope.value[symbol.value];
        const rhs = ctx.stack.pop();
        if (self.op === "=") {
            scope.value[symbol.value] = copyValue(rhs);
        } else {
            scope.value[symbol.value] = applyBinaryOperator(assignmentOperatorToBinaryOperator(self.op), lhs, rhs);
        }
        ctx.stack.push(scope.value[symbol.value]);
        ctx.pc += 1;
        return ctx;
    }
    static onMemberAssignmentInstruction(self: MemberAssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const receiver = ctx.stack.pop();
        const symbol = ctx.stack.pop();
        if (receiver.kind !== "Object") {
            throw new Error();
        }
        const lhs = getField(symbol, receiver);
        const rhs = ctx.stack.pop();
        const value = (self.op === "=") ? copyValue(rhs) : applyBinaryOperator(assignmentOperatorToBinaryOperator(self.op), lhs, rhs);
        receiver.value.set(symbol, value);
        ctx.stack.push(value);
        ctx.pc += 1;
        return ctx;
    }
    static onConditionalInstruction(self: ConditionalInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        const ret = applyConditionalOperator(self.op, lhs, rhs);
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onCallInstruction(self: CallInstruction, context: Context): Context {
        const ctx = new Context(context);
        const receiver: NullValue = createNullValue();
        const lhs = ctx.stack.pop();

        const argc = ctx.stack.pop();
        if (argc.kind !== "Number") {
            throw new Error();
        }

        const args: Value[] = [];
        for (let i = 0; i < argc.value; i++) {
            args.push(ctx.stack.pop());
        }

        if (lhs.kind === "Closure") {
            args.push(receiver);

            const closure = lhs.value;
            ctx.instructions = closure.func;
            ctx.pc = 0;
            ctx.callStack.push(context);
            ctx.stack = args;
            ctx.scope = createScope(closure.scope);
            return ctx;
        } else if (lhs.kind === "Native") {
            ctx.pc += 1;
            lhs.value(self, ctx, receiver, args);
            return ctx;
        } else {
            throw new Error();
        }
    }
    static onMemberCallInstruction(self: MemberCallInstruction, context: Context): Context {
        const ctx = new Context(context);
        const rhs = ctx.stack.pop();
        const receiver = ctx.stack.pop();
        const lhs = getField(rhs, receiver);

        const argc = ctx.stack.pop();
        if (argc.kind !== "Number") {
            throw new Error();
        }

        const args: Value[] = [];
        for (let i = 0; i < argc.value; i++) {
            args.push(ctx.stack.pop());
        }
        args.push(receiver);

        if (lhs.kind === "Closure") {
            const closure = lhs.value;
            ctx.instructions = closure.func;
            ctx.pc = 0;
            ctx.callStack.push(context);
            ctx.stack = args;
            ctx.scope = createScope(closure.scope);
            return ctx;
        } else if (lhs.kind === "Native") {
            ctx.pc += 1;
            lhs.value(self, ctx, receiver, args);
            return ctx;
        } else {
            throw new Error();
        }
    }
    static onObjectMemberInstruction(self: ObjectMemberInstruction, context: Context): Context {
        const ctx = new Context(context);
        const rhs = ctx.stack.pop();
        const receiver = ctx.stack.pop();
        const ret = getField(rhs, receiver) || createNullValue();
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onArrayLiteralInstruction(self: ArrayLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const values: ArrayDictionary = new ArrayDictionary();
        for (let i = 0; i < self.count; i++) {
            values.push(ctx.stack.pop());
        }
        values.reverse();
        ctx.stack.push(createArrayValue(values));
        ctx.pc += 1;
        return ctx;
    }
    static onObjectLiteralInstruction(self: ObjectLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const values = new Dictionary();
        for (let i = 0; i < self.count; i++) {
            const key = ctx.stack.pop();
            const value = ctx.stack.pop();
            if (key.kind !== "String") {
                throw new Error();
            }
            values.set(key, value);
        }
        ctx.stack.push(createObjectValue(values));
        ctx.pc += 1;
        return ctx;
    }
    static onStringLiteralInstruction(self: StringLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(createStringValue(self.value));
        ctx.pc += 1;
        return ctx;
    }
    static onFunctionInstruction(self: FunctionInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(createClosureValue(self.instructions, context.scope));
        ctx.pc += 1;
        return ctx;
    }
    static onNumericLiteralInstruction(self: NumericLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(createNumberValue(self.value));
        ctx.pc += 1;
        return ctx;
    }
    static onNullLiteralInstruction(self: NullLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(createNullValue());
        ctx.pc += 1;
        return ctx;
    }
    static onBooleanLiteralInstruction(self: BooleanLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(createBooleanValue(self.value));
        ctx.pc += 1;
        return ctx;
    }
    static onIdentifierLiteralInstruction(self: IdentifierLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(createStringValue(self.value));
        ctx.pc += 1;
        return ctx;
    }
    static onLoadInstruction(self: LoadInstruction, context: Context): Context {
        const ctx = new Context(context);
        const sym = ctx.stack.pop();
        if (sym.kind !== "String") {
            throw new Error();
        }
        ctx.stack.push(getScope(context.scope, sym.value) || createNullValue());
        ctx.pc += 1;
        return ctx;
    }
}

type ToStringContext = { scopes: Scope[], values: Value[] };
function objectToString(context: ToStringContext, self: Scope | Instruction | Value): string {
    switch (self.kind) {
        // Scope
        case "Scope": {
            let id = context.scopes.indexOf(self);
            if (id === -1) {
                id = context.scopes.length;
                context.scopes.push(self);
                const values = Object.keys(self.value).map(key => `${key}: ${objectToString(context, self.value[key])}`).join(',\r\n');
                const extend = self.extend ? objectToString(context, self.extend) : null;
                return `{ kind: "${self.kind}", id: "${id}", values: {\r\n${values}\r\n},\r\n extend: {\r\n${extend}\r\n} }`;
            } else {
                return `{ kind: "${self.kind}", ref: "${id}" }`;
            }
        }
        // Instruction
        case "LabelInstruction": {
            return `{ kind: "${self.kind}", pc: "${self.pc}" }`;
        }
        case "JumpInstruction": {
            return `{ kind: "${self.kind}", label: "${self.label.pc}" }`;
        }
        case "BranchInstruction": {
            return `{ kind: "${self.kind}", thenLabel: "${self.thenLabel.pc}", elseLabel: "${self.elseLabel.pc}" }`;
        }
        case "BindInstruction": {
            return `{ kind: "${self.kind}", ident: "${self.ident}" }`;
        }
        case "BindArgInstruction": {
            return `{ kind: "${self.kind}", ident: "${self.ident}", spread: ${self.spread} }`;
        }
        case "ReturnInstruction": {
            return `{ kind: "${self.kind}", hasValue: "${self.hasValue}" }`;
        }
        case "EnterInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "LeaveInstruction": {
            return `{ kind: "${self.kind}", level: ${self.level} }`;
        }
        case "PopInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "SimpleAssignmentInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "MemberAssignmentInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "ConditionalInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "CallInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "MemberCallInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "ObjectMemberInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "ArrayLiteralInstruction": {
            return `{ kind: "${self.kind}", count: ${self.count} }`;
        }
        case "ObjectLiteralInstruction": {
            return `{ kind: "${self.kind}", count: ${self.count} }`;
        }
        case "StringLiteralInstruction": {
            return `{ kind: "${self.kind}", value: "${self.value.toString().replace(/"/g, '\\"')}" }`;
        }
        case "FunctionInstruction": {
            return `{ kind: "${self.kind}", instructions: "${self.instructions}" }`;
        }
        case "NumericLiteralInstruction": {
            return `{ kind: "${self.kind}", value: ${self.value} }`;
        }
        case "NullLiteralInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "BooleanLiteralInstruction": {
            return `{ kind: "${self.kind}", value: ${self.value} }`;
        }
        case "IdentifierLiteralInstruction": {
            return `{ kind: "${self.kind}", value: "${self.value.toString().replace(/"/g, '\\"')}" }`;
        }
        case "LoadInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        // Value
        case "Number": {
            return `{ kind: "${self.kind}", value: ${self.value} }`;
        }
        case "Boolean": {
            return `{ kind: "${self.kind}", value: ${self.value} }`;
        }
        case "String": {
            return `{ kind: "${self.kind}", value: "${self.value.toString().replace(/"/g, '\\"')}" }`;
        }
        case "Closure": {
            return `{ kind: "${self.kind}", func: ${self.value.func}, scope: ${objectToString(context, self.value.scope)} }`;
        }
        case "Native": {
            return `{ kind: "${self.kind}", value: ${self.value.toString()} }`;
        }
        case "Array": {
            if (context.values.indexOf(self) !== -1) {
                return `{ kind: "${self.kind}", value: [ ... ] }`;
            } else {
                context.values.push(self);
                return `{ kind: "${self.kind}", value: [ ${self.value.values().map(x => objectToString(context, x)).join(", ")} ] }`;
            }
        }
        case "Object": {
            if (context.values.indexOf(self) !== -1) {
                return `{ kind: "${self.kind}", value: { ... } }`;
            } else {
                context.values.push(self);
                return `{ kind: "${self.kind}", value: ${self.value.toString()}`;
            }
        }
        case "Null": {
            return `{ kind: "${self.kind}" }`;
        }
    }
    throw new Error();
}

function contextToString(self: Context) {
    let ret: string[] = [];
    //ret.push(`{`);
    //{
    //    const context: ToStringContext = { scopes: [], values: [] };
    //    ret.push(`instructionBlocks: {`);
    //    self.instructionBlocks.forEach((block, i) => {
    //        ret.push(`${i}: [`);
    //        block.forEach(inst => ret.push(objectToString(context, inst) + ","));
    //        ret.push(`],`);
    //        ret.push(`},`);
    //    });
    //}
    ret.push(`pc: ${self.pc},`);
    ret.push(`instructions: ${self.instructions},`);
    {
        const context: ToStringContext = { scopes: [], values: [] };
        ret.push(`scope: ${objectToString(context, self.scope)},`);
    }
    {
        const context: ToStringContext = { scopes: [], values: [] };
        ret.push(`stack: [`);
        self.stack.forEach(inst => ret.push(objectToString(context, inst) + ","));
        ret.push(`], `);
    }
    ret.push(`}`);
    return ret.join("\r\n");
}

declare const parser: peg.GeneratedParser<Program>;

window.onload = () => {
    const $ = document.getElementById.bind(document);

    console.log("loaded");
    let parser: peg.GeneratedParser;
    {
        const dom = document.querySelector("script[type='text/peg-js']");
        const grammar = dom.innerHTML;
        parser = <peg.GeneratedParser<Program>>peg.generate(grammar, { cache: false, optimize: "speed", output: "parser" });
    }
    let context: Context = null;
    $("new").onclick = () => {
        $("run").setAttribute("disabled", "");
        $("step").setAttribute("disabled", "");
        $("output").value = $("code").value = $("console").value = "";
    };

    $("compile").onclick = () => {
        try {
            $("run").setAttribute("disabled", "");
            $("step").setAttribute("disabled", "");
            const result = parser.parse($("code").value);
            const c = new Compiler();
            c.accept(result);

            {
                const context: ToStringContext = { scopes: [], values: [] };
                var ret: string[] = [];
                ret.push(`instructionBlocks: {`);
                c.instructionBlocks.forEach((block, i) => {
                    ret.push(`${i}: [`);
                    block.forEach(inst => ret.push(objectToString(context, inst) + ","));
                    ret.push(`],`);
                    ret.push(`},`);
                });
                $("instruction").value = ret.join("\r\n");
            }

            context = new Context();
            context.instructionBlocks = c.instructionBlocks;
            context.instructions = 0;
            context.pc = 0;
            context.scope = createScope(null);
            context.scope.value["print"] = createNativeFuncValue(
                (self: CallInstruction, context: Context, receiver: Value, args: Value[]) => {
                    if (args.length !== 1) {
                        throw new Error();
                    }
                    const value = args[0];
                    const toStringContext: ToStringContext = { scopes: [], values: [] };
                    $("console").value += objectToString(toStringContext, value) + "\r\n";
                    context.stack.push(createNullValue());
                }
            );

            context.scope.value["Number"] = numberClass;
            context.scope.value["Array"] = arrayClass;
            context.stack = [];
            context.callStack = [];

            $("run").removeAttribute("disabled");
            $("step").removeAttribute("disabled");
        } catch (e) {
            console.log(e);
            if (e instanceof parser.SyntaxError) {
                $("console").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("console").value = `${e.toString()}: ${e.message}`;
            }
        }
    };

    $("run").onclick = () => {
        try {
            for (let i = 0; i < 1000; i++) {
                context = Vm.accept(context);
            }
            $("output").value = contextToString(context);
        } catch (e) {
            console.log(e);
            if (e instanceof parser.SyntaxError) {
                $("console").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("console").value = `${e.toString()}: ${e.message}`;
            }
        }
    };

    $("step").onclick = () => {
        try {
            context = Vm.accept(context);
            $("output").value = contextToString(context);
        } catch (e) {
            console.log(e);
            if (e instanceof parser.SyntaxError) {
                $("console").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("console").value = `${e.toString()}: ${e.message}`;
            }
        }
    };
};
