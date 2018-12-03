/// <reference path="typings/pegjs.d.ts" />
/// <reference path="typings/generated-parser.d.ts" />

///import * as Api from "./typings/api";

interface Array<T> {
    peek(): T;
}
Array.prototype.peek = function () {
    return this[this.length - 1];
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
    constructor(public exprs: IExpression[]) { }
}

class AssignmentExpression implements IExpression {
    constructor(public op: AssignmentOperator, public lhs: IExpression, public rhs: IExpression) { }
}

class ConditionalExpression implements IExpression {
    constructor(public condExpr: IExpression, public thenExpr: IExpression, public elseExpr: IExpression) { }
}

class LogicalORExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class LogicalANDExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class BitwiseORExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class BitwiseXORExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class BitwiseANDExpression implements IExpression {
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
    constructor(public key: IdentifierLiteral, public value: IExpression) { }
}

class FunctionExpression implements IExpression {
    constructor(public params: FunctionParameter[], public body) { }
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

type IInstruction =
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
    kind: "LabelInstruction",

}
type JumpInstruction = {
    kind: "JumpInstruction",
    label: LabelInstruction
}
type BranchInstruction = {
    kind: "BranchInstruction",
    thenLabel: LabelInstruction,
    elseLabel: LabelInstruction
}
type BindInstruction = {
    kind: "BindInstruction",
    ident: string
}
type BindArgInstruction = {
    kind: "BindArgInstruction",
    ident: string,
    spread: boolean
}
type ReturnInstruction = {
    kind: "ReturnInstruction",
    hasValue: boolean
}
type EnterInstruction = {
    kind: "EnterInstruction",
}
type LeaveInstruction = {
    kind: "LeaveInstruction",
    level: number
}
type PopInstruction = {
    kind: "PopInstruction",
}
type SimpleAssignmentInstruction = {
    kind: "SimpleAssignmentInstruction",
    op: AssignmentOperator
}
type ArrayAssignmentInstruction = {
    kind: "ArrayAssignmentInstruction",
    op: AssignmentOperator
}
type MemberAssignmentInstruction = {
    kind: "MemberAssignmentInstruction",
    op: AssignmentOperator
}
type ConditionalInstruction = {
    kind: "ConditionalInstruction",
    op: ConditionalOperator
}
type BinaryInstruction = {
    kind: "BinaryInstruction",
    op: BinaryOperator
}
type UnaryInstruction = {
    kind: "UnaryInstruction",
    op: UnaryOperator
}
type CallInstruction = {
    kind: "CallInstruction",
    argc: number
}
type ArrayIndexInstruction = {
    kind: "ArrayIndexInstruction",
}
type ObjectMemberInstruction = {
    kind: "ObjectMemberInstruction",
    member: string
}
type ArrayLiteralInstruction = {
    kind: "ArrayLiteralInstruction",
    count: number
}
type ObjectLiteralInstruction = {
    kind: "ObjectLiteralInstruction",
    count: number
}
type StringLiteralInstruction = {
    kind: "StringLiteralInstruction",
    value: string
}
type FunctionInstruction = {
    kind: "FunctionInstruction",
    params: FunctionParameter[],
    instructions: number
}
type NumericLiteralInstruction = {
    kind: "NumericLiteralInstruction",
    value: number
}
type NullLiteralInstruction = {
    kind: "NullLiteralInstruction",
}
type BooleanLiteralInstruction = {
    kind: "BooleanLiteralInstruction",
    value: boolean
}
type IdentifierLiteralInstruction = {
    kind: "IdentifierLiteralInstruction",
    value: string
}
type LoadInstruction = {
    kind: "LoadInstruction",
    value: string
}

type CompilerContext = {
    instructions: IInstruction[];
    breakTarget: { level: number, target: LabelInstruction }[];
    continueTarget: { level: number, target: LabelInstruction }[];
    blockLevel: number;
}

