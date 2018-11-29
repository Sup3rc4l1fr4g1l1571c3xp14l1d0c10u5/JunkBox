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
    constructor(public ident: string, public initExpr: IExpression) { }
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
    constructor(public lhs: IExpression, public member: string) { }
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
    constructor(public key: string, public value: IExpression) { }
}

class FunctionExpression implements IExpression {
    constructor(public params: FunctionParameter[], public body) { }
}

class FunctionParameter implements IExpression {
    constructor(public ident: string, public init: IExpression, public spread: boolean) { }
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

interface IInstruction {

}
class LabelInstruction implements IInstruction {

}
class JumpInstruction implements IInstruction {
    constructor(public label: LabelInstruction) { }
}
class BranchInstruction implements IInstruction {
    constructor(public thenLabel: LabelInstruction, public elseLabel: LabelInstruction) { }
}
class BindInstruction implements IInstruction {
    constructor(public ident: string) { }
}
class ReturnInstruction implements IInstruction {
    constructor(public hasValue: boolean) { }
}
class EnterInstruction implements IInstruction {
    constructor() { }
}
class LeaveInstruction implements IInstruction {
    constructor() { }
}
class PopInstruction implements IInstruction {
    constructor() { }
}
class SimpleAssignmentInstruction implements IInstruction {
    constructor(public op: AssignmentOperator) { }
}
class ArrayAssignmentInstruction implements IInstruction {
    constructor(public op: AssignmentOperator) { }
}
class MemberAssignmentInstruction implements IInstruction {
    constructor(public op: AssignmentOperator) { }
}
class ConditionalInstruction implements IInstruction {
    constructor(public op: ConditionalOperator) { }
}
class BinaryInstruction implements IInstruction {
    constructor(public op: BinaryOperator) { }
}
class UnaryInstruction implements IInstruction {
    constructor(public op: UnaryOperator) { }
}
class CallInstruction implements IInstruction {
    constructor(public argc: number) { }
}
class ArrayIndexInstruction implements IInstruction {
    constructor() { }
}
class ObjectMemberInstruction implements IInstruction {
    constructor(public member: string) { }
}
class ArrayLiteralInstruction implements IInstruction {
    constructor(public count: number) { }
}
class ObjectLiteralInstruction implements IInstruction {
    constructor(public count: number) { }
}
class StringLiteralInstruction implements IInstruction {
    constructor(public value: string) { }
}
class FunctionInstruction implements IInstruction {
    constructor(public params: FunctionParameter[], public instructions: IInstruction[]) { }
}
class NumericLiteralInstruction implements IInstruction {
    constructor(public value: number) { }
}
class NullLiteralInstruction implements IInstruction {
    constructor() { }
}
class BooleanLiteralInstruction implements IInstruction {
    constructor(public value: boolean) { }
}
class IdentifierLiteralInstruction implements IInstruction {
    constructor(public value: string) { }
}

class Compiler {
    instructions: IInstruction[];
    breakTarget: LabelInstruction[];
    continueTarget: LabelInstruction[];
    constructor() {
        this.instructions = [];
        this.breakTarget = [];
        this.continueTarget = [];
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
        const thenLabel = new LabelInstruction();
        const elseLabel = new LabelInstruction();
        const joinLabel = new LabelInstruction();

        this.accept(self.condExpr);
        this.instructions.push(new BranchInstruction(thenLabel, elseLabel));

        this.instructions.push(thenLabel);
        this.accept(self.thenStmt);
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(elseLabel);
        this.accept(self.elseStmt);
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(joinLabel);

        return null;
    }
    onDoStatement(self: DoStatement) {
        const headLabel = new LabelInstruction();
        const contLabel = new LabelInstruction();
        const brakLabel = new LabelInstruction();
        this.breakTarget.push(brakLabel);
        this.continueTarget.push(contLabel);

        this.instructions.push(headLabel);
        this.accept(self.bodyStmt);

        this.instructions.push(contLabel);
        this.accept(self.condExpr);
        this.instructions.push(new BranchInstruction(headLabel, brakLabel));

        this.instructions.push(brakLabel);

        return null;
    }
    onWhileStatement(self: WhileStatement) {
        const headLabel = new LabelInstruction();
        const contLabel = new LabelInstruction();
        const brakLabel = new LabelInstruction();
        this.breakTarget.push(brakLabel);
        this.continueTarget.push(contLabel);

        this.instructions.push(contLabel);
        this.accept(self.condExpr);
        this.instructions.push(new BranchInstruction(headLabel, brakLabel));

        this.instructions.push(headLabel);
        this.accept(self.bodyStmt);
        this.instructions.push(new JumpInstruction(contLabel));

        this.instructions.push(brakLabel);

        return null;
    }
    onForStatement(self: ForStatement) {
        const headLabel = new LabelInstruction();
        const contLabel = new LabelInstruction();
        const brakLabel = new LabelInstruction();
        const bodyLabel = new LabelInstruction();
        this.breakTarget.push(brakLabel);
        this.continueTarget.push(contLabel);

        this.accept(self.initExpr);

        this.instructions.push(headLabel);
        this.accept(self.condExpr);
        this.instructions.push(new BranchInstruction(bodyLabel, brakLabel));

        this.instructions.push(bodyLabel);
        this.accept(self.bodyStmt);

        this.instructions.push(contLabel);
        this.accept(self.updateExpr);
        this.instructions.push(new JumpInstruction(headLabel));

        this.instructions.push(brakLabel);

        return null;
    }
    onLexicalDeclaration(self: LexicalDeclaration) {
        self.binds.map(x => this.accept(x));
        return null;
    }
    onLexicalBinding(self: LexicalBinding) {
        this.accept(self.initExpr);
        this.instructions.push(new BindInstruction(self.ident));
        return null;
    }
    onSwitchStatement(self: SwitchStatement) {

        this.accept(self.expr);

        const brakLabel = new LabelInstruction();
        this.breakTarget.push(brakLabel);
        let defaultLabel = brakLabel;

        let labels: [LabelInstruction, IStatement][] = [];

        const elseLabel = self.clauses.reduce((s, x) => {
            this.instructions.push(s);
            const matchLabel = new LabelInstruction();
            labels.push([matchLabel, x.stmt]);
            s = x.clauses.reduce((s, y) => {
                if (y instanceof CaseClause) {
                    this.instructions.push(s);
                    s = new LabelInstruction();
                    this.accept(y.expr);
                    this.instructions.push(new BranchInstruction(matchLabel, s));
                } else {
                    defaultLabel = matchLabel;
                }
                return s;
            }, s);
            return s;
        }, new LabelInstruction());

        this.instructions.push(elseLabel);
        this.instructions.push(new JumpInstruction(defaultLabel));

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
        this.instructions.push(this.breakTarget.peek());
        return null;
    }
    onContinueStatement(self: ContinueStatement) {
        this.instructions.push(this.continueTarget.peek());
        return null;
    }
    onReturnStatement(self: ReturnStatement) {
        if (self.expr) {
            this.accept(self.expr);
            this.instructions.push(new ReturnInstruction(true));
        } else {
            this.instructions.push(new ReturnInstruction(false));
        }
        return null;
    }
    onBlockStatement(self: BlockStatement) {
        this.instructions.push(new EnterInstruction());
        for (const statement of self.statements) {
            this.accept(statement);
        }
        this.instructions.push(new LeaveInstruction());
        return null;
    }
    onExpressionStatement(self: ExpressionStatement) {
        this.accept(self.expr);
        this.instructions.push(new PopInstruction());
        return null;
    }
    onCommaExpression(self: CommaExpression) {
        self.exprs.forEach((expr, i) => {
            if (i !== 0) {
                this.instructions.push(new PopInstruction());
            }
            this.accept(expr);
        });
        return null;
    }
    onAssignmentExpression(self: AssignmentExpression) {
        this.accept(self.rhs);
        if (self.lhs instanceof IdentifierLiteral) {
            this.instructions.push(new IdentifierLiteralInstruction(self.lhs.value));
            this.instructions.push(new SimpleAssignmentInstruction(self.op));
        } else if (self.lhs instanceof ArrayIndexExpression) {
            this.accept(self.lhs.lhs);
            this.accept(self.lhs.index);
            this.instructions.push(new ArrayAssignmentInstruction(self.op));
        } else if (self.lhs instanceof ObjectMemberExpression) {
            this.accept(self.lhs.lhs);
            this.instructions.push(new IdentifierLiteralInstruction(self.lhs.member));
            this.instructions.push(new ArrayAssignmentInstruction(self.op));
        } else {
            throw new Error();
        }
        return null;
    }
    onConditionalExpression(self: ConditionalExpression) {
        const thenLabel = new LabelInstruction();
        const elseLabel = new LabelInstruction();
        const joinLabel = new LabelInstruction();

        this.accept(self.condExpr);
        this.instructions.push(new BranchInstruction(thenLabel, elseLabel));

        this.instructions.push(thenLabel);
        this.accept(self.thenExpr);
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(elseLabel);
        this.accept(self.elseExpr);
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(joinLabel);

        return null;
    }
    onLogicalORExpression(self: LogicalORExpression) {
        const thenLabel = new LabelInstruction();
        const elseLabel = new LabelInstruction();
        const joinLabel = new LabelInstruction();

        this.accept(self.lhs);
        this.instructions.push(new BranchInstruction(thenLabel, elseLabel));

        this.instructions.push(thenLabel);
        this.instructions.push(new BooleanLiteralInstruction(true));
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(elseLabel);
        this.accept(self.rhs);
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(joinLabel);

        return null;
    }
    onLogicalANDExpression(self: LogicalANDExpression) {
        const thenLabel = new LabelInstruction();
        const elseLabel = new LabelInstruction();
        const joinLabel = new LabelInstruction();

        this.accept(self.lhs);
        this.instructions.push(new BranchInstruction(thenLabel, elseLabel));

        this.instructions.push(thenLabel);
        this.accept(self.rhs);
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(elseLabel);
        this.instructions.push(new BooleanLiteralInstruction(false));
        this.instructions.push(new JumpInstruction(joinLabel));

        this.instructions.push(joinLabel);

        return null;
    }
    onBitwiseORExpression(self: BitwiseORExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction("|"));
        return null;
    }
    onBitwiseXORExpression(self: BitwiseXORExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction("^"));
        return null;
    }
    onBitwiseANDExpression(self: BitwiseANDExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction("&"));
        return null;
    }
    onEqualityExpression(self: EqualityExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new ConditionalInstruction(self.op));
        return null;
    }
    onRelationalExpression(self: RelationalExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new ConditionalInstruction(self.op));
        return null;
    }
    onShiftExpression(self: ShiftExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction(self.op));
        return null;
    }
    onAdditiveExpression(self: AdditiveExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction(self.op));
        return null;
    }
    onMultiplicativeExpression(self: MultiplicativeExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction(self.op));
        return null;
    }
    onExponentiationExpression(self: ExponentiationExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction("**"));
        return null;
    }
    onUnaryExpression(self: UnaryExpression) {
        this.accept(self.rhs);
        this.instructions.push(new UnaryInstruction(self.op));
        return null;
    }
    onCallExpression(self: CallExpression) {
        self.args.forEach(x => this.accept(x));
        this.accept(self.lhs);
        this.instructions.push(new CallInstruction(self.args.length));
        return null;
    }
    onArrayIndexExpression(self: ArrayIndexExpression) {
        this.accept(self.index);
        this.accept(self.lhs);
        this.instructions.push(new ArrayIndexInstruction());
        return null;
    }
    onObjectMemberExpression(self: ObjectMemberExpression) {
        this.accept(self.lhs);
        this.instructions.push(new ObjectMemberInstruction(self.member));
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
        this.instructions.push(new ArrayLiteralInstruction(self.values.length));
        return null;
    }
    onObjectLiteral(self: ObjectLiteral) {
        self.values.forEach(expr => {
            this.accept(expr.value);
            this.instructions.push(new StringLiteralInstruction(expr.key));
        });
        this.instructions.push(new ObjectLiteralInstruction(self.values.length));
        return null;
    }
    //onPropertyDefinition(self: PropertyDefinition) { return null; }
    onFunctionExpression(self: FunctionExpression) {
        const compiler = new Compiler();
        compiler.accept(self.body)
        this.instructions.push(new FunctionInstruction(self.params, compiler.instructions));
        return null;
    }
    //onFunctionParameter(self: FunctionParameter) { return null; }
    onNullLiteral(self: NullLiteral) {
        this.instructions.push(new NullLiteralInstruction());
        return null;
    }
    onBooleanLiteral(self: BooleanLiteral) {
        this.instructions.push(new BooleanLiteralInstruction(self.value));
        return null;
    }
    onNumericLiteral(self: NumericLiteral) {
        this.instructions.push(new NumericLiteralInstruction(self.value));
        return null;
    }
    onStringLiteral(self: StringLiteral) {
        this.instructions.push(new StringLiteralInstruction(self.value));
        return null;
    }
    onIdentifierLiteral(self: StringLiteral) {
        this.instructions.push(new IdentifierLiteralInstruction(self.value));
        return null;
    }
}

