/// <reference path="typings/pegjs.d.ts" />
/// <reference path="typings/generated-parser.d.ts" />

interface Array<T> {
    peek(): T;
}
Array.prototype.peek = function () {
    return this[this.length - 1];
};

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
    constructor(public condExpr: IExpression, public bodyStmt: IStatement) {
    }
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
    ArrayAssignmentInstruction |
    MemberAssignmentInstruction |
    ConditionalInstruction |
    BinaryInstruction |
    UnaryInstruction |
    CallInstruction |
    ArrayIndexInstruction |
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
type ArrayAssignmentInstruction = {
    kind: "ArrayAssignmentInstruction";
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
type BinaryInstruction = {
    kind: "BinaryInstruction";
    op: BinaryOperator;
}
type UnaryInstruction = {
    kind: "UnaryInstruction";
    op: UnaryOperator;
}
type CallInstruction = {
    kind: "CallInstruction";
}
type ArrayIndexInstruction = {
    kind: "ArrayIndexInstruction";
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
        this.continueTarget.push({ level: this.blockLevel, target: breakLabel });

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
        this.continueTarget.push({ level: this.blockLevel, target: breakLabel });

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
            this.accept(self.lhs.lhs);
            this.accept(self.lhs.index);
            this.instructions.push({ kind: "ArrayAssignmentInstruction", op: self.op });
        } else if (self.lhs instanceof ObjectMemberExpression) {
            this.accept(self.lhs.lhs);
            this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.lhs.member.value });
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
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "|" });
    }
    onBitwiseXorExpression(self: BitwiseXorExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "^" });
    }
    onBitwiseAndExpression(self: BitwiseAndExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "&" });
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
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: self.op });
    }
    onAdditiveExpression(self: AdditiveExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: self.op });
    }
    onMultiplicativeExpression(self: MultiplicativeExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: self.op });
    }
    onExponentiationExpression(self: ExponentiationExpression): void {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "**" });
    }
    onUnaryExpression(self: UnaryExpression): void {
        this.accept(self.rhs);
        this.instructions.push({ kind: "UnaryInstruction", op: self.op });
    }
    onCallExpression(self: CallExpression): void {
        self.args.forEach(x => this.accept(x));
        this.accept(self.lhs);
        this.instructions.push({ kind: "NumericLiteralInstruction", value: self.args.length });
        this.instructions.push({ kind: "CallInstruction" });
    }
    onArrayIndexExpression(self: ArrayIndexExpression): void {
        this.accept(self.index);
        this.accept(self.lhs);
        this.instructions.push({ kind: "ArrayIndexInstruction" });
    }
    onObjectMemberExpression(self: ObjectMemberExpression): void {
        this.instructions.push({ kind: "IdentifierLiteralInstruction", value: self.member.value });
        this.accept(self.lhs);
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

type Scope = {
    kind: "Scope";
    values: { [key: string]: Value };
    prev: Scope | null;
}

function findScope(self: Scope, key: string): Scope | null {
    for (let s: Scope = self; s != null; s = s.prev) {
        if (key in s.values) {
            return s;
        }
    }
    return null;
}
function getScope(self: Scope, key: string): Value | null {
    for (let s: Scope = self; s != null; s = s.prev) {
        if (key in s.values) {
            return s.values[key];
        }
    }
    return null;
}

type Value = NumberValue | BooleanValue | StringValue | SymbolValue | ClosureValue | ArrayValue | ObjectValue | NullValue;
type NumberValue  = { kind: "Number", value: number };
type BooleanValue = { kind: "Boolean", value: boolean };
type StringValue  = { kind: "String", value: string };
type SymbolValue  = { kind: "Symbol", value: string };
type ClosureValue = { kind: "Closure", value: { func: number, scope: Scope } };
type ArrayValue   = { kind: "Array", value: Value[] };
type ObjectValue  = { kind: "Object", value: { [key: string]: Value } };
type NullValue    = { kind: "Null", value: null };

function copyValue(self: Value): Value {
    return <Value>{ kind: self.kind, value: self.value };
}

function toBoolean(self: Value) {
    switch (self.kind) {
        case "Number":
            return self.value !== 0;
        case "Boolean":
            return self.value;
        case "String":
            return true;
        case "Symbol":
            return true;
        case "Closure":
            return true;
        case "Array":
            return true;
        case "Object":
            return true;
        case "Null":
            return false;
        default:
            throw new Error();
    }
}

function toNumber(self: Value) {
    switch (self.kind) {
        case "Number":
            return self.value;
        case "Boolean":
            return self.value ? 1 : 0;
        case "String":
        case "Symbol":
        case "Closure":
        case "Array":
        case "Object":
        case "Null":
        default:
            throw new Error();
    }
}

function applyBinaryOperator(op: BinaryOperator, lhs: Value, rhs: Value): Value {
    switch (op) {
        case "+":
            return { kind: "Number", value: toNumber(lhs) + toNumber(rhs) };
        case "-":
            return { kind: "Number", value: toNumber(lhs) - toNumber(rhs) };
        case "*":
            return { kind: "Number", value: toNumber(lhs) * toNumber(rhs) };
        case "/":
            return { kind: "Number", value: toNumber(lhs) / toNumber(rhs) };
        case "%":
            return { kind: "Number", value: toNumber(lhs) % toNumber(rhs) };
        case "<<":
            return { kind: "Number", value: toNumber(lhs) << toNumber(rhs) };
        case "<<<":
            return { kind: "Number", value: toNumber(lhs) << toNumber(rhs) };
        case ">>":
            return { kind: "Number", value: toNumber(lhs) >> toNumber(rhs) };
        case ">>>":
            return { kind: "Number", value: toNumber(lhs) >>> toNumber(rhs) };
        case "&":
            return { kind: "Number", value: toNumber(lhs) & toNumber(rhs) };
        case "^":
            return { kind: "Number", value: toNumber(lhs) ^ toNumber(rhs) };
        case "|":
            return { kind: "Number", value: toNumber(lhs) | toNumber(rhs) };
        case "**":
            return { kind: "Number", value: toNumber(lhs) ** toNumber(rhs) };
        default:
            throw new Error();
    }
}
function applyConditionalOperator(op: ConditionalOperator, lhs: Value, rhs: Value): BooleanValue {
    switch (op) {
        case "==":
            return { kind: "Boolean", value: (lhs.kind === rhs.kind) && (lhs.value === rhs.value) };
        case "!=":
            return { kind: "Boolean", value: !((lhs.kind === rhs.kind) && (lhs.value === rhs.value)) };
        case ">=":
            return { kind: "Boolean", value: toNumber(lhs) >= toNumber(rhs) };
        case "<=":
            return { kind: "Boolean", value: toNumber(lhs) <= toNumber(rhs) };
        case ">":
            return { kind: "Boolean", value: toNumber(lhs) > toNumber(rhs) };
        case "<":
            return { kind: "Boolean", value: toNumber(lhs) < toNumber(rhs) };
        default:
            throw new Error();
    }
}

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
        //ctx.pc = context.instructionBlocks[context.instructions].findIndex(x => x == self.label) + 1;
        ctx.pc = self.label.pc + 1;
        return ctx;
    }
    static onBranchInstruction(self: BranchInstruction, context: Context): Context {
        const ctx = new Context(context);
        if (toBoolean(context.stack.pop())) {
            //ctx.pc = context.instructionBlocks[context.instructions].findIndex(x => x == self.thenLabel) + 1;
            ctx.pc = self.thenLabel.pc + 1;
        } else {
            //ctx.pc = context.instructionBlocks[context.instructions].findIndex(x => x == self.elseLabel) + 1;
            ctx.pc = self.elseLabel.pc + 1;
        }
        return ctx;
    }
    static onBindInstruction(self: BindInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.scope.values[self.ident] = context.stack.pop();
        ctx.pc += 1;
        return ctx;
    }
    static onBindArgInstruction(self: BindArgInstruction, context: Context): Context {
        const ctx = new Context(context);
        if (self.spread) {
            const spread: ArrayValue = { kind: "Array", value: context.stack.slice().reverse() };
            ctx.scope.values[self.ident] = spread;
            context.stack.length = 0;
        } else {
            ctx.scope.values[self.ident] = context.stack.pop();
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
        ctx.scope = { kind: "Scope", values: {}, prev: ctx.scope };
        ctx.pc += 1;
        return ctx;
    }
    static onLeaveInstruction(self: LeaveInstruction, context: Context): Context {
        const ctx = new Context(context);
        for (let i = 0; i < self.level; i++) {
            ctx.scope = ctx.scope.prev;
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
        if (symbol.kind !== "Symbol") {
            throw new Error();
        }
        const scope = findScope(ctx.scope, symbol.value);
        if (scope == null) {
            throw new Error();
        }
        const lhs = scope.values[symbol.value];
        const rhs = ctx.stack.pop();
        if (self.op === "=") {
            scope.values[symbol.value] = copyValue(rhs);
        } else {
            scope.values[symbol.value] = applyBinaryOperator(assignmentOperatorToBinaryOperator(self.op), lhs, rhs);
        }
        ctx.stack.push(scope.values[symbol.value]);
        ctx.pc += 1;
        return ctx;
    }
    static onArrayAssignmentInstruction(self: ArrayAssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const index = ctx.stack.pop();
        if (index.kind !== "Number") {
            throw new Error();
        }
        const array = ctx.stack.pop();
        if (array.kind !== "Array") {
            throw new Error();
        }
        const lhs = array.value[index.value];
        const rhs = ctx.stack.pop();
        if (self.op === "=") {
            array.value[index.value] = copyValue(rhs);
        } else {
            array.value[index.value] = applyBinaryOperator(assignmentOperatorToBinaryOperator(self.op), lhs, rhs);
        }
        ctx.stack.push(array.value[index.value]);
        ctx.pc += 1;
        return ctx;
    }
    static onMemberAssignmentInstruction(self: MemberAssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const symbol = ctx.stack.pop();
        const object = ctx.stack.pop();
        if (symbol.kind !== "Symbol" || object.kind !== "Object") {
            throw new Error();
        }
        const lhs = object.value[symbol.value];
        const rhs = ctx.stack.pop();
        if (self.op === "=") {
            object.value[symbol.value] = copyValue(rhs);
        } else {
            object.value[symbol.value] = applyBinaryOperator(assignmentOperatorToBinaryOperator(self.op), lhs, rhs);
        }
        ctx.stack.push(object.value[symbol.value]);
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
    static onBinaryInstruction(self: BinaryInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        const ret = applyBinaryOperator(self.op, lhs, rhs);
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onUnaryInstruction(self: UnaryInstruction, context: Context): Context {
        const ctx = new Context(context);
        const rhs = ctx.stack.pop();
        let ret: Value;
        switch (self.op) {
            case "+":
                ret = { kind: "Number", value: toNumber(rhs) };
                break;
            case "-":
                ret = { kind: "Number", value: -toNumber(rhs) };
                break;
            case "~":
                ret = { kind: "Number", value: ~toNumber(rhs) };
                break;
            case "!":
                ret = { kind: "Boolean", value: !toBoolean(rhs) };
                break;
            default:
                throw new Error();
        }
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onCallInstruction(self: CallInstruction, context: Context): Context {
        const ctx = new Context(context);
        const argc = ctx.stack.pop();
        const lhs = ctx.stack.pop();
        if (argc.kind !== "Number" || lhs.kind !== "Closure") {
            throw new Error();
        }

        const args : Value[] = [];
        for (let i = 0; i < argc.value; i++) {
            args.push(ctx.stack.pop());
        }


        const closure = lhs.value;
        ctx.instructions = closure.func;
        ctx.pc = 0;
        ctx.callStack.push(context);
        ctx.stack = args;
        ctx.scope = { kind: "Scope", values: {}, prev: closure.scope };
        return ctx;
    }
    static onArrayIndexInstruction(self: ArrayIndexInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        if (lhs.kind !== "Array" || rhs.kind !== "Number") {
            throw new Error();
        }
        const array = lhs.value;
        const ret = array[toNumber(rhs)] || { kind: "Null", value: null };
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onObjectMemberInstruction(self: ObjectMemberInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        if (lhs.kind !== "Object" || rhs.kind !== "Symbol") {
            throw new Error();
        }
        const obj = lhs.value;
        const ret = obj[rhs.value] || { kind: "Null", value: null };
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onArrayLiteralInstruction(self: ArrayLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const values: Value[] = [];
        for (let i = 0; i < self.count; i++) {
            values.push(ctx.stack.pop());
        }
        values.reverse();
        ctx.stack.push({ kind: "Array", value: values });
        ctx.pc += 1;
        return ctx;
    }
    static onObjectLiteralInstruction(self: ObjectLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const values: { [key: string]: Value } = {};
        for (let i = 0; i < self.count; i++) {
            const key = ctx.stack.pop();
            const value = ctx.stack.pop();
            if (key.kind !== "Symbol") {
                throw new Error();
            }
            values[key.value] = value;
        }
        ctx.stack.push({ kind: "Object", value: values });
        ctx.pc += 1;
        return ctx;
    }
    static onStringLiteralInstruction(self: StringLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({ kind: "String", value: self.value });
        ctx.pc += 1;
        return ctx;
    }
    static onFunctionInstruction(self: FunctionInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({ kind: "Closure", value: { func: self.instructions, scope: context.scope } });
        ctx.pc += 1;
        return ctx;
    }
    static onNumericLiteralInstruction(self: NumericLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({ kind: "Number", value: self.value });
        ctx.pc += 1;
        return ctx;
    }
    static onNullLiteralInstruction(self: NullLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({ kind: "Null", value: null });
        ctx.pc += 1;
        return ctx;
    }
    static onBooleanLiteralInstruction(self: BooleanLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({ kind: "Boolean", value: self.value });
        ctx.pc += 1;
        return ctx;
    }
    static onIdentifierLiteralInstruction(self: IdentifierLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({ kind: "Symbol", value: self.value });
        ctx.pc += 1;
        return ctx;
    }
    static onLoadInstruction(self: LoadInstruction, context: Context): Context {
        const ctx = new Context(context);
        const sym = ctx.stack.pop();
        if (sym.kind !== "Symbol") {
            throw new Error();
        }
        ctx.stack.push(getScope(context.scope, sym.value) || { kind: "Null", value: null });
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
                const values = Object.keys(self.values).map(key => `${key}: ${objectToString(context, self.values[key])}`).join(',\r\n');
                const prev = self.prev ? objectToString(context, self.prev) : null;
                return `{ kind: "${self.kind}", id: "${id}", values: {\r\n${values}\r\n},\r\n prev: {\r\n${prev}\r\n}`;
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
        case "ArrayAssignmentInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "MemberAssignmentInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "ConditionalInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "BinaryInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "UnaryInstruction": {
            return `{ kind: "${self.kind}", op: "${self.op}" }`;
        }
        case "CallInstruction": {
            return `{ kind: "${self.kind}" }`;
        }
        case "ArrayIndexInstruction": {
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
        case "Symbol": {
            return `{ kind: "${self.kind}", value: "${self.value.toString().replace(/"/g, '\\"')}" }`;
        }
        case "Closure": {
            return `{ kind: "${self.kind}", func: ${self.value.func}, scope: ${objectToString(context, self.value.scope)} }`;
        }
        case "Array": {
            if (context.values.indexOf(self) !== -1) {
                return `{ kind: "${self.kind}", value: [ ... ] }`;
            } else {
                context.values.push(self);
                return `{ kind: "${self.kind}", value: [ ${self.value.map(x => objectToString(context, x)).join(", ")} ] }`;
            }
        }
        case "Object": {
            if (context.values.indexOf(self) !== -1) {
                return `{ kind: "${self.kind}", value: { ... } }`;
            } else {
                context.values.push(self);
                return `{ kind: "${self.kind}", value: { ${Object.keys(self.value).map(x => x + ": " + objectToString(context, self.value[x])).join(", ")} } }`;
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
    ret.push(`{`);
    {
        const context: ToStringContext = { scopes: [], values: [] };
        ret.push(`instructionBlocks: {`);
        self.instructionBlocks.forEach((block, i) => {
            ret.push(`${i}: [`);
            block.forEach(inst => ret.push(objectToString(context, inst)+","));
            ret.push(`],`);
            ret.push(`},`);
        });
    }
    ret.push(`instructions: ${self.instructions},`);
    ret.push(`pc: ${self.pc},`);
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

declare var jsDump: {
    parse(code: any): string;
};

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

    $("compile").onclick = () => {
        try {
            $("run").setAttribute("disabled", "");
            $("step").setAttribute("disabled", "");
            const result = parser.parse($("code").value);
            const c = new Compiler();
            c.accept(result);
            $("output").value = jsDump.parse(c.instructionBlocks);

            context = new Context();
            context.instructionBlocks = c.instructionBlocks;
            context.instructions = 0;
            context.pc = 0;
            context.scope = { kind: "Scope", values: {}, prev: null };
            context.stack = [];
            context.callStack = [];

            $("run").removeAttribute("disabled");
            $("step").removeAttribute("disabled");
        } catch (e) {
            console.log(e);
            if (e instanceof parser.SyntaxError) {
                $("output").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("output").value = `${e.toString()}: ${e.message}`;
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
                $("output").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("output").value = `${e.toString()}: ${e.message}`;
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
                $("output").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("output").value = `${e.toString()}: ${e.message}`;
            }
        }
    };
};