class Compiler {
    instructionBlocks: IInstruction[][];
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
        const context = {
            instructions: [],
            breakTarget: [],
            continueTarget: [],
            blockLevel: 0
        };
        this.contexts.push(context);
        this.instructionBlocks.push(context.instructions);

    }
    popContext() {
        this.contexts.pop();
    }
    accept(ast: any, ...args: any[]): any {
        const callName = "on" + ast.__proto__.constructor.name;
        if (this[callName]) {
            return this[callName].call(this, ast, ...args);
        } else {
            throw new Error(`${callName} is not found`);
        }
    }
    onProgram(self: Program) {
        self.statements.forEach((x) => this.accept(x));
    }
    onEmptyStatement(self: EmptyStatement) { return null; }
    onIfStatement(self: IfStatement) {
        const thenLabel: LabelInstruction = { kind: "LabelInstruction" };
        const elseLabel: LabelInstruction = { kind: "LabelInstruction" };
        const joinLabel: LabelInstruction = { kind: "LabelInstruction" };

        this.accept(self.condExpr);
        this.instructions.push({kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        this.instructions.push(thenLabel);
        if (self.thenStmt) {
            this.accept(self.thenStmt);
        }
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(elseLabel);
        if (self.elseStmt) {
            this.accept(self.elseStmt);
        }
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(joinLabel);

        return null;
    }
    onDoStatement(self: DoStatement) {
        const headLabel : LabelInstruction = { kind: "LabelInstruction" };
        const contLabel : LabelInstruction = { kind: "LabelInstruction" };
        const brakLabel : LabelInstruction = { kind: "LabelInstruction" };
        this.breakTarget.push({ level: this.blockLevel, target: brakLabel });
        this.continueTarget.push({ level: this.blockLevel, target: brakLabel });

        this.instructions.push(headLabel);
        this.accept(self.bodyStmt);

        this.instructions.push(contLabel);
        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: headLabel, elseLabel: brakLabel } );

        this.instructions.push(brakLabel);

        return null;
    }
    onWhileStatement(self: WhileStatement) {
        const headLabel : LabelInstruction = { kind: "LabelInstruction" };
        const contLabel : LabelInstruction = { kind: "LabelInstruction" };
        const brakLabel : LabelInstruction = { kind: "LabelInstruction" };
        this.breakTarget.push({ level: this.blockLevel, target: brakLabel });
        this.continueTarget.push({ level: this.blockLevel, target: brakLabel });

        this.instructions.push(contLabel);
        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: headLabel, elseLabel: brakLabel } );

        this.instructions.push(headLabel);
        this.accept(self.bodyStmt);
        this.instructions.push({ kind: "JumpInstruction", label: contLabel });

        this.instructions.push(brakLabel);

        return null;
    }
    onForStatement(self: ForStatement) {
        const headLabel : LabelInstruction = { kind: "LabelInstruction" };
        const contLabel : LabelInstruction = { kind: "LabelInstruction" };
        const brakLabel : LabelInstruction = { kind: "LabelInstruction" };
        const bodyLabel : LabelInstruction = { kind: "LabelInstruction" };
        this.breakTarget.push({ level: this.blockLevel, target: brakLabel });
        this.continueTarget.push({ level: this.blockLevel, target: contLabel });

        if (self.initExpr) {
            this.accept(self.initExpr);
        }

        this.instructions.push(headLabel);
        if (self.condExpr) {
            this.accept(self.condExpr);
            this.instructions.push({ kind: "BranchInstruction", thenLabel: bodyLabel, elseLabel: brakLabel });
        }

        this.instructions.push(bodyLabel);
        if (self.bodyStmt) {
            this.accept(self.bodyStmt);
        }

        this.instructions.push(contLabel);
        if (self.updateExpr) {
            this.accept(self.updateExpr);
            this.instructions.push({ kind: "PopInstruction" });
        }
        this.instructions.push({ kind: "JumpInstruction", label: headLabel });

        this.instructions.push(brakLabel);

        return null;
    }
    onLexicalDeclaration(self: LexicalDeclaration) {
        self.binds.map(x => this.accept(x));
        return null;
    }
    onLexicalBinding(self: LexicalBinding) {
        this.accept(self.initExpr);
        this.instructions.push({ kind: "BindInstruction", ident: self.ident.value });
        return null;
    }
    onSwitchStatement(self: SwitchStatement) {

        this.accept(self.expr);

        const brakLabel : LabelInstruction = { kind: "LabelInstruction" };
        this.breakTarget.push({ level: this.blockLevel, target: brakLabel });
        let defaultLabel = brakLabel;

        let labels: [LabelInstruction, IStatement][] = [];

        const elseLabel = self.clauses.reduce<LabelInstruction>((s, x) => {
            this.instructions.push(s);
            const matchLabel : LabelInstruction = { kind: "LabelInstruction" };
            labels.push([matchLabel, x.stmt]);
            return x.clauses.reduce<LabelInstruction>((s, y) => {
                if (y instanceof CaseClause) {
                    this.instructions.push(s);
                    s = <LabelInstruction>{ kind: "LabelInstruction" };
                    this.accept(y.expr);
                    this.instructions.push({ kind: "BranchInstruction", thenLabel: matchLabel, elseLabel: s });
                } else {
                    defaultLabel = matchLabel;
                }
                return s;
            }, <LabelInstruction>s);
        }, <LabelInstruction>{ kind: "LabelInstruction" });

        this.instructions.push(elseLabel);
        this.instructions.push({ kind: "JumpInstruction", label: defaultLabel });

        labels.forEach(([matchLabel, stmt]) => {
            this.instructions.push(matchLabel);
            this.accept(stmt);
        });
        this.instructions.push(brakLabel);
        return null;
    }
    //onCaseBlock(self: CaseBlock) { return null; }
    //onCaseClause(self: CaseClause) { return null; }
    //onDefaultClause(self: DefaultClause) { return null; }
    onBreakStatement(self: BreakStatement) {
        const info = this.breakTarget.peek();
        if (this.blockLevel > info.level) {
            this.instructions.push({ kind: "LeaveInstruction", level: this.blockLevel - info.level });
        }
        this.instructions.push({ kind: "JumpInstruction", label: info.target });
        return null;
    }
    onContinueStatement(self: ContinueStatement) {
        const info = this.continueTarget.peek();
        if (this.blockLevel > info.level) {
            this.instructions.push({ kind: "LeaveInstruction", level: this.blockLevel - info.level });
        }
        this.instructions.push({ kind: "JumpInstruction", label: info.target });
        return null;
    }
    onReturnStatement(self: ReturnStatement) {
        if (self.expr) {
            this.accept(self.expr);
            this.instructions.push({ kind: "ReturnInstruction", hasValue: true });
        } else {
            this.instructions.push({ kind: "ReturnInstruction", hasValue: false });
        }
        return null;
    }
    onBlockStatement(self: BlockStatement) {
        this.instructions.push({ kind: "EnterInstruction" });
        this.blockLevel += 1;
        for (const statement of self.statements) {
            this.accept(statement);
        }
        this.blockLevel -= 1;
        this.instructions.push({ kind: "LeaveInstruction", level: 1 });
        return null;
    }
    onExpressionStatement(self: ExpressionStatement) {
        this.accept(self.expr);
        this.instructions.push({ kind: "PopInstruction" });
        return null;
    }
    onCommaExpression(self: CommaExpression) {
        self.exprs.forEach((expr, i) => {
            if (i !== 0) {
                this.instructions.push({ kind: "PopInstruction" });
            }
            this.accept(expr);
        });
        return null;
    }
    onAssignmentExpression(self: AssignmentExpression) {
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
        return null;
    }
    onConditionalExpression(self: ConditionalExpression) {
        const thenLabel : LabelInstruction = { kind: "LabelInstruction" };
        const elseLabel : LabelInstruction = { kind: "LabelInstruction" };
        const joinLabel : LabelInstruction = { kind: "LabelInstruction" };

        this.accept(self.condExpr);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        this.instructions.push(thenLabel);
        this.accept(self.thenExpr);
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(elseLabel);
        this.accept(self.elseExpr);
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(joinLabel);

        return null;
    }
    onLogicalORExpression(self: LogicalORExpression) {
        const thenLabel : LabelInstruction = { kind: "LabelInstruction" };
        const elseLabel : LabelInstruction = { kind: "LabelInstruction" };
        const joinLabel : LabelInstruction = { kind: "LabelInstruction" };

        this.accept(self.lhs);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        this.instructions.push(thenLabel);
        this.instructions.push({ kind: "BooleanLiteralInstruction", value: true });
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(elseLabel);
        this.accept(self.rhs);
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(joinLabel);

        return null;
    }
    onLogicalANDExpression(self: LogicalANDExpression) {
        const thenLabel : LabelInstruction = { kind: "LabelInstruction" };
        const elseLabel : LabelInstruction = { kind: "LabelInstruction" };
        const joinLabel : LabelInstruction = { kind: "LabelInstruction" };

        this.accept(self.lhs);
        this.instructions.push({ kind: "BranchInstruction", thenLabel: thenLabel, elseLabel: elseLabel });

        this.instructions.push(thenLabel);
        this.accept(self.rhs);
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(elseLabel);
        this.instructions.push({ kind: "BooleanLiteralInstruction", value: false });
        this.instructions.push({ kind:"JumpInstruction", label: joinLabel});

        this.instructions.push(joinLabel);

        return null;
    }
    onBitwiseORExpression(self: BitwiseORExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "|" });
        return null;
    }
    onBitwiseXORExpression(self: BitwiseXORExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "^" });
        return null;
    }
    onBitwiseANDExpression(self: BitwiseANDExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "&" });
        return null;
    }
    onEqualityExpression(self: EqualityExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "ConditionalInstruction", op: self.op });
        return null;
    }
    onRelationalExpression(self: RelationalExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "ConditionalInstruction", op: self.op });
        return null;
    }
    onShiftExpression(self: ShiftExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: self.op });
        return null;
    }
    onAdditiveExpression(self: AdditiveExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: self.op });
        return null;
    }
    onMultiplicativeExpression(self: MultiplicativeExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: self.op });
        return null;
    }
    onExponentiationExpression(self: ExponentiationExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push({ kind: "BinaryInstruction", op: "**" });
        return null;
    }
    onUnaryExpression(self: UnaryExpression) {
        this.accept(self.rhs);
        this.instructions.push({ kind: "UnaryInstruction", op: self.op });
        return null;
    }
    onCallExpression(self: CallExpression) {
        self.args.forEach(x => this.accept(x));
        this.accept(self.lhs);
        this.instructions.push({ kind: "CallInstruction", argc: self.args.length });
        return null;
    }
    onArrayIndexExpression(self: ArrayIndexExpression) {
        this.accept(self.index);
        this.accept(self.lhs);
        this.instructions.push({ kind: "ArrayIndexInstruction" });
        return null;
    }
    onObjectMemberExpression(self: ObjectMemberExpression) {
        this.accept(self.lhs);
        this.instructions.push({ kind: "ObjectMemberInstruction", member: self.member.value });
        return null;
    }
    onEnclosedInParenthesesExpression(self: EnclosedInParenthesesExpression) {
        this.accept(self.expr);
        return null;
    }
    onArrayLiteral(self: ArrayLiteral) {
        self.values.forEach(expr => {
            this.accept(expr);
        });
        this.instructions.push({ kind: "ArrayLiteralInstruction", count: self.values.length });
        return null;
    }
    onObjectLiteral(self: ObjectLiteral) {
        self.values.forEach(expr => {
            this.accept(expr.value);
            this.instructions.push({ kind: "IdentifierLiteralInstruction", value: expr.key.value });
        });
        this.instructions.push({ kind: "ObjectLiteralInstruction", count: self.values.length });
        return null;
    }
    //onPropertyDefinition(self: PropertyDefinition) { return null; }
    onFunctionExpression(self: FunctionExpression) {
        this.pushContext();
        const index = this.instructionBlocks.length;

        if (self.params.length > 0) {
            for (let i = 0; i < self.params.length; i++) {
                this.instructions.push({ kind: "BindArgInstruction",ident:self.params[i].ident.value, spread:self.params[i].spread});
            }
        }
        this.accept(self.body);
        this.popContext();
        this.instructions.push({ kind: "FunctionInstruction", params: self.params, instructions: index });
        return null;
    }
    //onFunctionParameter(self: FunctionParameter) { return null; }
    onNullLiteral(self: NullLiteral) {
        this.instructions.push({ kind: "NullLiteralInstruction" });
        return null;
    }
    onBooleanLiteral(self: BooleanLiteral) {
        this.instructions.push({ kind: "BooleanLiteralInstruction", value: self.value });
        return null;
    }
    onNumericLiteral(self: NumericLiteral) {
        this.instructions.push({ kind: "NumericLiteralInstruction", value: self.value });
        return null;
    }
    onStringLiteral(self: StringLiteral) {
        this.instructions.push({ kind: "StringLiteralInstruction", value: self.value });
        return null;
    }
    onIdentifierLiteral(self: StringLiteral) {
        this.instructions.push({ kind: "LoadInstruction", value: self.value });
        return null;
    }
}

