using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CParser2 {
    public class CCompiler {
        private List<Instruction> Instructions = new List<Instruction>();

        public Dictionary<string, Instruction.Label> labelTable = null;
        public Stack<Tuple<Instruction.Label/*break*/, Instruction.Label/*continue*/>> branchStack = new Stack<Tuple<Instruction.Label, Instruction.Label>>();
        private class FunctionInfo {
            internal Instruction.Variable result_variable;
            internal Instruction.Label return_label;
        }
        private FunctionInfo currentFunction = null;
        private Stack<Dictionary<int?, Instruction.Label>> casedefaultStack = new Stack<Dictionary<int?, Instruction.Label>>();

        private Scope currentScope {
            get; set;
        }

        private void PopLocalScope() {
            // ローカルスコープを一つ出る
        }

        private void PushLocalScope() {
            // ローカルスコープを一つ積む
        }


        public void ToString(SyntaxNode node) {
            currentScope = new Scope(Scope.Empty);
            node.Accept(this);
        }

        //
        //
        //

        public Tuple<CType, Instruction.Variable> Visit(SyntaxNode.Expression.UnaryExpression.PrefixIncrementExpression self) {
            var retExpr = self.operand.Accept<Tuple<CType, Instruction.Variable>>(this);
            var constantVar = new Instruction.Variable();
            GenLoadInt(constantVar, 1);
            this.Instructions.Add(new Instruction.Add(retExpr.Item2, retExpr.Item2, constantVar));
            return retExpr;
        }
        public Tuple<CType, Instruction.Variable> Visit(SyntaxNode.Expression.UnaryExpression.PrefixDecrementExpression self) {
            var retExpr = self.operand.Accept<Tuple<CType, Instruction.Variable>>(this);
            var constantVar = new Instruction.Variable();
            GenLoadInt(constantVar, 1);
            this.Instructions.Add(new Instruction.Sub(retExpr.Item2, retExpr.Item2, constantVar));
            return retExpr;
        }
        public Tuple<CType, Instruction.Variable> Visit(SyntaxNode.Expression.UnaryExpression.AddressExpression self) {
            var retExpr = self.operand.Accept<Tuple<CType, Instruction.Variable>>(this);
            if (retExpr.Item2.Place == Instruction.Variable.PlaceKind.Heap) {
                var constantVar = new Instruction.Variable();
                GenLoadAddress(constantVar, retExpr.Item2);
                this.Instructions.(new Instruction.Sub(retExpr.Item2, retExpr.Item2, constantVar));
                return retExpr;

            }
        }
        public void Visit(SyntaxNode.Expression.UnaryExpression.IndirectionExpression self) {
            return $"(*{ self.operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression self) {
            return $"{self.@operator.Accept<string>(this)}({ self.operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.UnaryExpression.SizeofExpression self) {
            return $"sizeof({ self.operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.UnaryExpression.SizeofTypeExpression self) {
            return $"sizeof({ self.operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.PrimaryExpression.ObjectSpecifier self) {
            return self.identifier;
        }
        public void Visit(SyntaxNode.Expression.PrimaryExpression.ConstantSpecifier self) {
            return self.constant;
        }
        public void Visit(SyntaxNode.Expression.PrimaryExpression.StringLiteralSpecifier self) {
            return String.Join(" ", self.literal);
        }
        public void Visit(SyntaxNode.Expression.PrimaryExpression.GroupedExpression self) {
            return $"({ self.expression.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.ArraySubscriptExpression self) {
            return $"{ self.expression.Accept<string>(this)}[{self.array_subscript.Accept<string>(this)}]";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.FunctionCallExpression self) {
            return $"{ self.expression.Accept<string>(this)}({string.Join(", ", self.argument_expression_list.Select(x => x.Accept<string>(this)))})";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.MemberAccessByValueExpression self) {
            return $"{ self.expression.Accept<string>(this)}.{self.identifier}";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.MemberAccessByPointerExpression self) {
            return $"{ self.expression.Accept<string>(this)}->{self.identifier}";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.PostfixIncrementExpression self) {
            return $"({ self.operand.Accept<string>(this)}++)";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.PostfixDecrementExpression self) {
            return $"({ self.operand.Accept<string>(this)}--)";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.CompoundLiteralExpression self) {
            return $"({self.type_name.Accept<string>(this)}) {{{string.Join(", ", self.initializers.Select(x => ($"{x.Item1.Accept<string>(this)} = {x.Item2.Accept<string>(this)}"))) }}})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator} { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.SimpleAssignmentExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} = { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.LogicalOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} || { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.LogicalAndExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} && { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.InclusiveOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} | { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.ExclusiveOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} ^ { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.AndExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} & { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.EqualityExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.Accept<string>(this)} { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.RelationalExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.Accept<string>(this)} { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.ShiftExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.Accept<string>(this)} { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.AdditiveExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.Accept<string>(this)} { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.Accept<string>(this)} { self.rhs_operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.ConditionalExpression self) {
            return $"({ self.condition.Accept<string>(this)} ? {self.then_expression.Accept<string>(this)} : { self.else_expression.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.CommaSeparatedExpression self) {
            return $"({string.Join(", ", self.expressions.Select(x => $"{x.Accept<string>(this)}"))})";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.CastExpression self) {
            var cast_type = self.type_name.Accept<string>(this);
            return $"(({cast_type}){ self.operand.Accept<string>(this)})";
        }
        public void Visit(SyntaxNode.Expression.PostfixExpression.ErrorExpression self) {
            return $"/* {self.statement.Accept<string>(this)} */";
        }

        //
        // 宣言解析
        //

        public void Visit(SyntaxNode.Declaration self) {
            foreach (var item in self.items) {
                item.Accept(this);
            }
        }
        public void Visit(SyntaxNode.FunctionDeclaration self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.init_declarator.declarator, null, currentScope, entries);
            foreach (var entry in entries) {
                var ident = currentScope.FindIdentifierCurrent(entry.Item1);
                if (ident == null) {
                    currentScope.AddIdentifier(entry.Item1, new Scope.IdentifierValue.Function(entry.Item2, entry.Item3), SyntaxNode.StorageClassSpecifierKind.none, -1);
                } else if (!(ident is Scope.IdentifierValue.Function)) {
                    throw new Exception("同名の識別子がすでに登録されている");
                } else if (((Scope.IdentifierValue.Function)ident).type.Equals(ret) == false) {
                    throw new Exception("同名だが型が違う関数を宣言しようとした");
                }
            }
        }
        public void Visit(SyntaxNode.VariableDeclaration self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.declarator, null, currentScope, entries);
            foreach (var entry in entries) {
                var ident = currentScope.FindIdentifierCurrent(entry.Item1);
                if (ident == null) {
                    currentScope.AddIdentifier(entry.Item1, new Scope.IdentifierValue.Function(entry.Item2, entry.Item3), SyntaxNode.StorageClassSpecifierKind.none, -1);
                } else if (!(ident is Scope.IdentifierValue.Variable)) {
                    throw new Exception("同名の識別子が登録されている");
                } else if (((Scope.IdentifierValue.Variable)ident).type.Equals(ret) == false) {
                    throw new Exception("同名だが型が違う変数を宣言しようとした");
                }
            }
        }
        public void Visit(SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.declarator, null, currentScope, entries);
            var entry = entries.First();

            var ident = currentScope.FindIdentifierCurrent(entry.Item1);
            var func = ident as Scope.IdentifierValue.Function;
            if (ident == null) {
                if (entry.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || entry.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword) {
                    throw new Exception("指定できない記憶クラスが指定されている");
                }
                func = new Scope.IdentifierValue.Function(entry.Item2, entry.Item3);
                currentScope.AddIdentifier(entry.Item1, func, SyntaxNode.StorageClassSpecifierKind.none, -1);
            } else if (func == null) {
                throw new Exception("同名の識別子がすでに登録されている");
            } else if (func.body != null) {
                throw new Exception("すでに関数には実体がある。");
            } else if (func.type.Equals(ret) == false) {
                throw new Exception("同名だが型が違う関数を宣言しようとした");
            }

            var ft = ret as CType.FunctionType;

            currentFunction = new FunctionInfo();
            currentFunction.result_variable = new Instruction.Variable();
            currentFunction.return_label = new Instruction.Label(".return");
            func.body = new Instruction.Label(entry.Item1);

            GenLabelDef(func.body);
            currentScope = new Scope(currentScope);
            this.Instructions.Add(new Instruction.Enter());
            var i = 0;
            //foreach (var param in ft.args) {
            //    if ((param.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || param.Item3 == SyntaxNode.StorageClassSpecifierKind.static_keyword || param.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword)) {
            //        throw new Exception("引数に指定できない記憶クラスが指定されている");
            //    }
            //    currentScope.AddIdentifier(param.Item1, new Scope.IdentifierValue.Argument(param.Item2, param.Item3, i), -1);
            //    i++;
            //}
            foreach (var param in self.parameterDefinition) {
                param.Accept(this, i++);
            }
            self.function_body.Accept(this);
            GenLabelDef(currentFunction.return_label);
            this.Instructions.Add(new Instruction.Leave());
            currentScope = currentScope.Parent;
        }
        public void Visit(SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.declarator, null, currentScope, entries);
            var entry = entries.First();

            var ident = currentScope.FindIdentifierCurrent(entry.Item1);
            var func = ident as Scope.IdentifierValue.Function;
            if (ident == null) {
                if (entry.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || entry.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword) {
                    throw new Exception("指定できない記憶クラスが指定されている");
                }
                func = new Scope.IdentifierValue.Function(entry.Item2, entry.Item3);
                currentScope.AddIdentifier(entry.Item1, func, SyntaxNode.StorageClassSpecifierKind.none, -1);
            } else if (func == null) {
                throw new Exception("同名の識別子がすでに登録されている");
            } else if (func.body != null) {
                throw new Exception("すでに関数には実体がある。");
            } else if (func.type.Equals(ret) == false) {
                throw new Exception("同名だが型が違う関数を宣言しようとした");
            }

            var ft = ret as CType.FunctionType;

            currentFunction = new FunctionInfo();
            currentFunction.result_variable = new Instruction.Variable();
            currentFunction.return_label = new Instruction.Label(".return");
            func.body = new Instruction.Label(entry.Item1);

            GenLabelDef(func.body);
            currentScope = new Scope(currentScope);
            this.Instructions.Add(new Instruction.Enter());
            var i = 0;
            //foreach (var param in ft.args) {
            //    if ((param.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || param.Item3 == SyntaxNode.StorageClassSpecifierKind.static_keyword || param.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword)) {
            //        throw new Exception("引数に指定できない記憶クラスが指定されている");
            //    }
            //    currentScope.AddIdentifier(param.Item1, new Scope.IdentifierValue.Argument(param.Item2, param.Item3, i), -1);
            //    i++;
            //}
            foreach (var param in self.parameterDefinition) {
                param.Accept(this, i++);
            }
            self.function_body.Accept(this);
            GenLabelDef(currentFunction.return_label);
            this.Instructions.Add(new Instruction.Leave());
            currentScope = currentScope.Parent;
        }
        public void Visit(SyntaxNode.Definition.VariableDefinition self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.init_declarator.declarator, null, currentScope, entries);
            foreach (var entry in entries) {
                var ident = currentScope.FindIdentifierCurrent(entry.Item1);
                Scope.IdentifierValue.Variable variable = ident as Scope.IdentifierValue.Variable;
                if (ident == null) {
                    if (entry.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || entry.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword) {
                        throw new Exception("指定できない記憶クラスが指定されている");
                    }
                    variable = new Scope.IdentifierValue.Variable(entry.Item2, entry.Item3);
                    currentScope.AddIdentifier(entry.Item1, variable, SyntaxNode.StorageClassSpecifierKind.none, -1);
                } else if (!(ident is Scope.IdentifierValue.Variable)) {
                    throw new Exception("同名の識別子が登録されている");
                    //} else if (variable.body != null) {
                    //    throw new Exception("すでに変数には実体がある。");
                } else if (((Scope.IdentifierValue.Variable)ident).type.Equals(ret) == false) {
                    throw new Exception("同名だが型が違う変数を宣言しようとした");
                }
                if (variable.body == null) {
                    variable.body = new Instruction.Variable();
                }
            }
        }
        public void Visit(SyntaxNode.Definition.ParameterDefinition self, int i) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.declarator, null, currentScope, entries);
            var entry = entries.First();
            if ((entry.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || entry.Item3 == SyntaxNode.StorageClassSpecifierKind.static_keyword || entry.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword)) {
                throw new Exception("引数に指定できない記憶クラスが指定されている");
            }
            var ident = currentScope.FindIdentifierCurrent(entry.Item1);
            if (ident != null) {
                throw new Exception("同名の識別子が登録されている");
            }
            currentScope.AddIdentifier(entry.Item1, new Scope.IdentifierValue.Argument(entry.Item2, entry.Item3, i), SyntaxNode.StorageClassSpecifierKind.none, -1);
        }
        public void Visit(SyntaxNode.TypeDeclaration.TypedefDeclaration self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.declaration_specifiers, self.init_declarator.declarator, null, currentScope, entries);
            var entry = entries.First();
            var ident = currentScope.FindIdentifierCurrent(entry.Item1);
            if (ident != null) {
                throw new Exception("同名の識別子が登録されている");
            }
            currentScope.AddIdentifier(entry.Item1, new Scope.IdentifierValue.Type(entry.Item2, entry.Item3), SyntaxNode.StorageClassSpecifierKind.none, -1);
        }
        public void Visit(SyntaxNode.TypeDeclaration.StructTypeDeclaration self) {
            var ident = currentScope.FindTaggedTypeCurrent(self.identifier);
            var st = ident as CType.TaggedType.StructType;
            if (ident != null) {
                if (st == null) {
                    throw new Exception("同名のタグ型はすでに登録されている");
                }
                if (st.members != null && self.struct_specifier.struct_declarations != null) {
                    throw new Exception("同名の構造体はすでに登録されている");
                }
            } else {
                st = new CType.TaggedType.StructType(self.identifier, self.struct_specifier.anonymous, null);
                currentScope.AddTaggedType(self.identifier, st);
            }

            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            if (self.struct_specifier.struct_declarations != null) {
                var structScope = currentScope = new Scope(currentScope);
                foreach (var item in self.struct_specifier.struct_declarations) {
                    item.Accept(this);
                }
                currentScope = currentScope.Parent;
                st.members = structScope.identifiers.Where(x => x.Item2 is Scope.IdentifierValue.Variable).Select(x => {
                    if (x.Item3 != SyntaxNode.StorageClassSpecifierKind.none) {
                        throw new Exception("指定できない記憶クラスが指定されている");
                    }
                    return Tuple.Create(x.Item1, (x.Item2 as Scope.IdentifierValue.Variable).type, x.Item3, x.Item4);
                }).ToList();
            }
        }
        public void Visit(SyntaxNode.TypeDeclaration.UnionTypeDeclaration self) {
            var ident = currentScope.FindTaggedTypeCurrent(self.identifier);
            var st = ident as CType.TaggedType.UnionType;
            if (ident != null) {
                if (st == null) {
                    throw new Exception("同名のタグ型はすでに登録されている");
                }
                if (st.members != null && self.union_specifier.struct_declarations != null) {
                    throw new Exception("同名の構造体はすでに登録されている");
                }
            } else {
                st = new CType.TaggedType.UnionType(self.identifier, self.union_specifier.anonymous, null);
                currentScope.AddTaggedType(self.identifier, st);
            }

            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            if (self.union_specifier.struct_declarations != null) {
                var structScope = currentScope = new Scope(currentScope);
                foreach (var item in self.union_specifier.struct_declarations) {
                    item.Accept(this);
                }
                currentScope = currentScope.Parent;
                st.members = structScope.identifiers.Where(x => x.Item2 is Scope.IdentifierValue.Variable).Select(x => {
                    if (x.Item3 != SyntaxNode.StorageClassSpecifierKind.none) {
                        throw new Exception("指定できない記憶クラスが指定されている");
                    }
                    return Tuple.Create(x.Item1, (x.Item2 as Scope.IdentifierValue.Variable).type, x.Item3, x.Item4);
                }).ToList();
            }
        }
        public void Visit(SyntaxNode.TypeDeclaration.EnumTypeDeclaration self) {
            var ident = currentScope.FindTaggedTypeCurrent(self.identifier);
            var st = ident as CType.TaggedType.EnumType;
            if (ident != null) {
                if (st == null) {
                    throw new Exception("同名のタグ型はすでに登録されている");
                }
                if (st.enumerators != null && self.enum_specifier.enumerators != null) {
                    throw new Exception("同名の構造体はすでに登録されている");
                }
            } else {
                st = new CType.TaggedType.EnumType(self.identifier, self.enum_specifier.anonymous, null);
                currentScope.AddTaggedType(self.identifier, st);
            }

            if (self.enum_specifier.enumerators != null) {
                int i = 0;
                foreach (var item in self.enum_specifier.enumerators) {
                    var intValue = EvalToConstInt(item.expression);
                    currentScope.AddIdentifier(item.identifier, new Scope.IdentifierValue.EnumMember(st, i++), SyntaxNode.StorageClassSpecifierKind.none, -1);
                }
            }
        }
        public void Visit(SyntaxNode.DeclarationSpecifiers self) {
            //List<string> specs = new List<string>();
            //if (self.storage_class_specifier != SyntaxNode.StorageClassSpecifierKind.none) {
            //    specs.Add(self.storage_class_specifier.Accept<string>(this));
            //}
            //if (self.type_qualifiers != null) {
            //    specs.AddRange(self.type_qualifiers.Select(x => x.Accept<string>(this)));
            //}
            //if (self.type_specifiers != null) {
            //    specs.AddRange(self.type_specifiers.Select(x => x.Accept<string>(this)));
            //}
            //if (self.function_specifier != SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.none) {
            //    specs.Add(self.function_specifier.Accept<string>(this));
            //}
            //return String.Join(" ", specs.Where(x => !string.IsNullOrWhiteSpace(x)));
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeDeclaration.InitDeclarator self) {
            //return $"{self.declarator.Accept<string>(this)}" + (self.initializer != null ? " = " + self.initializer.Accept<string>(this) : "");
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.StructSpecifier self) {
            //return $"struct {self.identifier}";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.UnionSpecifier self) {
            //return $"union {self.identifier}";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.StandardTypeSpecifier self) {
            //return $"{self.Kind.Accept<string>(this)}";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.TypedefTypeSpecifier self) {
            //return $"{self.identifier}";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.StructDeclaration self) {
            if (self.items != null) {
                foreach (var item in self.items) {
                    item.Accept(this);
                }
            }
        }
        public void Visit(SyntaxNode.MemberDeclaration self) {
            var entries = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
            var ret = TypeBuilder.Parse(self.specifier_qualifier_list.type_specifiers, self.specifier_qualifier_list.type_qualifiers, SyntaxNode.StorageClassSpecifierKind.none, SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.none, self.struct_declarator.declarator, null, currentScope, entries);
            foreach (var entry in entries) {
                var ident = currentScope.FindIdentifierCurrent(entry.Item1);
                Scope.IdentifierValue.Variable variable = ident as Scope.IdentifierValue.Variable;
                if (ident == null) {
                    if (entry.Item3 == SyntaxNode.StorageClassSpecifierKind.extern_keyword || entry.Item3 == SyntaxNode.StorageClassSpecifierKind.typedef_keyword) {
                        throw new Exception("指定できない記憶クラスが指定されている");
                    }
                    variable = new Scope.IdentifierValue.Variable(entry.Item2, entry.Item3);
                    currentScope.AddIdentifier(entry.Item1, variable, SyntaxNode.StorageClassSpecifierKind.none, -1);
                } else {
                    throw new Exception("同名のメンバが登録されている");
                }
            }
        }
        public void Visit(SyntaxNode.SpecifierQualifierList self) {
            //var items = new List<string>();
            //if (self.type_qualifiers != null) {
            //    items.AddRange(self.type_qualifiers.Select(x => x.Accept<string>(this)));
            //}
            //if (self.type_specifiers != null) {
            //    items.AddRange(self.type_specifiers.Select(x => x.Accept<string>(this)));
            //}
            //return String.Join(" ", items);
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.StructDeclarator self) {
            //if (self.bitfield_expr != null) {
            //    return $"{self.declarator.Accept<string>(this)} : {self.bitfield_expr.Accept<string>(this)}";
            //} else {
            //    return $"{self.declarator.Accept<string>(this)}";
            //}
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeSpecifier.EnumSpecifier self) {
            //string items = "";
            //if (self.enumerators != null) {
            //    items = " { " + String.Join(", ", self.enumerators.Select(x => x.Accept<string>(this))) + "}";
            //}
            //return $"enum {self.identifier}{items};";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.TypeSpecifier.EnumSpecifier.Enumerator self) {
            //return self.identifier + ((self.expression != null) ? " = " + self.expression.Accept<string>(this) : "");
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.GroupedDeclarator self) {
            //return $"({self.@base.Accept<string>(this)})";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.IdentifierDeclarator self) {
            //var ptr = (self.pointer) != null ? (String.Join(" ", self.pointer.Select(x => x.Accept<string>(this))) + " ") : "";
            //return ptr + self.identifier;
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.ArrayDeclarator self) {
            //var ptr = (self.pointer == null) ? "" : (String.Join(" ", self.pointer.Select(x => x.Accept<string>(this))) + " ");
            //var sz = (self.size_expression == null) ? "" : self.size_expression.Accept<string>(this);
            //return $"{ptr}{self.@base.Accept<string>(this)}[{sz}]";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator self) {
            //return $"{self.@base.Accept<string>(this)}()";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator self) {
            //var pars = (self.identifier_list != null) ? string.Join(", ", self.identifier_list) : "";
            //return $"{self.@base.Accept<string>(this)}({pars})";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator self) {
            //return $"{self.@base.Accept<string>(this)}{self.parameter_type_list.Accept<string>(this)}";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator self) {
            //return $"{self.@base.Accept<string>(this)}({self.parameter_type_list.Accept<string>(this)})";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator self) {
            //return $"{self.@base.Accept<string>(this)}[{self.size_expression.Accept<string>(this)}]";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.AbstractDeclarator.PointerAbstractDeclarator self) {
            //var parts = new List<string>();
            //if (self.pointer != null) {
            //    parts.AddRange(self.pointer.Select(x => x.Accept<string>(this)));
            //}
            //if (self.@base != null) {
            //    parts.Add(self.@base.Accept<string>(this));
            //}
            //return string.Join(" ", parts);
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator self) {
            //string p = (self.pointer != null) ? (String.Concat(self.pointer.Select(x => x.Accept<string>(this)))) : "";
            //string b = self.@base != null ? self.@base.Accept<string>(this) : "";
            //return $"({b}{p})";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.ParameterTypeList self) {
            //return "(" + (self.parameters != null ? String.Join(", ", self.parameters.Select(x => x.Accept<string>(this))) + (self.have_va_list ? ", ..." : "") : "") + ")";
            throw new NotImplementedException();
        }
        public void Visit(SyntaxNode.ParameterDeclaration self) {
            //return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}";
            throw new NotImplementedException();
        }

        //
        // statements
        //

        public void Visit(SyntaxNode.Statement.ErrorStatement self) {
            throw new Exception("ErrorStatement");
        }
        public void Visit(SyntaxNode.Statement.LabeledStatement.DefaultLabeledStatement self) {
            if (casedefaultStack.Any() == false) {
                throw new Exception("switch文外でdefaultラベルを定義している。");
            }

            var table = casedefaultStack.Peek();
            if (table.Any(x => x.Key.HasValue == false)) {
                throw new Exception($"default ラベルは既に定義済み");
            }
            table[null] = new Instruction.Label($"default");

            self.statement.Accept(this);
        }
        public void Visit(SyntaxNode.Statement.LabeledStatement.CaseLabeledStatement self) {
            if (casedefaultStack.Any() == false) {
                throw new Exception("switch文外でcaseラベルを定義している。");
            }

            var table = casedefaultStack.Peek();
            int constv = EvalToConstInt(self.expression);
            if (table.ContainsKey(constv) == true) {
                throw new Exception($"case {constv} ラベルは既に定義済み");
            }
            table[constv] = new Instruction.Label($"case {constv}");

            self.statement.Accept(this);
        }

        public void Visit(SyntaxNode.Statement.LabeledStatement.GenericLabeledStatement self) {
            if (labelTable == null) {
                throw new Exception("関数外でラベルを定義している。");
            }
            Instruction.Label label;
            if (labelTable.TryGetValue(self.label, out label) == false) {
                label = new Instruction.Label(self.label);
                labelTable[self.label] = label;
            }
            GenLabelDef(label);
        }
        public void Visit(SyntaxNode.Statement.CompoundStatement self) {
            if (self.block_items != null) {
                PushLocalScope();
                foreach (var item in self.block_items) {
                    item.Accept(this);
                }
                PopLocalScope();
            }
        }
        public void Visit(SyntaxNode.Statement.ExpressionStatement self) {
            self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
        }
        public void Visit(SyntaxNode.Statement.SelectionStatement.IfStatement self) {

            var exprResult = self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);

            if (self.then_statement != null && self.else_statement != null) {
                Instruction.Label elseLabel = new Instruction.Label(".else");
                Instruction.Label junctionLabel = new Instruction.Label(".junction");
                GenBranchIfFalse(exprResult, elseLabel);
                self.then_statement.Accept(this);
                GenGoto(junctionLabel);
                GenLabelDef(elseLabel);
                self.else_statement.Accept(this);
                GenLabelDef(junctionLabel);
            } else if (self.then_statement != null && self.else_statement == null) {
                Instruction.Label junctionLabel = new Instruction.Label(".junction");
                GenBranchIfFalse(exprResult, junctionLabel);
                self.then_statement.Accept(this);
                GenLabelDef(junctionLabel);
            } else if (self.then_statement == null && self.else_statement != null) {
                Instruction.Label elseLabel = new Instruction.Label(".else");
                Instruction.Label junctionLabel = new Instruction.Label(".junction");
                GenBranchIfTrue(exprResult, junctionLabel);
                self.else_statement.Accept(this);
                GenLabelDef(junctionLabel);
            } else {
                // nothing
            }
        }
        public void Visit(SyntaxNode.Statement.SelectionStatement.SwitchStatement self) {
            Instruction.Label breakLabel = new Instruction.Label(".break");
            branchStack.Push(Tuple.Create(breakLabel, (Instruction.Label)null));
            casedefaultStack.Push(new Dictionary<int?, Instruction.Label>());

            var exprResult = self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
            var intExprResult = tointtype(exprResult);
            var insertMark = Instructions.Count();
            self.statement.Accept(this);
            GenLabelDef(breakLabel);
            casedefaultStack.Pop();
            branchStack.Pop();


            Instruction.Label defaultLabel = casedefaultStack.Peek().Where(x => !x.Key.HasValue).Select(x => x.Value).FirstOrDefault() ?? breakLabel;
            List<Tuple<int, Instruction.Label>> caseLabels = casedefaultStack.Peek().Where(x => x.Key != null).Select(x => Tuple.Create(x.Key.Value, x.Value)).OrderBy(x => x.Item1).ToList();

            List<Instruction> branchInstructions = caseLabels.SelectMany(x => {
                List<Instruction> insts = new List<Instruction>();

                var constantVar = new Instruction.Variable();
                GenLoadInt(constantVar, x.Item1, insts);

                var eqResultVar = new Instruction.Variable();
                GenEq(eqResultVar, intExprResult.Item2, constantVar, insts);

                var eqResultExp = Tuple.Create((CType)new CType.StandardType(CType.StandardType.TypeBit._bool), eqResultVar);
                GenBranchIfTrue(eqResultExp, x.Item2, insts);

                return insts;
            }).ToList();

            GenGoto(defaultLabel, branchInstructions);

            Instructions.InsertRange(insertMark, branchInstructions);
        }

        public void Visit(SyntaxNode.Statement.IterationStatement.C99ForStatement self) {
            Instruction.Label breakLabel = new Instruction.Label(".break");
            Instruction.Label continueLabel = new Instruction.Label(".continue");
            Instruction.Label loopheadLabel = new Instruction.Label(".loophead");
            branchStack.Push(Tuple.Create(breakLabel, continueLabel));

            PushLocalScope();   // c99では for の外側に { が挿入される

            self.declaration.Accept(this);
            GenLabelDef(loopheadLabel);
            var exprResult = self.condition_statement.Accept<Tuple<CType, Instruction.Variable>>(this);
            GenBranchIfFalse(exprResult, breakLabel);
            self.body_statement.Accept(this);
            GenLabelDef(continueLabel);
            self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
            GenGoto(loopheadLabel);
            GenLabelDef(breakLabel);
            branchStack.Pop();

            PopLocalScope();   // c99では for の外側に } が挿入される

        }

        public void Visit(SyntaxNode.Statement.IterationStatement.ForStatement self) {
            Instruction.Label breakLabel = new Instruction.Label(".break");
            Instruction.Label continueLabel = new Instruction.Label(".continue");
            Instruction.Label loopheadLabel = new Instruction.Label(".loophead");
            branchStack.Push(Tuple.Create(breakLabel, continueLabel));

            self.initial_statement.Accept(this);
            GenLabelDef(loopheadLabel);
            var exprResult = self.condition_statement.Accept<Tuple<CType, Instruction.Variable>>(this);
            GenBranchIfFalse(exprResult, breakLabel);
            self.body_statement.Accept(this);
            GenLabelDef(continueLabel);
            self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
            GenGoto(loopheadLabel);
            GenLabelDef(breakLabel);
            branchStack.Pop();
        }
        public void Visit(SyntaxNode.Statement.IterationStatement.DoStatement self) {

            Instruction.Label breakLabel = new Instruction.Label(".break");
            Instruction.Label continueLabel = new Instruction.Label(".continue");
            Instruction.Label loopheadLabel = new Instruction.Label(".loophead");

            branchStack.Push(Tuple.Create(breakLabel, continueLabel));
            GenLabelDef(loopheadLabel);
            self.statement.Accept(this);
            GenLabelDef(continueLabel);
            var exprResult = self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
            GenBranchIfTrue(exprResult, loopheadLabel);
            GenLabelDef(breakLabel);
            branchStack.Pop();

        }


        public void Visit(SyntaxNode.Statement.IterationStatement.WhileStatement self) {
            Instruction.Label breakLabel = new Instruction.Label(".break");
            Instruction.Label continueLabel = new Instruction.Label(".continue");
            branchStack.Push(Tuple.Create(breakLabel, continueLabel));

            GenLabelDef(continueLabel);
            var exprResult = self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
            GenBranchIfFalse(exprResult, breakLabel);
            self.statement.Accept(this);
            GenGoto(continueLabel);
            GenLabelDef(breakLabel);
            branchStack.Pop();

        }

        public void Visit(SyntaxNode.Statement.JumpStatement.ReturnStatement self) {
            if (currentFunction == null) {
                throw new Exception("関数外でreturn文を使っている。");
            }

            Instruction.Variable variable = currentFunction.result_variable;

            if (self.expression == null && variable != null) {
                throw new Exception("return 文に戻り値が指定されていない。");
            }
            if (self.expression != null && variable == null) {
                throw new Exception("return 文に戻り値が指定されている。");
            }
            if (self.expression != null && variable != null) {
                var exprResult = self.expression.Accept<Tuple<CType, Instruction.Variable>>(this);
                // type check here!
                var instStoreResult = new Instruction.Store(variable, exprResult.Item2);
            }

            GenGoto(currentFunction.return_label);
        }
        public void Visit(SyntaxNode.Statement.JumpStatement.BreakStatement self) {
            if (branchStack.Any() == false) {
                throw new Exception("関数外でbreak文を使っている。");
            }
            Instruction.Label label = null;
            foreach (var branch in branchStack) {
                if (branch.Item1 != null) {
                    label = branch.Item1;
                    break;
                }
            }
            if (label == null) {
                throw new Exception("ループ, switch文外でbreak文を使っている。");
            }

            GenGoto(label);
        }
        public void Visit(SyntaxNode.Statement.JumpStatement.ContinueStatement self) {
            if (branchStack.Any() == false) {
                throw new Exception("関数外でcontinue文を使っている。");
            }
            Instruction.Label label = null;
            foreach (var branch in branchStack) {
                if (branch.Item2 != null) {
                    label = branch.Item2;
                    break;
                }
            }
            if (label == null) {
                throw new Exception("ループ外でcontinue文を使っている。");
            }
            GenGoto(label);
        }
        public void Visit(SyntaxNode.Statement.JumpStatement.GotoStatement self) {
            if (labelTable == null) {
                throw new Exception("関数外でgoto文を使っている。");
            }
            Instruction.Label label;
            if (labelTable.TryGetValue(self.identifier, out label) == false) {
                label = new Instruction.Label(self.identifier);
                labelTable[self.identifier] = label;
            }
            GenGoto(label);
        }
        public void Visit(SyntaxNode.TranslationUnit self) {
            if (self.external_declarations != null) {
                foreach (var external_declaration in self.external_declarations) {
                    external_declaration.Accept(this);
                }
            }
        }
        public void Visit(SyntaxNode.TypeName self) {
            var parts = new List<string>();
            if (self.specifier_qualifier_list != null) {
                parts.Add(self.specifier_qualifier_list.Accept<string>(this));
            }
            if (self.type_declaration != null) {
                parts.Add(self.type_declaration.Accept<string>(this));
            }
            if (self.abstract_declarator != null) {
                parts.Add(self.abstract_declarator.Accept<string>(this));
            }

            return string.Join(" ", parts);
        }
        public void Visit(SyntaxNode.Initializer self) {
            var inits = new List<string>();
            if (self.initializers != null) {
                inits.AddRange(self.initializers.Select(x => (x.Item1 != null ? x.Item1.Accept<string>(this) + " = " : "") + x.Item2.Accept<string>(this)));
                return inits.Any() ? "{ " + String.Join(", ", inits) + " }" : "{}";
            } else if (self.expression != null) {
                return self.expression.Accept<string>(this);
            } else {
                throw new Exception("Either initializer or expression must be specified.");
            }
        }
        public void Visit(SyntaxNode.Initializer.Designator.MemberDesignator self) {
            return "." + self.identifier;
        }
        public void Visit(SyntaxNode.Initializer.Designator.IndexDesignator self) {
            return $"[{self.expression.Accept<string>(this)}]";
        }


        //
        // instruction dsl
        //
        private void GenEq(Instruction.Variable result, Instruction.Variable lhs, Instruction.Variable rhs, List<Instruction> instructions) {
            var inst = new Instruction.Eq(result, lhs, rhs);
            instructions.Add(inst);

        }

        private void GenEq(Instruction.Variable result, Instruction.Variable lhs, Instruction.Variable rhs) {
            GenEq(result, lhs, rhs, this.Instructions);
        }

        private void GenLabelDef(Instruction.Label label) {
            GenLabelDef(label, this.Instructions);
        }
        private void GenLabelDef(Instruction.Label label, List<Instruction> insts) {
            Instruction.LabelDef labelDef = new Instruction.LabelDef(label);
            label.DefinedBy(labelDef);
            insts.Add(labelDef);
        }
        private void GenBranchIfTrue(Tuple<CType, Instruction.Variable> exprResult, Instruction.Label label) {
            GenBranchIfTrue(exprResult, label, this.Instructions);
        }
        private void GenBranchIfTrue(Tuple<CType, Instruction.Variable> exprResult, Instruction.Label label, List<Instruction> insts) {
            var booledResult = tobooltype(exprResult, insts);
            Instruction.BranchIfTrue branchIfTrue = new Instruction.BranchIfTrue(booledResult.Item2, label);
            label.ReferencedFrom(branchIfTrue);
            insts.Add(branchIfTrue);
        }
        private void GenBranchIfFalse(Tuple<CType, Instruction.Variable> exprResult, Instruction.Label label) {
            GenBranchIfFalse(exprResult, label, this.Instructions);
        }
        private void GenBranchIfFalse(Tuple<CType, Instruction.Variable> exprResult, Instruction.Label label, List<Instruction> insts) {
            var booledResult = tobooltype(exprResult, insts);
            Instruction.BranchIfFalse branchIfFalse = new Instruction.BranchIfFalse(booledResult.Item2, label);
            label.ReferencedFrom(branchIfFalse);
            insts.Add(branchIfFalse);
        }
        private void GenGoto(Instruction.Label label) {
            GenGoto(label, this.Instructions);
        }
        private void GenGoto(Instruction.Label label, List<Instruction> insts) {
            Instruction.Goto @goto = new Instruction.Goto(label);
            label.ReferencedFrom(@goto);
            insts.Add(@goto);
        }


        private void GenLoadInt(Instruction.Variable var, int value) {
            GenLoadInt(var, value, this.Instructions);
        }

        private void GenLoadInt(Instruction.Variable var, int value, List<Instruction> insts) {
            var constant = new Instruction.Constant.IntConst(value);
            var constantLoadInst = new Instruction.Load(var, constant);
            insts.Add(constantLoadInst);
        }

        //
        //
        //

        private Tuple<CType, Instruction.Variable> tobooltype(Tuple<CType, Instruction.Variable> exprResult) {
            return tobooltype(exprResult, this.Instructions);
        }
        private Tuple<CType, Instruction.Variable> tobooltype(Tuple<CType, Instruction.Variable> exprResult, List<Instruction> insts) {
            // exprResult を bool 型に変換する
            return exprResult;
        }

        private Tuple<CType, Instruction.Variable> tointtype(Tuple<CType, Instruction.Variable> exprResult) {
            return tointtype(exprResult, this.Instructions);
        }
        private Tuple<CType, Instruction.Variable> tointtype(Tuple<CType, Instruction.Variable> exprResult, List<Instruction> insts) {
            // exprResult を int 型に変換する
            return exprResult;
        }

        //
        // 定数式評価
        //

        private int EvalToConstInt(SyntaxNode.Expression expression) {
            throw new NotImplementedException();
        }

    }

    public abstract class Instruction {

        /// <summary>
        /// 式の値
        /// </summary>
        public class Value {
            enum ValueKind {
                GlobalVariable, // グローバル変数の参照 GlobalBase + OffsetAddress  
                LocalVariable,  // ローカル変数の参照   ベースポインタ + OffsetAddress
                Constant,       // 定数（整数・小数・アドレス値）
                Variable        // 仮想レジスタ変数
            };

            public ValueKind kind { get; }
            public CType type { get; }


            public Label GlobalBase;
            public int OffsetAddress;
            public int Value;
        }

        /// <summary>
        /// 仮想レジスタ変数
        /// </summary>
        public class Variable {
            public enum PlaceKind {
                Stack,
                Heap,
            }
            internal readonly PlaceKind Place = PlaceKind.Stack;

            public string name { get; } = "";
        }

        /// <summary>
        /// 定数値
        /// </summary>
        internal class Constant {
            internal class IntConst : Constant {
                private int item1;

                public IntConst(int item1) {
                    this.item1 = item1;
                }
            }
        }

        /// <summary>
        /// ラベル
        /// </summary>
        public class Label {
            private LabelDef _DefinedBy = null;
            private List<Instruction> _ReferencedFrom = new List<Instruction>();

            public void DefinedBy(LabelDef defed) {
                this._DefinedBy = defed;
            }
            public void ReferencedFrom(Instruction refed) {
                this._ReferencedFrom.Add(refed);
            }
            public string identifier {
                get;
            }
            public Label(string ident) {
                this.identifier = ident;
            }
        }

        public class LabelDef : Instruction {
            public Label target {
                get;
            }
            public LabelDef(Label target) {
                this.target = target;
            }
        }
        public class Goto : Instruction {
            public Label target {
                get;
            }
            public Goto(Label target) {
                this.target = target;
            }
        }

        internal class Store : Instruction {
            private Variable exprResult;
            private Variable variable;

            /// <summary>
            /// 変数をコピー
            /// </summary>
            /// <param name="variable"></param>
            /// <param name="exprResult"></param>
            public Store(Variable variable, Variable exprResult) {
                this.variable = variable;
                this.exprResult = exprResult;
            }
        }

        internal class BranchIfFalse : Instruction {
            private Label target;
            private Variable variable;

            public BranchIfFalse(Variable variable, Label target) {
                this.variable = variable;
                this.target = target;
            }
        }
        internal class BranchIfTrue : Instruction {
            private Label target;
            private Variable variable;

            public BranchIfTrue(Variable variable, Label target) {
                this.variable = variable;
                this.target = target;
            }
        }

        internal class Load : Instruction {
            private Constant constant;
            private Variable constantVar;

            public Load(Variable constantVar, Constant constant) {
                this.constantVar = constantVar;
                this.constant = constant;
            }
        }

        internal class Eq : Instruction {
            private Variable result;
            private Variable lhs;
            private Variable rhs;

            public Eq(Variable result, Variable lhs, Variable rhs) {
                this.result = result;
                this.lhs = lhs;
                this.rhs = rhs;
            }
        }

        internal class Enter : Instruction {
        }

        internal class Leave : Instruction {
        }

        internal class Add : Instruction {
            private Variable result;
            private Variable lhs;
            private Variable rhs;

            public Add(Variable result, Variable lhs, Variable rhs) {
                this.result = result;
                this.lhs = lhs;
                this.rhs = rhs;
            }
        }
        internal class Sub : Instruction {
            private Variable result;
            private Variable lhs;
            private Variable rhs;

            public Sub(Variable result, Variable lhs, Variable rhs) {
                this.result = result;
                this.lhs = lhs;
                this.rhs = rhs;
            }
        }
        internal class Mul : Instruction {
            private Variable result;
            private Variable lhs;
            private Variable rhs;

            public Mul(Variable result, Variable lhs, Variable rhs) {
                this.result = result;
                this.lhs = lhs;
                this.rhs = rhs;
            }
        }
        internal class Div : Instruction {
            private Variable result;
            private Variable lhs;
            private Variable rhs;

            public Div(Variable result, Variable lhs, Variable rhs) {
                this.result = result;
                this.lhs = lhs;
                this.rhs = rhs;
            }
        }
        internal class Mod : Instruction {
            private Variable result;
            private Variable lhs;
            private Variable rhs;

            public Mod(Variable result, Variable lhs, Variable rhs) {
                this.result = result;
                this.lhs = lhs;
                this.rhs = rhs;
            }
        }
    }

    public abstract class CObject {
    }
}
