/// <reference path="typings/pegjs.d.ts" />

import * as Api from "./typings/api";

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
    constructor(public op: string, public lhs: IExpression, public rhs: IExpression) { }
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
    constructor(public op: string, public lhs: IExpression, public rhs: IExpression) { }
}

class RelationalExpression implements IExpression {
    constructor(public op: string, public lhs: IExpression, public rhs: IExpression) { }
}

class ShiftExpression implements IExpression {
    constructor(public op: string, public lhs: IExpression, public rhs: IExpression) { }
}

class AdditiveExpression implements IExpression {
    constructor(public op: string, public lhs: IExpression, public rhs: IExpression) { }
}

class MultiplicativeExpression implements IExpression {
    constructor(public op: string, public lhs: IExpression, public rhs: IExpression) { }
}

class ExponentiationExpression implements IExpression {
    constructor(public lhs: IExpression, public rhs: IExpression) { }
}

class UnaryExpression implements IExpression {
    constructor(public op: string, public rhs: IExpression) { }
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
    constructor(public label: LabelInstruction) {}
}
class BranchInstruction implements IInstruction {
    constructor(public thenLabel: LabelInstruction, public elseLabel: LabelInstruction) {}
}
class BindInstruction implements IInstruction {
    constructor(public ident: string) {}
}
class ReturnInstruction implements IInstruction {
    constructor(public hasValue: boolean) {}
}
class EnterInstruction implements IInstruction {
    constructor() {}
}
class LeaveInstruction implements IInstruction {
    constructor() {}
}
class PopInstruction implements IInstruction {
    constructor() {}
}
class AssignmentInstruction implements IInstruction {
    constructor(public op:string) {}
}
class ConditionalInstruction implements IInstruction {
    constructor(public op:string) {}
}
class LogicalInstruction implements IInstruction {
    constructor(public op:string) {}
}
class BitwiseInstruction implements IInstruction {
    constructor(public op:string) {}
}
class EqualityInstruction implements IInstruction {
    constructor(public op:string) {}
}
class RelationalInstruction implements IInstruction {
    constructor(public op:string) {}
}
class BinaryInstruction implements IInstruction {
    constructor(public op:string) {}
}
class UnaryInstruction implements IInstruction {
    constructor(public op:string) {}
}
class CallInstruction implements IInstruction {
    constructor(public argc:number) {}
}
class ArrayIndexInstruction implements IInstruction {
    constructor() {}
}
class ObjectMemberInstruction implements IInstruction {
    constructor(public member:string) {}
}
class ArrayLiteralInstruction implements IInstruction {
    constructor(public count:number) {}
}
class ObjectLiteralInstruction implements IInstruction {
    constructor(public count:number) {}
}
class StringLiteralInstruction implements IInstruction {
    constructor(public value:string) {}
}
class FunctionInstruction implements IInstruction {
    constructor(public params:FunctionParameter[], public instructions:IInstruction[]) {}
}
class NumericLiteralInstruction implements IInstruction {
    constructor(public value:number) {}
}
class NullLiteralInstruction implements IInstruction {
    constructor() {}
}
class BooleanLiteralInstruction implements IInstruction {
    constructor(public value:boolean) {}
}
class IdentifierLiteralInstruction implements IInstruction {
    constructor(public value:string) {}
}

class Compiler {
    instructions:IInstruction[];
    breakTarget:LabelInstruction[];
    continueTarget:LabelInstruction[];
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

        let labels: [LabelInstruction,IStatement][] = [];