class IScope {
    values: { [key: string]: Value };
    prev: IScope;
    constructor(prev: IScope) {
        this.values = {};
        this.prev = prev;
    }
    getScope(key: string) {
        for (let self : IScope= this; self != null; self = self.prev) {
            if (key in self.values) {
                return self;
            }
        }
        return null;
    }
    get(key: string) {
        for (let self : IScope= this; self != null; self = self.prev) {
            if (key in self.values) {
                return self.values[key];
            }
        }
        return null;
    }
}

type Value = NumberValue | BooleanValue | StringValue | SymbolValue | ClosureValue | ArrayValue | ObjectValue | NullValue;
type NumberValue  = { kind: "number", value: number };
type BooleanValue = { kind: "boolean", value: boolean };
type StringValue  = { kind: "string", value: string };
type SymbolValue  = { kind: "symbol", value: string };
type ClosureValue = { kind: "closure", value: { func: number, scope: IScope } };
type ArrayValue   = { kind: "array", value: Value[] };
type ObjectValue  = { kind: "object", value:{ [key: string]: Value } };
type NullValue    = { kind: "null", value:null};

function copyValue(self: Value) : Value {
    return <Value>{ kind: self.kind, value: self.value };
}

function toBoolean(self: Value) {
    switch (self.kind) {
        case "number":
            return self.value != 0;
        case "boolean":
            return self.value;
        case "string":
            return true;
        case "symbol":
            return true;
        case "closure":
            return true;
        case "array":
            return true;
        case "object":
            return true;
        case "null":
            return false;
        default:
            throw new Error();
    }
}
function toNumber(self: Value) {
    switch (self.kind) {
        case "number":
            return this.value;
        case "boolean":
            return this.value ? 1 : 0;
        case "string":
        case "symbol":
        case "closure":
        case "array":
        case "object":
        case "null":
        default:
            throw new Error();
    }
}