class IScope {
    values: { [key: string]: IValue };
    prev: IScope;
    constructor(prev: IScope) {
        this.values = {};
        this.prev = prev;
    }
}

type Closure = { func: IInstruction[], scope: IScope };
class IValue {
    kind: "number" | "boolean" | "string" | "closure" | "array" | "object" | "null";
    value: number | boolean | string | Closure | IValue[] | { [key: string]: IValue };
    constructor() {
        this.kind = "null";
        this.value = null;
    }
    toBoolean() {
        switch (this.kind) {
            case "number":
                return (<number>this.value) != 0;
            case "boolean":
                return (<boolean>this.value);
            case "string":
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
    toNumber() {
        switch (this.kind) {
            case "number":
                return (<number>this.value);
            case "boolean":
                return (<boolean>this.value) ? 1 : 0;
            case "string":
            case "closure":
            case "array":
            case "object":
            case "null":
            default:
                throw new Error();
        }
    }
}

class Context {
    instructions: IInstruction[];
    pc: number;
    stack: IValue[];
    scope: IScope;
    callStack: Context[];
    constructor(context?: Context) {
        if (context) {
            this.instructions = context.instructions;
            this.pc = context.pc;
            this.stack = context.stack;
            this.scope = context.scope;
            this.callStack = context.callStack;
        } else {
            this.instructions = null;
            this.pc = null;
            this.stack = null;
            this.scope = null;
            this.callStack = null;
        }
    }
}
class VM {
    static accept(context: Context): Context {
        const inst : any = context.instructions[context.pc];
        if (inst == null) {
            return context;
        }
        const callName = "on" + inst.__proto__.constructor.name;
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
        ctx.pc = ctx.instructions.findIndex(x => x == self.label) + 1;
        return ctx;
    }
    static onBranchInstruction(self: BranchInstruction, context: Context): Context {
        const ctx = new Context(context);
        if (context.stack.pop().toBoolean()) {
            ctx.pc = ctx.instructions.findIndex(x => x == self.thenLabel) + 1;
        } else {
            ctx.pc = ctx.instructions.findIndex(x => x == self.elseLabel) + 1;
        }
        return ctx;
    }
    static onBindInstruction(self: BindInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx[self.ident] = context.stack.pop();
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
        ctx.scope = ctx.scope.prev;
        ctx.pc += 1;
        return ctx;
    }
    static onPopInstruction(self: PopInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.pop();
        ctx.pc += 1;
        return ctx;
    }
    static onAssignmentInstruction(self: AssignmentInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.peek();
        switch (self.op) {
            case "=":
                lhs.kind = rhs.kind;
                lhs.value = rhs.value;
                break;
            case "+=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() + rhs.toNumber();
                break;
            case "-=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() - rhs.toNumber();
                break;
            case "*=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() * rhs.toNumber();
                break;
            case "/=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() / rhs.toNumber();
                break;
            case "%=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() % rhs.toNumber();
                break;
            case "<<=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() << rhs.toNumber();
                break;
            case "<<<=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() << rhs.toNumber();
                break;
            case ">>=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() >> rhs.toNumber();
                break;
            case ">>>=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() >>> rhs.toNumber();
                break;
            case "&=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() & rhs.toNumber();
                break;
            case "^=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() ^ rhs.toNumber();
                break;
            case "|=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() | rhs.toNumber();
                break;
            case "**=":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() ** rhs.toNumber();
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
        const ret = new IValue();
        switch (self.op) {
            case "==":
                ret.kind = "boolean";
                ret.value = (lhs.kind == rhs.kind) && (lhs.value == rhs.value);
                break;
            case "!=":
                ret.kind = "boolean";
                ret.value = !((lhs.kind == rhs.kind) && (lhs.value == rhs.value));
                break;
            case ">=":
                ret.kind = "boolean";
                ret.value = lhs.toNumber() >= rhs.toNumber();
                break;
            case "<=":
                ret.kind = "boolean";
                ret.value = lhs.toNumber() <= rhs.toNumber();
                break;
            case ">":
                ret.kind = "number";
                ret.value = lhs.toNumber() > rhs.toNumber();
                break;
            case "<":
                ret.kind = "number";
                ret.value = lhs.toNumber() < rhs.toNumber();
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
        const ret = new IValue();
        switch (self.op) {
            case "+":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() + rhs.toNumber();
                break;
            case "-":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() - rhs.toNumber();
                break;
            case "*":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() * rhs.toNumber();
                break;
            case "/":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() / rhs.toNumber();
                break;
            case "%":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() % rhs.toNumber();
                break;
            case "<<":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() << rhs.toNumber();
                break;
            case "<<<":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() << rhs.toNumber();
                break;
            case ">>":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() >> rhs.toNumber();
                break;
            case ">>>":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() >>> rhs.toNumber();
                break;
            case "&":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() & rhs.toNumber();
                break;
            case "^":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() ^ rhs.toNumber();
                break;
            case "|":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() | rhs.toNumber();
                break;
            case "**":
                lhs.kind = "number";
                lhs.value = lhs.toNumber() ** rhs.toNumber();
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
        const ret = new IValue();
        switch (self.op) {
            case "+":
                ret.kind = "number";
                ret.value = rhs.toNumber();
                break;
            case "-":
                ret.kind = "number";
                ret.value = -rhs.toNumber();
                break;
            case "~":
                ret.kind = "number";
                ret.value = ~rhs.toNumber();
                break;
            case "!":
                ret.kind = "boolean";
                ret.value = !rhs.toBoolean();
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
        const args = [];
        for (let i = 0; i < self.argc; i++) {
            args.push(ctx.stack.pop());
        }

        const lhs = ctx.stack.pop();
        if (lhs.kind != "closure") {
            throw new Error();
        }

        const closure = <Closure>lhs.value;
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
        const array = <IValue[]>lhs.value;
        const ret = array[rhs.toNumber()] || new IValue(); 
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onObjectMemberInstruction(self: ObjectMemberInstruction, context: Context): Context {
        const ctx = new Context(context);
        const lhs = ctx.stack.pop();
        const rhs = ctx.stack.pop();
        if (lhs.kind != "object" || rhs.kind != "string") {
            throw new Error();
        }
        const obj = <{ [key: string]: IValue }>lhs.value;
        const ret = obj[rhs.toString()] || new IValue(); 
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onArrayLiteralInstruction(self: ArrayLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const values = [];
        for (let i = 0; i < self.count; i++) {
            values.push(ctx.stack.pop());
        }
        values.reverse();
        const ret = new IValue();
        ret.kind = "array";
        ret.value = values;
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onObjectLiteralInstruction(self: ObjectLiteralInstruction, context: Context): Context  {
        const ctx = new Context(context);
        const values = {};
        for (let i = 0; i < self.count; i++) {
            const key = ctx.stack.pop();
            const value = ctx.stack.pop();
            if (key.kind != "string") {
                throw new Error();
            }
            values[<string>(key.value)] = value;
        }
        const ret = new IValue();
        ret.kind = "object";
        ret.value = values;
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onStringLiteralInstruction(self: StringLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const ret = new IValue();
        ret.kind = "string";
        ret.value = self.value;
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onFunctionInstruction(self: FunctionInstruction, context: Context): Context {
        const ctx = new Context(context);
        const ret = new IValue();
        ret.kind = "closure";
        ret.value = { func: self.instructions, scope: context.scope };
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onNumericLiteralInstruction(self: NumericLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const ret = new IValue();
        ret.kind = "number";
        ret.value = self.value;
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onNullLiteralInstruction(self: NullLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const ret = new IValue();
        ret.kind = "null";
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onBooleanLiteralInstruction(self: BooleanLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        const ret = new IValue();
        ret.kind = "boolean";
        ret.value = self.value;
        ctx.stack.push(ret);
        ctx.pc += 1;
        return ctx;
    }
    static onIdentifierLiteralInstruction(self: IdentifierLiteralInstruction, context: Context): Context {
        const ctx = new Context(context);
        ctx.stack.push(context.scope.values[self.value] || new IValue());
        ctx.pc += 1;
        return ctx;
    }

}
declare var jsDump: {
    parse(code: any): string;
}

declare const parser: peg.GeneratedParser<Program>;

window.onload = () => {
    console.log("loaded");
    const $ = document.getElementById.bind(document);
    let parser: peg.GeneratedParser = null;
    {
        const dom = document.querySelector("script[type='text/peg-js']");
        const grammer = dom.innerHTML;
        parser = <peg.GeneratedParser<Program>>peg.generate(grammer, { cache: false, optimize: "speed", output: "parser" });
    }

    $("eval").onclick = () => {
        try {
            const result = parser.parse($("code").value);
            const c = new Compiler();
            c.accept(result);

            //$("output").value = JSON.stringify(c.instructions);
            //$("output").value = jsDump.parse(c.instructions);


            let context = new Context();
            context.instructions = c.instructions;
            context.pc = 0;
            context.scope = new IScope(null);
            context.stack = [];
            context.callStack = null;

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
};