        const elseLabel = self.clauses.reduce((s,x) => {
            this.instructions.push(s);
            const matchLabel = new LabelInstruction();
            labels.push([matchLabel,x.stmt]);
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
        this.instructions.push(this.breakTarget[this.breakTarget.length-1]);
        return null;
    }
    onContinueStatement(self: ContinueStatement) {
        this.instructions.push(this.continueTarget[this.continueTarget.length-1]);
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
        this.accept(self.lhs);
        this.instructions.push(new AssignmentInstruction(self.op));
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
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new LogicalInstruction("Or"));
        return null;
    }
    onLogicalANDExpression(self: LogicalANDExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new LogicalInstruction("And"));
        return null;
    }
    onBitwiseORExpression(self: BitwiseORExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BitwiseInstruction("Or"));
        return null;
    }
    onBitwiseXORExpression(self: BitwiseXORExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BitwiseInstruction("Xor"));
        return null;
    }
    onBitwiseANDExpression(self: BitwiseANDExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BitwiseInstruction("And"));
        return null;
    }
    onEqualityExpression(self: EqualityExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new EqualityInstruction(self.op));
        return null;
    }
    onRelationalExpression(self: RelationalExpression) {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new RelationalInstruction(self.op));
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
    onMultiplicativeExpression(self: MultiplicativeExpression)  {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction(self.op));
        return null;
    }
    onExponentiationExpression(self: ExponentiationExpression)  {
        this.accept(self.rhs);
        this.accept(self.lhs);
        this.instructions.push(new BinaryInstruction("**"));
        return null;
    }
    onUnaryExpression(self: UnaryExpression)  {
        this.accept(self.rhs);
        this.instructions.push(new UnaryInstruction(self.op));
        return null;
    }
    onCallExpression(self: CallExpression)  {
        self.args.forEach(x => this.accept(x));
        this.accept(self.lhs);
        this.instructions.push(new CallInstruction(self.args.length));
        return null;
    }
    onArrayIndexExpression(self: ArrayIndexExpression)  {
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
    onObjectLiteral(self: ObjectLiteral)  {
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
    onNumericLiteral(self: NumericLiteral)  {
        this.instructions.push(new NumericLiteralInstruction(self.value));
        return null;
    }
    onStringLiteral(self: StringLiteral)  {
        this.instructions.push(new StringLiteralInstruction(self.value));
        return null;
    }
    onIdentifierLiteral(self: StringLiteral)  {
        this.instructions.push(new IdentifierLiteralInstruction(self.value));
        return null;
    }
}
interface IValue {
    kind:string;
}
class 

class Context {
    instructions : IInstruction[];
    pc:number;
    stack:IValue[];
}
class VM {
static onLabelInstruction(context:Context) : Context { return context; }
static onJumpInstruction(context:Context) : Context { return context; }
static onBranchInstruction(context:Context) : Context { return context; }
static onBindInstruction(context:Context) : Context { return context; }
static onReturnInstruction(context:Context) : Context { return context; }
static onEnterInstruction(context:Context) : Context { return context; }
static onLeaveInstruction(context:Context) : Context { return context; }
static onPopInstruction(context:Context) : Context { return context; }
static onAssignmentInstruction(context:Context) : Context { return context; }
static onConditionalInstruction(context:Context) : Context { return context; }
static onLogicalInstruction(context:Context) : Context { return context; }
static onBitwiseInstruction(context:Context) : Context { return context; }
static onEqualityInstruction(context:Context) : Context { return context; }
static onRelationalInstruction(context:Context) : Context { return context; }
static onBinaryInstruction(context:Context) : Context { return context; }
static onUnaryInstruction(context:Context) : Context { return context; }
static onCallInstruction(context:Context) : Context { return context; }
static onArrayIndexInstruction(context:Context) : Context { return context; }
static onObjectMemberInstruction(context:Context) : Context { return context; }
static onArrayLiteralInstruction(context:Context) : Context { return context; }
static onObjectLiteralInstruction(context:Context) : Context { return context; }
static onStringLiteralInstruction(context:Context) : Context { return context; }
static onFunctionInstruction(context:Context) : Context { return context; }
static onNumericLiteralInstruction(context:Context) : Context { return context; }
static onNullLiteralInstruction(context:Context) : Context { return context; }
static onBooleanLiteralInstruction(context:Context) : Context { return context; }
static onIdentifierLiteralInstruction(context:Context) : Context { return context; }
}
declare var jsDump: {
    parse(code: any): string;
}

declare const parser: peg.GeneratedParser<Program>;

window.onload = () => {
    const $ = document.getElementById.bind(document);
    let parser : Api.GeneratedParser = null;
    {
        const dom = document.querySelector("script[type='text/peg-js']");
        const grammer = dom.innerHTML;
        parser = peg.generate(grammer, <Api.IBuildOptions<"parser">>{ cache:false, optimize:"speed", output: "parser"});
    }

    $("eval").onclick = () => {
        try {
            const result = parser.parse($("code").value);
            const c = new Compiler();
            c.accept(result);
            
            //$("output").value = JSON.stringify(c.instructions);
            $("output").value = jsDump.parse(c.instructions);
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