class Context {
    instructionBlocks: IInstruction[][];
    instructions: number;
    pc: number;
    stack: Value[];
    scope: IScope;
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
class VM {
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
        if (this[callName]) {
            return VM[callName].call(null, inst, context);
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
        ctx.pc = context.instructionBlocks[context.instructions].findIndex(x => x == self.label) + 1;
        return ctx;
    }
    static onBranchInstruction(self: BranchInstruction, context: Context): Context {
        const ctx = new Context(context);
        if (toBoolean(context.stack.pop())) {
            ctx.pc = context.instructionBlocks[context.instructions].findIndex(x => x == self.thenLabel) + 1;
        } else {
            ctx.pc = context.instructionBlocks[context.instructions].findIndex(x => x == self.elseLabel) + 1;
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
            const spread : ArrayValue = { kind: "array", value: context.stack.slice().reverse() };
            ctx.scope.values[self.ident] = spread
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
        ctx.scope = new IScope(ctx.scope);
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
        if (symbol.kind != "symbol") {
            throw new Error();
        }
        const scope = ctx.scope.getScope(symbol.value);
        if (scope == null) {
            throw new Error();
        }
        const ret = copyValue(scope.values[symbol.value]);
        const rhs = ctx.stack.peek();
        switch (self.op) {
            case "=":
                scope.values[symbol.value] = copyValue(rhs);
                break;
            case "+=":
                ret.kind = "number";
                ret.value = toNumber(ret) + toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "-=":
                ret.kind = "number";
                ret.value = toNumber(ret) - toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "*=":
                ret.kind = "number";
                ret.value = toNumber(ret) * toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "/=":
                ret.kind = "number";
                ret.value = toNumber(ret) / toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "%=":
                ret.kind = "number";
                ret.value = toNumber(ret) % toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "<<=":
                ret.kind = "number";
                ret.value = toNumber(ret) << toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "<<<=":
                ret.kind = "number";
                ret.value = toNumber(ret) << toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case ">>=":
                ret.kind = "number";
                ret.value = toNumber(ret) >> toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case ">>>=":
                ret.kind = "number";
                ret.value = toNumber(ret) >>> toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "&=":
                ret.kind = "number";
                ret.value = toNumber(ret) & toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "^=":
                ret.kind = "number";
                ret.value = toNumber(ret) ^ toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "|=":
                ret.kind = "number";
                ret.value = toNumber(ret) | toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            case "**=":
                ret.kind = "number";
                ret.value = toNumber(ret) ** toNumber(rhs);
                scope.values[symbol.value] = ret;
                break;
            default:
                throw new Error();
        }
        ctx.pc += 1;
        return ctx;
    }
    static onArrayAssignmentInstruction(self: ArrayAssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const index = ctx.stack.pop();
        if (index.kind != "number") {
            throw new Error();
        }
        const array = ctx.stack.pop();
        if (array.kind != "array") {
            throw new Error();
        }
        const lhs = array.value[index.value];
        const rhs = ctx.stack.peek();
        switch (self.op) {
            case "=":
                array.value[index.value] = copyValue(rhs);
                break;
            case "+=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) + toNumber(rhs)};
                break;
            case "-=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) - toNumber(rhs)};
                break;
            case "*=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) * toNumber(rhs)};
                break;
            case "/=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) / toNumber(rhs)};
                break;
            case "%=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) % toNumber(rhs)};
                break;
            case "<<=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) << toNumber(rhs)};
                break;
            case "<<<=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) << toNumber(rhs)};
                break;
            case ">>=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) >> toNumber(rhs)};
                break;
            case ">>>=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) >>> toNumber(rhs)};
                break;
            case "&=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) & toNumber(rhs)};
                break;
            case "^=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) ^ toNumber(rhs)};
                break;
            case "|=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) | toNumber(rhs)};
                break;
            case "**=":
                array.value[index.value] = {kind:"number", value:toNumber(lhs) ** toNumber(rhs)};
                break;
            default:
                throw new Error();
        }
        ctx.pc += 1;
        return ctx;
    }
    static onMemberAssignmentInstruction(self: MemberAssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const symbol = ctx.stack.pop();
        if (symbol.kind != "symbol") {
            throw new Error();
        }
        const object = ctx.stack.pop();
        if (object.kind != "object") {
            throw new Error();
        }
        const lhs = object.value[symbol.value];
        const rhs = ctx.stack.peek();
        switch (self.op) {
            case "=":
                object.value[symbol.value] = copyValue(rhs);
                break;
            case "+=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) + toNumber(rhs)};
                break;
            case "-=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) - toNumber(rhs)};
                break;
            case "*=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) * toNumber(rhs)};
                break;
            case "/=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) / toNumber(rhs)};
                break;
            case "%=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) % toNumber(rhs)};
                break;
            case "<<=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) << toNumber(rhs)};
                break;
            case "<<<=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) << toNumber(rhs)};
                break;
            case ">>=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) >> toNumber(rhs)};
                break;
            case ">>>=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) >>> toNumber(rhs)};
                break;
            case "&=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) & toNumber(rhs)};
                break;
            case "^=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) ^ toNumber(rhs)};
                break;
            case "|=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) | toNumber(rhs)};
                break;
            case "**=":
                object.value[symbol.value] = {kind:"number", value:toNumber(lhs) ** toNumber(rhs)};
                break;
            default:
                throw new Error();
        }
        ctx.pc += 1;
        return ctx;
    }
    static onConditionalInstruction(self: ConditionalInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        let ret: Value;
        switch (self.op) {
            case "==":
                ret = { kind: "boolean", value: (lhs.kind == rhs.kind) && (lhs.value == rhs.value) };
                break;
            case "!=":
                ret = { kind: "boolean", value: !((lhs.kind == rhs.kind) && (lhs.value == rhs.value)) };
                break;
            case ">=":
                ret = { kind: "boolean", value: toNumber(lhs) >= toNumber(rhs) };
                break;
            case "<=":
                ret = { kind: "boolean", value: toNumber(lhs) <= toNumber(rhs) };
                break;
            case ">":
                ret = { kind: "boolean", value: toNumber(lhs) > toNumber(rhs) };
                break;
            case "<":
                ret = { kind: "boolean", value: toNumber(lhs) < toNumber(rhs) };
                break;
            default:
                throw new Error();
        }
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onBinaryInstruction(self: BinaryInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        let ret: Value;
        switch (self.op) {
            case "+":
                ret = { kind: "number", value: toNumber(lhs) + toNumber(rhs) };
                break;
            case "-":
                ret = { kind: "number", value: toNumber(lhs) - toNumber(rhs) };
                break;
            case "*":
                ret = { kind: "number", value: toNumber(lhs) * toNumber(rhs) };
                break;
            case "/":
                ret = { kind: "number", value: toNumber(lhs) / toNumber(rhs) };
                break;
            case "%":
                ret = { kind: "number", value: toNumber(lhs) % toNumber(rhs) };
                break;
            case "<<":
                ret = { kind: "number", value: toNumber(lhs) << toNumber(rhs) };
                break;
            case "<<<":
                ret = { kind: "number", value: toNumber(lhs) << toNumber(rhs) };
                break;
            case ">>":
                ret = { kind: "number", value: toNumber(lhs) >> toNumber(rhs) };
                break;
            case ">>>":
                ret = { kind: "number", value: toNumber(lhs) >>> toNumber(rhs) };
                break;
            case "&":
                ret = { kind: "number", value: toNumber(lhs) & toNumber(rhs) };
                break;
            case "^":
                ret = { kind: "number", value: toNumber(lhs) ^ toNumber(rhs) };
                break;
            case "|":
                ret = { kind: "number", value: toNumber(lhs) | toNumber(rhs) };
                break;
            case "**":
                ret = { kind: "number", value: toNumber(lhs) ** toNumber(rhs) };
                break;
            default:
                throw new Error();
        }
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
                ret = { kind: "number", value: toNumber(rhs) };
                break;
            case "-":
                ret = { kind: "number", value: -toNumber(rhs) };
                break;
            case "~":
                ret = { kind: "number", value: ~toNumber(rhs) };
                break;
            case "!":
                ret = { kind: "boolean", value: !toBoolean(rhs) };
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

        const lhs = ctx.stack.pop();
        if (lhs.kind != "closure") {
            throw new Error();
        }

        const args = [];
        for (let i = 0; i < self.argc; i++) {
            args.push(ctx.stack.pop());
        }


        const closure = lhs.value;
        ctx.instructions = closure.func;
        ctx.pc = 0;
        ctx.callStack.push(context)
        ctx.stack = args;
        ctx.scope = new IScope(closure.scope);
        return ctx;
    }
    static onArrayIndexInstruction(self: ArrayIndexInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        if (lhs.kind != "array" || rhs.kind != "number") {
            throw new Error();
        }
        const array = lhs.value;
        const ret = array[toNumber(rhs)] || {kind:"null",value:null}; 
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onObjectMemberInstruction(self: ObjectMemberInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        if (lhs.kind != "object" || rhs.kind != "symbol") {
            throw new Error();
        }
        const obj = <{ [key: string]: Value }>lhs.value;
        const ret = obj[<string>rhs.value] || {kind:"null",value:null}; 
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onArrayLiteralInstruction(self: ArrayLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const values : Value[] = [];
        for (let i = 0; i < self.count; i++) {
            values.push(ctx.stack.pop());
        }
        values.reverse();
        ctx.stack.push({ kind: "array", value: values });
        ctx.pc += 1;
        return ctx;
    }
    static onObjectLiteralInstruction(self: ObjectLiteralInstruction, context: Context): Context  {
        const ctx = new Context(context);
        const values = {};
        for (let i = 0; i < self.count; i++) {
            const key = ctx.stack.pop();
            const value = ctx.stack.pop();
            if (key.kind != "symbol") {
                throw new Error();
            }
            values[<string>(key.value)] = value;
        }
        ctx.stack.push({kind: "object", value: values});
        ctx.pc += 1;
        return ctx;
    }
    static onStringLiteralInstruction(self: StringLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({kind: "string", value: self.value});
        ctx.pc += 1;
        return ctx;
    }
    static onFunctionInstruction(self: FunctionInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({kind: "closure", value:{ func: self.instructions, scope: context.scope }});
        ctx.pc += 1;
        return ctx;
    }
    static onNumericLiteralInstruction(self: NumericLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({kind: "number", value: self.value});
        ctx.pc += 1;
        return ctx;
    }
    static onNullLiteralInstruction(self: NullLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({kind: "null", value: null});
        ctx.pc += 1;
        return ctx;
    }
    static onBooleanLiteralInstruction(self: BooleanLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({kind: "boolean", value: self.value});
        ctx.pc += 1;
        return ctx;
    }
    static onIdentifierLiteralInstruction(self: IdentifierLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push({kind: "symbol", value: self.value});
        ctx.pc += 1;
        return ctx;
    }
    static onLoadInstruction(self: LoadInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(context.scope.get(self.value) || {kind: "null", value: null});
        ctx.pc += 1;
        return ctx;
    }
            
}
declare var jsDump: {
    parse(code: any): string;
}

declare const parser: peg.GeneratedParser<Program>;

window.onload = () => {
    const $ = document.getElementById.bind(document);

    console.log("loaded");
    let parser: peg.GeneratedParser = null;
    {
        const dom = document.querySelector("script[type='text/peg-js']");
        const grammer = dom.innerHTML;
        parser = <peg.GeneratedParser<Program>>peg.generate(grammer, { cache: false, optimize: "speed", output: "parser" });
    }
    let context: Context= null

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
            context.scope = new IScope(null);
            context.stack = [];
            context.callStack = [];

            $("run").removeAttribute("disabled");
            $("step").removeAttribute("disabled");
        } catch(e) {
            console.log(e);
            if (e instanceof parser.SyntaxError) {
                $("output").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("output").value = `${e.toString()}: ${e.message}`;
            }
        }
    }

    $("run").onclick = () => {
        try {
            for (let i = 0; i < 100; i++) {
                context = VM.accept(context);
            }
            $("output").value = jsDump.parse(context);
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
            context = VM.accept(context);
            $("output").value = jsDump.parse(context);
        } catch (e) {
            console.log(e);
            if (e instanceof parser.SyntaxError) {
                $("output").value = `line ${e.location.start.line} column ${e.location.start.column}: ${e.message}`;
            } else {
                $("output").value = `${e.toString()}: ${e.message}`;
            }
        }
    };};
