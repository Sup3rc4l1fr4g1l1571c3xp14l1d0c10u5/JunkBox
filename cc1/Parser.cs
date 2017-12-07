using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    /// <summary>
    /// �p�[�T
    /// </summary>
    public class Parser {
        /// <summary>
        /// ���O���(�X�e�[�g�����g ���x��)
        /// </summary>
        private Scope<SyntaxTree.Statement.GenericLabeledStatement> label_scope = Scope<SyntaxTree.Statement.GenericLabeledStatement>.Empty.Extend();

        /// <summary>
        /// ���O���(�\���́A���p�́A�񋓑̂̃^�O��)
        /// </summary>
        private Scope<CType.TaggedType> _tagScope = Scope<CType.TaggedType>.Empty.Extend();

        /// <summary>
        /// ���O���(�ʏ�̎��ʎq�i�ϐ��A�֐��A�����A�񋓒萔)
        /// </summary>
        private Scope<IdentifierScopeValue> _identScope = Scope<IdentifierScopeValue>.Empty.Extend();

        /// <summary>
        /// ���O���(Typedef��)
        /// </summary>
        private Scope<SyntaxTree.Declaration.TypeDeclaration> _typedefScope = Scope<SyntaxTree.Declaration.TypeDeclaration>.Empty.Extend();

        // �\���̂܂��͋��p�̂̃����o�[�ɂ��Ă͂��ꂼ��̐錾�I�u�W�F�N�g�ɕt�^�����

        /// <summary>
        /// break���߂ɂ��ẴX�R�[�v
        /// </summary>
        private readonly Stack<SyntaxTree.Statement> _breakScope = new Stack<SyntaxTree.Statement>();

        /// <summary>
        /// continue���߂ɂ��ẴX�R�[�v
        /// </summary>
        private readonly Stack<SyntaxTree.Statement> _continueScope = new Stack<SyntaxTree.Statement>();

        //
        // lex spec
        //


        private Lexer lexer {
            get;
        }


        public Parser(string s) {
            lexer = new Lexer(s, "<built-in>");

            // GCC�̑g�ݍ��݌^�̐ݒ�
            _typedefScope.Add("__builtin_va_list", new SyntaxTree.Declaration.TypeDeclaration("__builtin_va_list", CType.CreatePointer(new CType.BasicType(TypeSpecifier.Void))));

        }


        public void Parse() {
            var ret = translation_unit();
            Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));
        }


        private void EoF() {
            if (!lexer.is_eof()) {
                throw new Exception();
            }
        }




        private bool is_ENUMERATION_CONSTANT() {
            if (!is_IDENTIFIER(false)) {
                return false;
            }
            var ident = lexer.current_token();
            IdentifierScopeValue v;
            if (_identScope.TryGetValue(ident.Raw, out v) == false) {
                return false;
            }
            return (v as IdentifierScopeValue.EnumValue)?.ParentType.Members.First(x => x.Ident == ident.Raw) != null;
        }

        private CType.TaggedType.EnumType.MemberInfo ENUMERATION_CONSTANT() {
            var ident = IDENTIFIER(false);
            IdentifierScopeValue v;
            if (_identScope.TryGetValue(ident, out v) == false) {
                throw new Exception();
            }
            if (!(v is IdentifierScopeValue.EnumValue)) {
                throw new Exception();
            }
            var el = ((IdentifierScopeValue.EnumValue)v).ParentType.Members.First(x => x.Ident == ident);
            return el;
        }

        private bool is_CHARACTER_CONSTANT() {
            return lexer.current_token().Kind == Token.TokenKind.STRING_CONSTANT;
        }

        private string CHARACTER_CONSTANT() {
            if (is_CHARACTER_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        private bool is_FLOATING_CONSTANT() {
            return lexer.current_token().Kind == Token.TokenKind.FLOAT_CONSTANT;
        }
        private SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant FLOATING_CONSTANT() {
            if (is_FLOATING_CONSTANT() == false) {
                throw new Exception();
            }
            var raw = lexer.current_token().Raw;
            var m = Lexer.ParseFloat(raw);
            var value = Convert.ToDouble(m.Item1);
            CType.BasicType.TypeKind type;
            switch (m.Item2) {
                case "F":
                    type = CType.BasicType.TypeKind.Float;
                    break;
                case "L":
                    type = CType.BasicType.TypeKind.LongDouble;
                    break;
                case "":
                    type = CType.BasicType.TypeKind.Double;
                    break;
                default:
                    throw new Exception();
            }
            lexer.next_token();
            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant(raw, value, type);
        }

        private bool is_INTEGER_CONSTANT() {
            return lexer.current_token().Kind == Token.TokenKind.HEXIMAL_CONSTANT | lexer.current_token().Kind == Token.TokenKind.OCTAL_CONSTANT | lexer.current_token().Kind == Token.TokenKind.DECIAML_CONSTANT;
        }
        private SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant INTEGER_CONSTANT() {
            if (is_INTEGER_CONSTANT() == false) {
                throw new Exception();
            }
            string raw = lexer.current_token().Raw;
            string body;
            string suffix;
            int radix;
            CType.BasicType.TypeKind[] candidates;

            switch (lexer.current_token().Kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT: {
                        var m = Lexer.ParseHeximal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 16;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }

                        break;
                    }
                case Token.TokenKind.OCTAL_CONSTANT: {
                        var m = Lexer.ParseOctal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 8;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    }
                case Token.TokenKind.DECIAML_CONSTANT: {
                        var m = Lexer.ParseDecimal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 10;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongLongInt };
                                break;
                            case "UL":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    }
                default:
                    throw new Exception();

            }

            var originalSigned = Convert.ToInt64(body, radix);
            var originalUnsigned = Convert.ToUInt64(body, radix);
            Int64 value = 0;

            CType.BasicType.TypeKind selectedType = 0;
            System.Diagnostics.Debug.Assert(candidates.Length > 0);
            foreach (var candidate in candidates) {
                switch (candidate) {
                    case CType.BasicType.TypeKind.SignedInt: {
                            var v = Convert.ToInt32(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.UnsignedInt: {
                            var v = Convert.ToUInt32(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.SignedLongInt: {
                            var v = Convert.ToInt32(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.UnsignedLongInt: {
                            var v = Convert.ToUInt32(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.SignedLongLongInt: {
                            var v = Convert.ToInt64(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.UnsignedLongLongInt: {
                            var v = Convert.ToUInt64(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    default:
                        throw new Exception();
                }
                selectedType = candidate;
                break;
            }

            lexer.next_token();

            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(raw, value, selectedType);

        }

        private bool is_STRING() {
            return lexer.current_token().Kind == Token.TokenKind.STRING_LITERAL;
        }
        private string STRING() {
            if (is_STRING() == false) {
                throw new Exception();
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        //
        // Grammers
        //


        /// <summary>
        /// 6.9 �O����`(�|��P��)
        /// </summary>
        /// <returns></returns>
        public SyntaxTree.TranslationUnit translation_unit() {
            var ret = new SyntaxTree.TranslationUnit();
            while (is_external_declaration(null, TypeSpecifier.None)) {
                ret.declarations.AddRange(external_declaration());
            }
            EoF();
            return ret;
        }

        /// <summary>
        /// 6.9 �O����`(�O���錾�ƂȂ肦�邩�H)
        /// </summary>
        /// <returns></returns>
        private bool is_external_declaration(CType baseType, TypeSpecifier typeSpecifier) {
            return (is_declaration_specifier(baseType, typeSpecifier) || lexer.Peek(';') || is_declarator());
        }

        /// <summary>
        /// 6.9 �O����`(�O���錾)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Declaration> external_declaration() {
            CType baseType = null;
            StorageClassSpecifier storageClass = StorageClassSpecifier.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;
            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }
            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            baseType = baseType.WrapTypeQualifier(typeQualifier);

            var ret = new List<SyntaxTree.Declaration>();


            if (!is_declarator()) {
                if (!baseType.IsStructureType() && !baseType.IsEnumeratedType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "��̐錾�͎g�p�ł��܂���B");
                } else if (baseType.IsStructureType() && (baseType.Unwrap() as CType.TaggedType.StructUnionType).IsAnonymous) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "�����\����/���p�̂��錾����Ă��܂����A���̃C���X�^���X���`���Ă��܂���B");
                } else {
                    lexer.Read(';');
                    return ret;
                }
            } else {
                for (; ; ) {
                    string ident = "";
                    List<CType> stack = new List<CType>() { new CType.StubType() };
                    declarator(ref ident, stack, 0);
                    var type = CType.Resolve(baseType, stack);
                    if (lexer.Peek('=', ',', ';')) {
                        // �錾

                        SyntaxTree.Declaration decl = func_or_var_or_typedef_declaration(ident, type, storageClass, functionSpecifier);

                        ret.Add(decl);


                        if (lexer.ReadIf(',')) {
                            continue;
                        }
                        break;
                    } else if (type.IsFunctionType()) {
                        ret.Add(function_definition(ident, type.Unwrap() as CType.FunctionType, storageClass, functionSpecifier));
                        return ret;
                    } else {
                        throw new Exception("");
                    }

                }
                lexer.Read(';');
                return ret;
            }

        }

        /// <summary>
        /// 6.9.1�@�֐���`
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <returns></returns>
        /// <remarks>
        /// ����
        /// - �֐���`�Ő錾���鎯�ʎq�i���̊֐��̖��O�j�̌^���֐��^�ł��邱�Ƃ́C���̊֐���`�̐錾�q �̕����Ŏw�肵�Ȃ���΂Ȃ�Ȃ�
        /// - �֐��̕ԋp�l�̌^�́C�z��^�ȊO�̃I�u�W�F�N�g�^���� void �^�łȂ���΂Ȃ�Ȃ��B
        /// - �錾�w��q��̒��ɋL����N���X�w��q������ꍇ�C����� extern ���� static �̂����ꂩ�łȂ���΂Ȃ�Ȃ��B
        /// - �錾�q���������^���т��܂ޏꍇ�C���ꂼ��̉������̐錾�͎��ʎq���܂܂Ȃ���΂Ȃ�Ȃ��B
        ///   �������C�������^���т� void �^�̉�������������琬����ʂȏꍇ�������B���̏ꍇ�́C���ʎq�������Ă͂Ȃ炸�C�X�ɐ錾�q�̌��ɐ錾���т������Ă͂Ȃ�Ȃ��B
        /// </remarks>
        private SyntaxTree.Declaration function_definition(string ident, CType.FunctionType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) {

            // K&R�ɂ�����錾���т�����ꍇ�͓ǂݎ��B
            var argmuents = is_declaration() ? declaration() : null;

            // �錾���т�����ꍇ�͉������錾������
            if (argmuents != null) {
                foreach (var arg in argmuents) {
                    if (!(arg is SyntaxTree.Declaration.VariableDeclaration)) {
                        throw new Exception("�Â��X�^�C���̊֐��錾�ɂ�����錾���ђ��ɉ������錾�ȊO������");
                    }
                    if ((arg as SyntaxTree.Declaration.VariableDeclaration).Init != null) {
                        throw new Exception("�Â��X�^�C���̊֐��錾�ɂ����鉼�����錾�����������������Ă���B");
                    }
                    if ((arg as SyntaxTree.Declaration.VariableDeclaration).StorageClass != StorageClassSpecifier.Register && (arg as SyntaxTree.Declaration.VariableDeclaration).StorageClass != StorageClassSpecifier.None) {
                        throw new Exception("�Â��X�^�C���̊֐��錾�ɂ����鉼�����錾���Aregister �ȊO�̋L���N���X�w��q�𔺂��Ă���B");
                    }
                }
            }

            if (type.Arguments == null) {
                // ���ʎq���сE�������^���тȂ�
                if (argmuents != null) {
                    throw new Exception("K&R�`���̊֐���`�����A���ʎq���т���Ȃ̂ɁA�錾���т�����");
                } else {
                    // ANSI�ɂ�����������Ƃ�Ȃ��֐�(void)
                    // �x�����o�����ƁB
                    type.Arguments = new CType.FunctionType.ArgumentInfo[0];
                }
            } else if (type.Arguments.Any(x => (x.Type as CType.BasicType)?.Kind == CType.BasicType.TypeKind.KAndRImplicitInt)) {

                // ANSI�`���̉��������тƂ̋����͕s�\
                if (type.Arguments.Any(x => (x.Type as CType.BasicType)?.Kind != CType.BasicType.TypeKind.KAndRImplicitInt)) {
                    throw new Exception("�֐���`����K&R�`���̎��ʎq���т�ANSI�`���̉������^���т����݂��Ă���");
                }


                // �W�����O��K��R���łɂ����ẮA�����ɂ͋K��̎������g�����K�p����Achar, short�^��int�ɁAfloat�^�� double�^�Ɋg�������B�܂�A�����Ƃ��ēn���鐮���^��int/�����^��double�̂݁B
                //  -> int f(x,y,z) char x; float y; short* z; {...} �� int f(int x, double y, short *z) { ... } �ɂȂ�(short*�̓|�C���^�^�Ȃ̂Ŋg������Ȃ�)
                //
                // gcc�͔�W���g���Ƃ��Ċ֐��v���g�^�C�v�錾�̌�ɓ����^��K&R�^�̊֐���`���o�ꂷ��ƁA�v���g�^�C�v�錾���g����K&R�̊֐���`������������B�i"info gcc" -> "C Extension" -> "Function Prototypes"�j
                //  -> int f(char, float, short*); �����O�ɂ���� int f(x,y,z) char x; float y; short* z; {...} �� int f(char x, float y, short* z) { ... } �ɂȂ�B�i�v���g�^�C�v�������ꍇ�͏]���ʂ�H�j
                // 
                // �������A
                // K&R�`���̉�������`�̏ꍇ�A�K��̎������g���O��Ō^���H���Ⴄ�����錾�̓G���[�ɂ���B

                // ����킵����̏ꍇ
                // int f();
                // void foo(void) { f(3.14f); }
                // int f (x) floar x; { ... }
                //
                // int f(); �����̏�񂪂Ȃ��֐��̃v���g�^�C�v�Ȃ̂ŁA�������ɂ͋K��̎������g�����K�p����A�����Ƃ��ēn���鐮���^��int/�����^��double�̂݁B
                // f(3.14f); �͈����̏�񂪂Ȃ��֐��̃v���g�^�C�v�Ȃ̂ň����̌^�E���̓`�F�b�N�����A����̎������g���ɂ�������double�^�ɕϊ������B�i�K��̎������g���Ō^���ω�����Ȃ�x�����o�����ق���������ˁj
                // int f(x) float x; {...} �� �K��̎������g���ɂ�� inf f(x) double x; {... }�����ƂȂ�B�i�̂Ōx���o�����ق���������ˁj
                // �Ȃ̂ŁA�S�̂ł݂�ƌ^�̐������͂Ƃ�Ă���B

                // �֐����^���܂܂Ȃ��^�Ŋ֐����`���C���g����̎������̌^���C�g����̉������̌^�ƓK�����Ȃ��ꍇ�C���̓���͖���`�Ƃ���B
                // ��̗��f(1) �Ƃ���ƁA�Ăяo�����͈�����int�^�œn���̂ɁA�󂯎�葤��double�Ŏ󂯎�邽�߃o�O�̉����ɂȂ�B
                // ������������ɂ͌^���_���邵���Ȃ�


                // K&R�`���̎��ʎq���тɐ錾���т̌^�����K��̎������g���𔺂��Ĕ��f������B
                // �錾���т𖼑O�����ł��鎫���ɕϊ�
                var dic = argmuents.Cast<SyntaxTree.Declaration.VariableDeclaration>().ToDictionary(x => x.Ident, x => x);
                // �^�錾���̉�����
                var mapped = type.Arguments.Select(x => {
                    if (dic.ContainsKey(x.Ident)) {
                        var dapType = dic[x.Ident].Type.DefaultArgumentPromotion();
                        if (CType.IsEqual(dapType, dic[x.Ident].Type) == false) {
                            throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}�͋K��̎������g���Ō^���ω����܂��B");
                        }
                        return new CType.FunctionType.ArgumentInfo(x.Ident, x.StorageClass, dic[x.Ident].Type.DefaultArgumentPromotion());
                    } else {
                        return new CType.FunctionType.ArgumentInfo(x.Ident, x.StorageClass, CType.CreateSignedInt().DefaultArgumentPromotion());
                    }
                }).ToList();



                type.Arguments = mapped.ToArray();

            } else {
                // ANSI�`���̉������^���т݂̂Ȃ̂ŉ������Ȃ�
            }

            // �֐�����`�ς݂̏ꍇ�́A�Ē�`�̃`�F�b�N���s��
            IdentifierScopeValue iv;
            if (_identScope.TryGetValue(ident, out iv)) {
                if (iv.IsFunction() == false) {
                    throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}�͊��Ɋ֐��^�ȊO�Ő錾�ς�");
                }
                if ((iv.ToFunction().Ty as CType.FunctionType).Arguments != null) {
                    if (CType.IsEqual(iv.ToFunction().Ty, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, "�Ē�`�^�̕s��v");
                    }
                } else {
                    // ���������ȗ�����Ă��邽�߁A�����̐���^�̓`�F�b�N���Ȃ�
                    Console.WriteLine($"���������ȗ�����Đ錾���ꂽ�֐� {ident} �̎��Ԃ�錾���Ă��܂��B");
                }
                if (iv.ToFunction().Body != null) {
                    throw new Exception("�֐��͂��łɖ{�̂������Ă���B");
                }

            }
            var funcdecl = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, functionSpecifier);

            // ���ɖ��O��ǉ�
            _identScope.Add(ident, new IdentifierScopeValue.Declaration(funcdecl));

            // �e�X�R�[�v��ς�
            _tagScope = _tagScope.Extend();
            _typedefScope = _typedefScope.Extend();
            _identScope = _identScope.Extend();

            if (type.Arguments != null) {
                foreach (var arg in type.Arguments) {
                    _identScope.Add(arg.Ident, new IdentifierScopeValue.Declaration(new SyntaxTree.Declaration.ArgumentDeclaration(arg.Ident, arg.Type, arg.StorageClass)));
                }
            }

            // �֐��{�́i�����j�����
            funcdecl.Body = compound_statement();

            //�e�X�R�[�v����o��
            _identScope = _identScope.Parent;
            _typedefScope = _typedefScope.Parent;
            _tagScope = _tagScope.Parent;

            return funcdecl;
        }

        /// <summary>
        /// 6.9.2�@�O���I�u�W�F�N�g��`�A�������́A�錾
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="functionSpecifier"></param>
        /// <returns></returns>
        private SyntaxTree.Declaration func_or_var_or_typedef_declaration(string ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) {
            SyntaxTree.Declaration decl;

            if (functionSpecifier != FunctionSpecifier.None) {
                throw new Exception("inline�͊֐���`�ɑ΂��Ă̂ݎg����B");
            }
            if (storageClass == StorageClassSpecifier.Auto || storageClass == StorageClassSpecifier.Register) {
                throw new Exception("�錾�ɑ΂��ė��p�ł��Ȃ��L���N���X�w��q���w�肳��Ă���B");
            }


            if (lexer.ReadIf('=')) {
                // ���������𔺂��̂ŁA�������t���̕ϐ��錾

                if (storageClass == StorageClassSpecifier.Typedef || storageClass == StorageClassSpecifier.Auto || storageClass == StorageClassSpecifier.Register) {
                    throw new Exception("�ϐ��錾�ɂ͎w��ł��Ȃ��L���N���X�w��q���w�肳��Ă���B");
                }

                if (type.IsFunctionType()) {
                    throw new Exception("�֐��錾�ɏ����l���w�肵�Ă���");
                }
                if (ident == "tktk") {

                }
                var init = initializer(type);
                decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, init);
                // ���ɏ����l�t���ϐ���ǉ�
                _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
            } else {
                // ���������𔺂�Ȃ����߁A�֐��錾�A�ϐ��錾�ATypedef�錾�̂ǂꂩ

                if (storageClass == StorageClassSpecifier.Auto || storageClass == StorageClassSpecifier.Register) {
                    throw new Exception("�t�@�C���L���͈͂ł̊֐��錾�A�ϐ��錾�ATypedef�錾�Ŏw��ł��Ȃ��L���N���X�w��q���w�肳��Ă���B");
                }

                CType.FunctionType ft;
                if (type.IsFunctionType(out ft) && ft.Arguments != null) {
                    // 6.7.5.3 �֐��錾�q�i�֐����^���܂ށj
                    // �֐���`�̈ꕔ�łȂ��֐��錾�q�ɂ����鎯�ʎq���т́C��łȂ���΂Ȃ�Ȃ��B
                    // �r���@�֐��錾��K&R�̊֐���`�̂悤�� int f(a,b,c); �Ə������Ƃ̓_���Ƃ������ƁBint f(); �Ȃ�OK
                    // K&R �̋L�@�Ő錾���L�q�����ꍇ�A������Type��null
                    // ANSI�̋L�@�Ő錾���L�q�����ꍇ�A������Type�͔�null
                    if (ft.Arguments.Any(x => x.Type == null)) {
                        throw new Exception("�֐���`�̈ꕔ�łȂ��֐��錾�q�ɂ����鎯�ʎq���т́C��łȂ���΂Ȃ�Ȃ��B");
                    }
                }

                if (storageClass == StorageClassSpecifier.Typedef) {
                    // typedef �錾
                    SyntaxTree.Declaration.TypeDeclaration tdecl;
                    bool current;
                    if (_typedefScope.TryGetValue(ident, out tdecl, out current)) {
                        if (current == true) {
                            throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�^���Ē�`���ꂽ�B�i�^�̍Ē�`��C11�ȍ~�̋@�\�B�j");
                        }
                    }
                    tdecl = new SyntaxTree.Declaration.TypeDeclaration(ident, type);
                    decl = tdecl;
                    _typedefScope.Add(ident, tdecl);
                } else if (type.IsFunctionType()) {
                    // �֐��錾
                    IdentifierScopeValue iv;
                    if (_identScope.TryGetValue(ident, out iv)) {
                        if (iv.IsFunction() == false) {
                            throw new Exception("�֐��^�ȊO�Ő錾�ς�");
                        }
                        if (CType.IsEqual(iv.ToFunction().Ty, type) == false) {
                            throw new Exception("�Ē�`�^�̕s��v");
                        }
                        // Todo: �^�̍���
                    }
                    decl = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, functionSpecifier);
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                } else {
                    // �ϐ��錾
                    IdentifierScopeValue iv;
                    if (_identScope.TryGetValue(ident, out iv)) {
                        if (iv.IsVariable() == false) {
                            throw new Exception("�ϐ��^�ȊO�Ő錾�ς�");
                        }
                        if (CType.IsEqual(iv.ToVariable().Type, type) == false) {
                            throw new Exception("�Ē�`�^�̕s��v");
                        }
                        // Todo: �^�̍���
                    }
                    decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, null);
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            }
            return decl;
        }

        /// <summary>
        /// 6.7 �錾�ƂȂ肤�邩�H
        /// </summary>
        /// <returns></returns>
        private bool is_declaration() {
            return is_declaration_specifiers(null, TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7 �錾(�錾)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Declaration> declaration() {

            // �錾�w��q�� 
            StorageClassSpecifier storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            // �������錾�q����
            List<SyntaxTree.Declaration> decls = null;
            if (!lexer.Peek(';')) {
                // ��ȏ�̏������錾�q
                decls = new List<SyntaxTree.Declaration>();
                decls.Add(init_declarator(baseType, storageClass));
                while (lexer.ReadIf(',')) {
                    decls.Add(init_declarator(baseType, storageClass));
                }
            }
            lexer.Read(';');
            return decls;
        }

        /// <summary>
        /// 6.7 �錾(�錾�w��q��ɂȂ肤�邩)
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private bool is_declaration_specifiers(CType type, TypeSpecifier typeSpecifier) {
            return is_declaration_specifier(type, typeSpecifier);
        }

        /// <summary>
        /// 6.7 �錾(�錾�w��q��)
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private CType declaration_specifiers(out StorageClassSpecifier sc) {
            CType baseType = null;
            StorageClassSpecifier storageClass = StorageClassSpecifier.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;

            declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);

            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }

            if (functionSpecifier != FunctionSpecifier.None) {
                throw new Exception("inline�͊֐���`�ł̂ݎg����B");
            }

            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            sc = storageClass;

            baseType = baseType.WrapTypeQualifier(typeQualifier);
            return baseType;
        }

        /// <summary>
        /// 6.7 �錾(�錾�w��q�v�f�ɂȂ肤�邩)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool is_declaration_specifier(CType type, TypeSpecifier typeSpecifier) {
            return (is_storage_class_specifier() ||
                (is_type_specifier() && type == null) ||
                (is_struct_or_union_specifier() && type == null) ||
                (is_enum_specifier() && type == null) ||
                (is_TYPEDEF_NAME() && type == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier() ||
                is_function_specifier());
        }

        /// <summary>
        /// 6.7 �錾(�錾�w��q�v�f)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="typeSpecifier"></param>
        /// <param name="typeQualifier"></param>
        /// <param name="functionSpecifier"></param>
        private void declaration_specifier(ref CType type, ref StorageClassSpecifier storageClass, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier, ref FunctionSpecifier functionSpecifier) {
            if (is_storage_class_specifier()) {
                storageClass = storageClass.Marge(storage_class_specifier());
            } else if (is_type_specifier()) {
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                if (type != null) {
                    throw new Exception("");
                }
                type = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                if (type != null) {
                    throw new Exception("");
                }
                type = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                SyntaxTree.Declaration.TypeDeclaration value;
                if (_typedefScope.TryGetValue(lexer.current_token().Raw, out value) == false) {
                    throw new Exception();
                }
                if (type != null) {
                    if (CType.IsEqual(type, value.Type) == false) {
                        throw new Exception("");
                    }
                }
                type = new CType.TypedefedType(lexer.current_token().Raw, value.Type);
                lexer.next_token();
            } else if (is_type_qualifier()) {
                typeQualifier.Marge(type_qualifier());
            } else if (is_function_specifier()) {
                functionSpecifier.Marge(function_specifier());
            } else {
                throw new Exception("");
            }
        }

        /// <summary>
        /// 6.7 �錾 (�������錾�q�ƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_init_declarator() {
            return is_declarator();
        }

        /// <summary>
        /// 6.7 �錾 (�������錾�q)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <returns></returns>
        private SyntaxTree.Declaration init_declarator(CType type, StorageClassSpecifier storageClass) {
            // �錾�q
            string ident = "";
            List<CType> stack = new List<CType>() { new CType.StubType() };
            declarator(ref ident, stack, 0);
            type = CType.Resolve(type, stack);

            SyntaxTree.Declaration decl;
            if (lexer.ReadIf('=')) {
                // �������q�𔺂��֐��錾

                if (storageClass == StorageClassSpecifier.Typedef) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�������q�𔺂��ϐ��錾�Ɏw�肷�邱�Ƃ��ł��Ȃ��L���N���X�w��q typedef ���w�肳��Ă���B");
                }

                if (type.IsFunctionType()) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�֐��^�����錾�q�ɑ΂��ď������q��ݒ肵�Ă��܂��B");
                }
                if (ident == "tktk") {

                }
                var init = initializer(type);

                // �Đ錾�̊m�F
                IdentifierScopeValue iv;
                bool isCurrent;
                if (_identScope.TryGetValue(ident, out iv, out isCurrent) && isCurrent == true) {
                    if (iv.IsVariable() == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}�͊��ɕϐ��ȊO�Ƃ��Đ錾����Ă��܂��B");
                    }
                    if (CType.IsEqual(iv.ToVariable().Type, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"���ɐ錾����Ă���ϐ�{ident}�ƌ^����v���Ȃ����ߍĐ錾�ł��܂���B");
                    }
                    if (iv.ToVariable().Init != null) {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"�ϐ�{ident}�͊��ɏ������q�𔺂��Đ錾����Ă���B");
                    }
                    iv.ToVariable().Init = init;
                    decl = iv.ToVariable();
                } else {
                    if (iv != null) {
                        // �x���I���O���B�����I
                    }
                    decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, init);
                    // ���ʎq�X�R�[�v�ɕϐ��錾��ǉ�
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            } else if (storageClass == StorageClassSpecifier.Typedef) {
                // �^�錾��

                // �Đ錾�̊m�F
                SyntaxTree.Declaration.TypeDeclaration tdecl;
                bool isCurrent;
                if (_typedefScope.TryGetValue(ident, out tdecl, out isCurrent)) {
                    if (isCurrent) {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"{ident} �͊��Ɍ^�錾���Ƃ��Đ錾����Ă��܂��B");
                    }
                }
                tdecl = new SyntaxTree.Declaration.TypeDeclaration(ident, type);
                decl = tdecl;
                _typedefScope.Add(ident, tdecl);
            } else if (type.IsFunctionType()) {
                // �Đ錾�̊m�F
                IdentifierScopeValue iv;
                if (_identScope.TryGetValue(ident, out iv)) {
                    if (iv.IsFunction() == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}�͊��Ɋ֐��ȊO�Ƃ��Đ錾����Ă��܂��B");
                    }
                    if (CType.IsEqual(iv.ToFunction().Ty, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"���ɐ錾����Ă���֐�{ident}�ƌ^����v���Ȃ����ߍĐ錾�ł��܂���B");
                    }
                    if (storageClass != StorageClassSpecifier.Static && storageClass == StorageClassSpecifier.None && storageClass != StorageClassSpecifier.Extern) {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"�֐��錾�Ɏw�肷�邱�Ƃ��ł��Ȃ��L���N���X�w��q {(storageClass == StorageClassSpecifier.Register ? "register" : storageClass == StorageClassSpecifier.Typedef ? "typedef" : storageClass.ToString())} ���w�肳��Ă���B");
                    }
                    if (storageClass == StorageClassSpecifier.Static && iv.ToFunction().StorageClass == StorageClassSpecifier.Static) {
                        // ���݂��� static �Ȃ̂ōĐ錾�\
                    } else if ((storageClass == StorageClassSpecifier.Extern || storageClass == StorageClassSpecifier.None) &&
                               (iv.ToFunction().StorageClass == StorageClassSpecifier.Extern || iv.ToFunction().StorageClass == StorageClassSpecifier.None)) {
                        // ���݂��� extern �������� �w��Ȃ� �Ȃ̂ōĐ錾�\
                    } else {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"���ɐ錾����Ă���֐�{ident}�ƋL���w��N���X����v���Ȃ����ߍĐ錾�ł��܂���B");
                    }
                    decl = iv.ToFunction();
                } else {
                    decl = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, FunctionSpecifier.None);
                    // ���ʎq�X�R�[�v�Ɋ֐��錾��ǉ�
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            } else {
                if (storageClass == StorageClassSpecifier.Typedef) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�������q�𔺂��ϐ��錾�Ɏw�肷�邱�Ƃ��ł��Ȃ��L���N���X�w��q typedef ���w�肳��Ă���B");
                }

                if (type.IsFunctionType()) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�֐��^�����錾�q�ɑ΂��ď������q��ݒ肵�Ă��܂��B");
                }

                // �Đ錾�̊m�F
                IdentifierScopeValue iv;
                if (_identScope.TryGetValue(ident, out iv)) {
                    if (iv.IsVariable() == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}�͊��ɕϐ��ȊO�Ƃ��Đ錾����Ă��܂��B");
                    }
                    if (CType.IsEqual(iv.ToVariable().Type, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"���ɐ錾����Ă���ϐ�{ident}�ƌ^����v���Ȃ����ߍĐ錾�ł��܂���B");
                    }
                    decl = iv.ToVariable();
                } else {
                    decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, null);
                    // ���ʎq�X�R�[�v�ɕϐ��錾��ǉ�
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            }
            return decl;
        }

        /// <summary>
        /// 6.7.1 �L����N���X�w��q�ɂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_storage_class_specifier() {
            return lexer.Peek(Token.TokenKind.AUTO, Token.TokenKind.REGISTER, Token.TokenKind.STATIC, Token.TokenKind.EXTERN, Token.TokenKind.TYPEDEF);
        }

        /// <summary>
        /// 6.7.1 �L����N���X�w��q
        /// </summary>
        /// <returns></returns>
        private StorageClassSpecifier storage_class_specifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.AUTO:
                    lexer.next_token();
                    return StorageClassSpecifier.Auto;
                case Token.TokenKind.REGISTER:
                    lexer.next_token();
                    return StorageClassSpecifier.Register;
                case Token.TokenKind.STATIC:
                    lexer.next_token();
                    return StorageClassSpecifier.Static;
                case Token.TokenKind.EXTERN:
                    lexer.next_token();
                    return StorageClassSpecifier.Extern;
                case Token.TokenKind.TYPEDEF:
                    lexer.next_token();
                    return StorageClassSpecifier.Typedef;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.2 �^�w��q�ɂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_type_specifier() {
            return lexer.Peek(Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED);
        }

        /// <summary>
        /// �����^�Ɋ��蓖�Ă閼�O�𐶐����邽�߂̃J�E���^�[
        /// </summary>
        private int anony = 0;

        /// <summary>
        /// 6.7.2 �^�w��q
        /// </summary>
        /// <returns></returns>
        private TypeSpecifier type_specifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.VOID:
                    lexer.next_token();
                    return TypeSpecifier.Void;
                case Token.TokenKind.CHAR:
                    lexer.next_token();
                    return TypeSpecifier.Char;
                case Token.TokenKind.INT:
                    lexer.next_token();
                    return TypeSpecifier.Int;
                case Token.TokenKind.FLOAT:
                    lexer.next_token();
                    return TypeSpecifier.Float;
                case Token.TokenKind.DOUBLE:
                    lexer.next_token();
                    return TypeSpecifier.Double;
                case Token.TokenKind.SHORT:
                    lexer.next_token();
                    return TypeSpecifier.Short;
                case Token.TokenKind.LONG:
                    lexer.next_token();
                    return TypeSpecifier.Long;
                case Token.TokenKind.SIGNED:
                    lexer.next_token();
                    return TypeSpecifier.Signed;
                case Token.TokenKind.UNSIGNED:
                    lexer.next_token();
                    return TypeSpecifier.Unsigned;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.2.1 �\���̎w��q�y�ы��p�̎w��q�ɂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_struct_or_union_specifier() {
            return lexer.Peek(Token.TokenKind.STRUCT, Token.TokenKind.UNION);
        }

        /// <summary>
        /// 6.7.2.1 �\���̎w��q�y�ы��p�̎w��q�i�\���̋��p�̎w��q�j
        /// </summary>
        /// <returns></returns>
        private CType struct_or_union_specifier() {
            var kind = lexer.current_token().Kind == Token.TokenKind.STRUCT ? CType.TaggedType.StructUnionType.StructOrUnion.Struct : CType.TaggedType.StructUnionType.StructOrUnion.Union;

            // �\���̋��p��
            lexer.Read(Token.TokenKind.STRUCT, Token.TokenKind.UNION);

            // ���ʎq�̗L���ŕ���
            if (is_IDENTIFIER(true)) {

                var ident = IDENTIFIER(true);

                // �g���ʂ̗L���ŕ���
                if (lexer.ReadIf('{')) {
                    // ���ʎq�𔺂����S�^�̐錾
                    CType.TaggedType tagType;
                    CType.TaggedType.StructUnionType structUnionType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // �^�O���O�\�ɖ����ꍇ�͐V�����ǉ�����B
                        structUnionType = new CType.TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, structUnionType);
                    } else if (!(tagType is CType.TaggedType.StructUnionType)) {
                        throw new Exception($"�\����/���p�� {ident} �͊��ɗ񋓌^�Ƃ��Ē�`����Ă��܂��B");
                    } else if ((tagType as CType.TaggedType.StructUnionType).Kind != kind) {
                        throw new Exception($"�\����/���p�� {ident} �͊��ɒ�`����Ă��܂����A�\����/���p�̂̎�ʂ���v���܂���B");
                    } else if ((tagType as CType.TaggedType.StructUnionType).Members != null) {
                        throw new Exception($"�\����/���p�� {ident} �͊��Ɋ��S�^�Ƃ��Ē�`����Ă��܂��B");
                    } else {
                        // �s���S�^�Ƃ��Ē�`����Ă���̂Ŋ��S�^�ɂ��邽�߂ɏ��������ΏۂƂ���
                        structUnionType = (tagType as CType.TaggedType.StructUnionType);
                    }
                    // �����o�錾���т���͂���
                    structUnionType.Members = struct_declarations();
                    lexer.Read('}');
                    return structUnionType;
                } else {
                    // �s���S�^�̐錾
                    CType.TaggedType tagType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // �^�O���O�\�ɖ����ꍇ�͐V�����ǉ�����B
                        tagType = new CType.TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, tagType);
                    } else if (!(tagType is CType.TaggedType.StructUnionType)) {
                        throw new Exception($"�\����/���p�� {ident} �͊��ɗ񋓌^�Ƃ��Ē�`����Ă��܂��B");
                    } else if ((tagType as CType.TaggedType.StructUnionType).Kind != kind) {
                        throw new Exception($"�\����/���p�� {ident} �͊��ɒ�`����Ă��܂����A�\����/���p�̂̎�ʂ���v���܂���B");
                    } else {
                        // ���ɒ�`����Ă�����̂����S�^�E�s���S�^��킸�������Ȃ��B
                    }
                    return tagType;
                }
            } else {
                // ���ʎq�𔺂�Ȃ������̊��S�^�̐錾

                // ���O�𐶐�
                var ident = $"${kind}_{anony++}";

                // �^���𐶐�����
                var structUnionType = new CType.TaggedType.StructUnionType(kind, ident, true);

                // �^�O���O�\�ɒǉ�����
                _tagScope.Add(ident, structUnionType);

                // �����o�錾���т���͂���
                lexer.Read('{');
                structUnionType.Members = struct_declarations();
                lexer.Read('}');
                return structUnionType;
            }
        }

        /// <summary>
        /// 6.7.2.1 �\���̎w��q�y�ы��p�̎w��q(�����o�錾����)
        /// </summary>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declarations() {
            var items = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            items.AddRange(struct_declaration());
            while (is_struct_declaration()) {
                items.AddRange(struct_declaration());
            }
            return items;
        }

        /// <summary>
        /// 6.7.2.1 �\���̎w��q�y�ы��p�̎w��q(�����o�錾)�ƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_struct_declaration() {
            return is_specifier_qualifiers();
        }

        /// <summary>
        /// 6.7.2.1 �\���̎w��q�y�ы��p�̎w��q(�����o�錾)
        /// </summary>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declaration() {
            CType baseType = specifier_qualifiers();
            var ret = struct_declarator_list(baseType);
            lexer.Read(';');
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 �^�w��q�^�C���q���тƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_specifier_qualifiers() {
            return is_specifier_qualifier(null, TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7.2.1 �^�w��q�^�C���q����
        /// </summary>
        /// <returns></returns>
        private CType specifier_qualifiers() {
            CType baseType = null;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;

            // �^�w��q�������͌^�C���q��ǂݎ��B
            if (is_specifier_qualifier(null, TypeSpecifier.None) == false) {
                if (is_storage_class_specifier()) {
                    // �L���N���X�w��q�i���@��͖����Ă悢�B�G���[���b�Z�[�W�\���̂��߂ɗp�ӁB�j
                    throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"�L���N���X�w��q { lexer.current_token().ToString() } �͎g���܂���B");
                }
                throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, "�^�w��q�������͌^�C���q�ȊO�̗v�f������B");
            }
            specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            while (is_specifier_qualifier(baseType, typeSpecifier)) {
                specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            }

            if (baseType != null) {
                // �^�w��q���ɍ\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`�����o������ꍇ
                if (typeSpecifier != TypeSpecifier.None) {
                    // 6.7.2 �^�w��q�u�^�w��q�̕��т́C���Ɏ������̂̂����ꂩ��łȂ���΂Ȃ�Ȃ��B�v���ō\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`���͂���P�݂̂̂Ŏg�����Ƃ��K�肳��Ă��邽�߁A
                    // �\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`���̂����ꂩ�Ƃ����ȊO�̌^�w��q���g�ݍ��킹���Ă���ꍇ�̓G���[�Ƃ���B
                    // �Ȃ��A�\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`���������񗘗p����Ă���ꍇ�� specifier_qualifier ���ŃG���[�ƂȂ�B
                    // �i���j�I�Șb�FK&R �ł� typedef �� �ʖ�(alias)�������������߁Atypedef int INT; unsingned INT x; �͑Ó��������j
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�^�w��q�E�^�C���q���ђ��ō\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`���̂����ꂩ�ƁA�����ȊO�̌^�w��q���g�ݍ��킹���Ă���B");
                }
            } else {
                // �^�w��q���ɍ\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`�����o�����Ȃ��ꍇ
                if (typeSpecifier == TypeSpecifier.None) {
                    // 6.7.2 ���ꂼ��̐錾�̐錾�w��q��̒��ŁC���͂��ꂼ��̍\���̐錾�y�ь^���̌^�w��q�^�C���q���т̒��ŁC���Ȃ��Ƃ���̌^�w��q���w�肵�Ȃ���΂Ȃ�Ȃ��B
                    // �Ƃ��邽�߁A�錾�w��q������w�肵�Ȃ����Ƃ͋�����Ȃ��B
                    // �i���j�I�Șb�FK&R �ł� �錾�w��q���ȗ������ int �����j
                    // ToDo: C90�͌݊����̊ϓ_����K&R������c����Ă���̂őI���ł���悤�ɂ���
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "���ꂼ��̐錾�̐錾�w��q��̒��ŁC���͂��ꂼ��̍\���̐錾�y�ь^���̌^�w��q�^�C���q���т̒��ŁC���Ȃ��Ƃ���̌^�w��q���w�肵�Ȃ���΂Ȃ�Ȃ��B");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            }

            // �^�C���q��K�p
            baseType = baseType.WrapTypeQualifier(typeQualifier);

            return baseType;
        }

        /// <summary>
        /// 6.7.2.1 �^�w��q�^�C���q���сi�^�w��q�������͌^�C���q�ƂȂ肤�邩�H�j
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool is_specifier_qualifier(CType type, TypeSpecifier typeSpecifier) {
            return (
                (is_type_specifier() && type == null) ||
                (is_struct_or_union_specifier() && type == null) ||
                (is_enum_specifier() && type == null) ||
                (is_TYPEDEF_NAME() && type == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier());
        }

        /// <summary>
        /// 6.7.2.1 �^�w��q�^�C���q���сi�^�w��q�������͌^�C���q�j
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private void specifier_qualifier(ref CType type, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier) {
            if (is_type_specifier()) {
                // �^�w��q�i�\���j
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                // �^�w��q�i�\���̎w��q�������͋��p�̎w��q�j
                if (type != null) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�^�w��q�E�^�C���q���ђ��ō\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`�����Q�ȏ�g�p����Ă���B");
                }
                type = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                // �^�w��q�i�񋓌^�w��q�j
                if (type != null) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�^�w��q�E�^�C���q���ђ��ō\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`�����Q�ȏ�g�p����Ă���B");
                }
                type = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                // �^�w��q�i�^��`���j
                SyntaxTree.Declaration.TypeDeclaration value;
                if (_typedefScope.TryGetValue(lexer.current_token().Raw, out value) == false) {
                    throw new CompilerException.UndefinedIdentifierErrorException(lexer.current_token().Start, lexer.current_token().End, $"�^�� {lexer.current_token().Raw} �͒�`����Ă��܂���B");
                }
                if (type != null) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "�^�w��q�E�^�C���q���ђ��ō\���̋��p�̎w��q�A�񋓌^�w��q�A�^��`�����Q�ȏ�g�p����Ă���B");
                }
                type = new CType.TypedefedType(lexer.current_token().Raw, value.Type);
                lexer.next_token();
            } else if (is_type_qualifier()) {
                // �^�C���q
                typeQualifier.Marge(type_qualifier());
            } else {
                throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"�^�w��q�^�C���q�� �^�w��q�̗\���, �\���̎w��q�������͋��p�̎w��q, �񋓌^�w��q, �^��`�� �^�C���q�̉��ꂩ�ł����A { lexer.current_token().ToString() } �͂��̂�����ł�����܂���B�i�{�����n�̎����Ɍ�肪����Ǝv���܂��B�j");
            }
        }

        /// <summary>
        /// 6.7.2.1 �^�w��q�^�C���q���сi�����o�錾�q���сj
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declarator_list(CType type) {
            var ret = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            ret.Add(struct_declarator(type));
            while (lexer.ReadIf(',')) {
                ret.Add(struct_declarator(type));
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 �^�w��q�^�C���q���сi�����o�錾�q�j
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private CType.TaggedType.StructUnionType.MemberInfo struct_declarator(CType type) {
            string ident = null;
            if (is_declarator()) {
                // �錾�q
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator(ref ident, stack, 0);
                type = CType.Resolve(type, stack);

                // �r�b�g�t�B�[���h����(opt)
                SyntaxTree.Expression expr = null;
                if (lexer.ReadIf(':')) {
                    expr = constant_expression();
                }

                return new CType.TaggedType.StructUnionType.MemberInfo(ident, type, expr == null ? (int?)null : Evaluator.ConstantEval(expr));
            } else if (lexer.ReadIf(':')) {
                // �r�b�g�t�B�[���h����(must)
                SyntaxTree.Expression expr = constant_expression();

                return new CType.TaggedType.StructUnionType.MemberInfo(ident, type, expr == null ? (int?)null : Evaluator.ConstantEval(expr));
            } else {
                throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"�\����/���p�̂̃����o�錾�q�ł́A�錾�q�ƃr�b�g�t�B�[���h���̗������ȗ����邱�Ƃ͂ł��܂���B�����\����/���p�̂��g�p�ł���̂͋K�i���C11����ł��B(C11 6.7.2.1�ŋK��)�B");
            }
        }

        /// <summary>
        /// 6.7.2.2 �񋓌^�w��q�ƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_enum_specifier() {
            return lexer.Peek(Token.TokenKind.ENUM);
        }

        /// <summary>
        /// 6.7.2.2 �񋓌^�w��q
        /// </summary>
        /// <returns></returns>
        private CType enum_specifier() {
            lexer.Read(Token.TokenKind.ENUM);

            if (is_IDENTIFIER(true)) {
                var ident = IDENTIFIER(true);
                CType.TaggedType etype;
                if (_tagScope.TryGetValue(ident, out etype) == false) {
                    // �^�O���O�\�ɖ����ꍇ�͐V�����ǉ�����B
                    etype = new CType.TaggedType.EnumType(ident, false);
                    _tagScope.Add(ident, etype);
                } else if (!(etype is CType.TaggedType.EnumType)) {
                    throw new Exception($"�񋓌^ {ident} �͊��ɍ\����/���p�̂Ƃ��Ē�`����Ă��܂��B");
                } else {

                }
                if (lexer.ReadIf('{')) {
                    if ((etype as CType.TaggedType.EnumType).Members != null) {
                        throw new Exception($"�񋓌^ {ident} �͊��Ɋ��S�^�Ƃ��Ē�`����Ă��܂��B");
                    } else {
                        // �s���S�^�Ƃ��Ē�`����Ă���̂Ŋ��S�^�ɂ��邽�߂ɏ��������ΏۂƂ���
                        (etype as CType.TaggedType.EnumType).Members = enumerator_list(etype as CType.TaggedType.EnumType);
                        lexer.Read('}');
                    }
                }
                return etype;
            } else {
                var ident = $"$enum_{anony++}";
                var etype = new CType.TaggedType.EnumType(ident, true);
                _tagScope.Add(ident, etype);
                lexer.Read('{');
                enumerator_list(etype);
                lexer.Read('}');
                return etype;
            }
        }

        /// <summary>
        /// 6.7.2.2 �񋓌^�w��q�i�񋓎q���сj
        /// </summary>
        /// <param name="enumType"></param>
        private List<CType.TaggedType.EnumType.MemberInfo> enumerator_list(CType.TaggedType.EnumType enumType) {
            var ret = new List<CType.TaggedType.EnumType.MemberInfo>();
            enumType.Members = ret;
            var e = enumerator(enumType, 0);
            _identScope.Add(e.Ident, new IdentifierScopeValue.EnumValue(enumType, e.Ident));
            ret.Add(e);
            while (lexer.ReadIf(',')) {
                var i = e.Value + 1;
                if (is_enumerator() == false) {
                    break;
                }
                e = enumerator(enumType, i);
                _identScope.Add(e.Ident, new IdentifierScopeValue.EnumValue(enumType, e.Ident));
                ret.Add(e);
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.2 �񋓌^�w��q�i�񋓎q�ƂȂ肤�邩�j
        /// </summary>
        /// <returns></returns>
        private bool is_enumerator() {
            return is_IDENTIFIER(false);
        }

        /// <summary>
        /// 6.7.2.2 �񋓌^�w��q�i�񋓎q�j
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private CType.TaggedType.EnumType.MemberInfo enumerator(CType.TaggedType.EnumType enumType, int i) {
            var ident = IDENTIFIER(false);
            if (lexer.ReadIf('=')) {
                var expr = constant_expression();
                i = Evaluator.ConstantEval(expr);
            }
            return new CType.TaggedType.EnumType.MemberInfo(enumType, ident, i);
        }

        /// <summary>
        /// 6.7.3 �^�C���q�ƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_type_qualifier() {
            return lexer.Peek(Token.TokenKind.CONST, Token.TokenKind.VOLATILE, Token.TokenKind.RESTRICT, Token.TokenKind.NEAR, Token.TokenKind.FAR);
        }

        /// <summary>
        /// 6.7.3 �^�C���q
        /// </summary>
        /// <returns></returns>
        private TypeQualifier type_qualifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.CONST:
                    lexer.next_token();
                    return TypeQualifier.Const;
                case Token.TokenKind.VOLATILE:
                    lexer.next_token();
                    return TypeQualifier.Volatile;
                case Token.TokenKind.RESTRICT:
                    lexer.next_token();
                    return TypeQualifier.Restrict;
                case Token.TokenKind.NEAR:
                    lexer.next_token();
                    return TypeQualifier.Near;
                case Token.TokenKind.FAR:
                    lexer.next_token();
                    return TypeQualifier.Far;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.4 �֐��w��q�ƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_function_specifier() {
            return lexer.Peek(Token.TokenKind.INLINE);
        }

        /// <summary>
        /// 6.7.4 �֐��w��q
        /// </summary>
        /// <returns></returns>
        private FunctionSpecifier function_specifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.INLINE:
                    lexer.next_token();
                    return FunctionSpecifier.Inline;
                default:
                    throw new Exception();
            }
        }

        private bool is_TYPEDEF_NAME() {
            //return lexer.current_token().Kind == Token.TokenKind.TYPE_NAME;
            return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER && _typedefScope.ContainsKey(lexer.current_token().Raw);
        }

        /// <summary>
        /// 6.7.5 �錾�q�ƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_declarator() {
            return is_pointer() || is_direct_declarator();
        }

        /// <summary>
        /// 6.7.5 �錾�q
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
            }
            direct_declarator(ref ident, stack, index);
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ڐ錾�q�ƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_direct_declarator() {
            return lexer.Peek('(') || is_IDENTIFIER(true);
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ڐ錾�q�̑O������)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void direct_declarator(ref string ident, List<CType> stack, int index) {
            if (lexer.ReadIf('(')) {
                stack.Add(new CType.StubType());
                declarator(ref ident, stack, index + 1);
                lexer.Read(')');
            } else {
                ident = lexer.current_token().Raw;
                lexer.next_token();
            }
            more_direct_declarator(stack, index);
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ڐ錾�q�̌㔼����)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void more_direct_declarator(List<CType> stack, int index) {
            if (lexer.ReadIf('[')) {
                // 6.7.5.2 �z��錾�q
                // ToDo: AnsiC�͈͂̂ݑΉ�
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_direct_declarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (lexer.ReadIf('(')) {
                // 6.7.5.3 �֐��錾�q�i�֐����^���܂ށj
                if (lexer.ReadIf(')')) {
                    // k&r or ANSI empty parameter list
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else if (is_identifier_list()) {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => new CType.FunctionType.ArgumentInfo(x, StorageClassSpecifier.None, (CType)new CType.BasicType(TypeSpecifier.None))).ToList();
                    lexer.Read(')');
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else {
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    lexer.Read(')');
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                    more_direct_declarator(stack, index);

                }
            } else {
                //_epsilon_
            }
        }

        /// <summary>
        /// 6.7.5 �錾�q(�������^���тƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_parameter_type_list() {
            return is_parameter_declaration();
        }

        /// <summary>
        /// 6.7.5 �錾�q(�������^����)
        /// </summary>
        /// <returns></returns>
        private List<CType.FunctionType.ArgumentInfo> parameter_type_list(ref bool vargs) {
            var items = new List<CType.FunctionType.ArgumentInfo>();
            items.Add(parameter_declaration());
            while (lexer.ReadIf(',')) {
                if (lexer.ReadIf(Token.TokenKind.ELLIPSIS)) {
                    vargs = true;
                    break;
                } else {
                    items.Add(parameter_declaration());
                }
            }
            return items;
        }

        /// <summary>
        /// 6.7.5 �錾�q(���������тƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        public bool is_parameter_declaration() {
            return is_declaration_specifier(null, TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7.5 �錾�q(����������)
        /// </summary>
        /// <returns></returns>
        private CType.FunctionType.ArgumentInfo parameter_declaration() {
            StorageClassSpecifier storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            if (is_declarator_or_abstract_declarator()) {
                string ident = "";
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator_or_abstract_declarator(ref ident, stack, 0);
                var type = CType.Resolve(baseType, stack);
                return new CType.FunctionType.ArgumentInfo(ident, storageClass, type);
            } else {
                return new CType.FunctionType.ArgumentInfo((string)null, storageClass, baseType);
            }

        }

        /// <summary>
        /// 6.7.5 �錾�q(�錾�q�������͒��ې錾�q�ƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_declarator_or_abstract_declarator() {
            return is_pointer() || is_direct_declarator_or_direct_abstract_declarator();
        }

        /// <summary>
        /// 6.7.5 �錾�q(�錾�q�������͒��ې錾�q)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void declarator_or_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_declarator_or_direct_abstract_declarator()) {
                    direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
                }
            } else {
                direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
            }
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ڐ錾�q�������͒��ڒ��ې錾�q�ƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_direct_declarator_or_direct_abstract_declarator() {
            return is_IDENTIFIER(true) || lexer.Peek('(', '[');
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ڐ錾�q�������͒��ڒ��ې錾�q�̑O��)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void direct_declarator_or_direct_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_IDENTIFIER(true)) {
                ident = IDENTIFIER(true);
                more_dd_or_dad(stack, index);
            } else if (lexer.ReadIf('(')) {
                if (lexer.Peek(')')) {
                    // function?
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                } else {
                    stack.Add(new CType.StubType());
                    declarator_or_abstract_declarator(ref ident, stack, index + 1);
                }
                lexer.Read(')');
                more_dd_or_dad(stack, index);
            } else if (lexer.ReadIf('[')) {
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_dd_or_dad(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else {
                throw new Exception();
            }

        }

        /// <summary>
        /// 6.7.5 �錾�q(���ڐ錾�q�������͒��ڒ��ې錾�q�̌㔼)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void more_dd_or_dad(List<CType> stack, int index) {
            if (lexer.ReadIf('(')) {
                if (lexer.Peek(')')) {
                    // function?
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => new CType.FunctionType.ArgumentInfo(x, StorageClassSpecifier.None, (CType)new CType.BasicType(TypeSpecifier.None))).ToList();
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                }
                lexer.Read(')');
                more_dd_or_dad(stack, index);
            } else if (lexer.ReadIf('[')) {
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_dd_or_dad(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else {
                // _epsilon_
            }
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ʎq���тƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_identifier_list() {
            return is_IDENTIFIER(false);
        }

        /// <summary>
        /// 6.7.5 �錾�q(���ʎq����)
        /// </summary>
        /// <returns></returns>
        private List<string> identifier_list() {
            var items = new List<string>();
            items.Add(IDENTIFIER(false));
            while (lexer.ReadIf(',')) {
                items.Add(IDENTIFIER(false));
            }
            return items;
        }

        /// <summary>
        /// 6.7.5.1 �|�C���^�錾�q�ƂȂ肤�邩
        /// </summary>
        /// <returns></returns>
        private bool is_pointer() {
            return lexer.Peek('*');
        }

        /// <summary>
        /// 6.7.5.1 �|�C���^�錾�q
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void pointer(List<CType> stack, int index) {
            lexer.Read('*');
            stack[index] = CType.CreatePointer(stack[index]);
            TypeQualifier typeQualifier = TypeQualifier.None;
            while (is_type_qualifier()) {
                typeQualifier = typeQualifier.Marge(type_qualifier());
            }
            stack[index] = stack[index].WrapTypeQualifier(typeQualifier);

            if (is_pointer()) {
                pointer(stack, index);
            }
        }

        /// <summary>
        /// 6.7.6 �^��(�^��)�ƂȂ肤�邩�H
        /// </summary>
        /// <returns></returns>
        private bool is_type_name() {
            return is_specifier_qualifiers();
        }

        /// <summary>
        /// 6.7.6 �^��(�^��)
        /// </summary>
        /// <returns></returns>
        private CType type_name() {
            CType baseType = specifier_qualifiers();
            if (is_abstract_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                abstract_declarator(stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            return baseType;
        }

        /// <summary>
        /// 6.7.6 �^��(���ې錾�q)�ƂȂ肤�邩�H
        /// </summary>
        /// <returns></returns>
        private bool is_abstract_declarator() {
            return (is_pointer() || is_direct_abstract_declarator());
        }

        /// <summary>
        /// 6.7.6 �^��(���ې錾�q)
        /// </summary>
        /// <returns></returns>
        private void abstract_declarator(List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_abstract_declarator()) {
                    direct_abstract_declarator(stack, index);
                }
            } else {
                direct_abstract_declarator(stack, index);
            }
        }

        /// <summary>
        /// 6.7.6 �^��(���ڒ��ې錾�q���\������O���̗v�f)�ƂȂ肤�邩�H
        /// </summary>
        /// <returns></returns>
        private bool is_direct_abstract_declarator() {
            return lexer.Peek('(', '[');
        }

        /// <summary>
        /// 6.7.6 �^��(���ڒ��ې錾�q���\������O���̗v�f)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void direct_abstract_declarator(List<CType> stack, int index) {
            if (lexer.ReadIf('(')) {
                if (is_abstract_declarator()) {
                    stack.Add(new CType.StubType());
                    abstract_declarator(stack, index + 1);
                } else if (lexer.Peek(')') == false) {
                    // ansi args
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                } else {
                    // k&r or ansi
                }
                lexer.Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                lexer.Read('[');
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_direct_abstract_declarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            }
        }

        /// <summary>
        /// 6.7.6 �^��(���ڒ��ې錾�q���\������㔼�̗v�f)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void more_direct_abstract_declarator(List<CType> stack, int index) {
            if (lexer.ReadIf('[')) {
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_direct_abstract_declarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (lexer.ReadIf('(')) {
                if (lexer.Peek(')') == false) {
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(items, vargs, stack[index]);
                } else {
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                lexer.Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                // _epsilon_
            }
        }

        /// <summary>
        /// 6.7.7 �^��`(�^��`��)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool is_typedefed_type(string v) {
            return _typedefScope.ContainsKey(v);
        }

        private void CheckInitializerExpression(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType()) {
                // �z��^�̏ꍇ
                var arrayType = type.Unwrap() as CType.ArrayType;
                CheckInitializerArray(arrayType, ast);
                return;
            } else if (type.IsStructureType()) {
                // �\���̌^�̏ꍇ
                var arrayType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerStruct(arrayType, ast);
                return;
            } else if (type.IsUnionType()) {
                // ���p�̌^�̏ꍇ
                var arrayType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerUnion(arrayType, ast);
                return;
            } else {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "�W���̌^���͋��p�̌^�����I�u�W�F�N�g�ɑ΂��鏉�����q�́C�v�f���͖��O�t�������o�ɑ΂��鏉�����q���т�g���ʂň͂񂾂��̂łȂ���΂Ȃ�Ȃ��B");
                }
                // ������𐶐����Č���
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // ��O���N���Ȃ��Ȃ����ł���
            }
        }

        private void CheckInitializerArray(CType.ArrayType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "�W���̌^���͋��p�̌^�����I�u�W�F�N�g�ɑ΂��鏉�����q�́C�v�f���͖��O�t�������o�ɑ΂��鏉�����q���т�g���ʂň͂񂾂��̂łȂ���΂Ȃ�Ȃ��B");
                }
                // ������𐶐����Č���
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // ��O���N���Ȃ��Ȃ����ł���
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                if (type.Length == -1) {
                    type.Length = inits.Count;
                } else if (type.Length < inits.Count) {
                    throw new Exception("�v�f�����Ⴄ");
                }
                // �v�f������
                for (var i = 0; type.Length == -1 || i < inits.Count; i++) {
                    CheckInitializerExpression(type.BaseType, inits[i]);
                }
            }
        }
        private void CheckInitializerStruct(CType.TaggedType.StructUnionType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "�W���̌^���͋��p�̌^�����I�u�W�F�N�g�ɑ΂��鏉�����q�́C�v�f���͖��O�t�������o�ɑ΂��鏉�����q���т�g���ʂň͂񂾂��̂łȂ���΂Ȃ�Ȃ��B");
                }
                // ������𐶐����Č���
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // ��O���N���Ȃ��Ȃ����ł���
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                if (type.Members.Count < inits.Count) {
                    throw new Exception("�v�f�����Ⴄ");
                }
                // �v�f������
                // Todo: �r�b�g�t�B�[���h
                for (var i = 0; i < inits.Count; i++) {
                    CheckInitializerExpression(type.Members[i].Type, inits[i]);
                }
            } else {
                throw new Exception("");
            }
        }
        private void CheckInitializerUnion(CType.TaggedType.StructUnionType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "�W���̌^���͋��p�̌^�����I�u�W�F�N�g�ɑ΂��鏉�����q�́C�v�f���͖��O�t�������o�ɑ΂��鏉�����q���т�g���ʂň͂񂾂��̂łȂ���΂Ȃ�Ȃ��B");
                }
                // ������𐶐����Č���
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // ��O���N���Ȃ��Ȃ����ł���
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                // �ŏ��̗v�f�Ƃ̂݃`�F�b�N
                CheckInitializerExpression(type.Members[0].Type, ast);
            } else {
                throw new Exception("");
            }
        }
        private void CheckInitializerList(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType()) {
                // �z��^�̏ꍇ
                var arrayType = type.Unwrap() as CType.ArrayType;

                if (arrayType.BaseType.IsCharacterType()) {
                    // ��������̌^�������z��̏ꍇ

                    if (ast is SyntaxTree.Initializer.SimpleInitializer && (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                        // �����񎮂ŏ�����
                        var strExpr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                        if (arrayType.Length == -1) {
                            arrayType.Length = string.Concat(strExpr.Strings).Length + 1;
                        }
                        return;
                    } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                        // �g���ʂŊ���ꂽ������ŏ�����
                        if ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret?.Count == 1
                            && ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret[0] as SyntaxTree.Initializer.SimpleInitializer)?.AssignmentExpression is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                            var strExpr = ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret[0] as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                            if (arrayType.Length == -1) {
                                arrayType.Length = string.Concat(strExpr.Strings).Length + 1;
                            }
                            return;
                        }
                    }
                }
                CheckInitializerArray(arrayType, ast);
                return;
            } else if (type.IsStructureType()) {
                // �\���̌^�̏ꍇ
                var suType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerStruct(suType, ast);
                return;
            } else if (type.IsUnionType()) {
                // ���p�̌^�̏ꍇ
                var suType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerUnion(suType, ast);
                return;
            } else {
                throw new Exception();
            }
        }

        private void CheckInitializer(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType() || type.IsStructureType() || type.IsUnionType()) {
                CheckInitializerList(type, ast);
            } else {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "�W���̌^���͋��p�̌^�����I�u�W�F�N�g�ɑ΂��鏉�����q�́C�v�f���͖��O�t�������o�ɑ΂��鏉�����q���т�g���ʂň͂񂾂��̂łȂ���΂Ȃ�Ȃ��B");
                }
                // ������𐶐����Č���
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // ��O���N���Ȃ��Ȃ����ł���
            }
        }

        /// <summary>
        /// 6.7.8 ������(�������q)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Initializer initializer(CType type) {
            var init = initializer_();
            CheckInitializer(type, init);
            return init;
        }
        private SyntaxTree.Initializer initializer_() {
            if (lexer.ReadIf('{')) {
                List<SyntaxTree.Initializer> ret = null;
                if (lexer.Peek('}') == false) {
                    ret = initializer_list();
                }
                lexer.Read('}');
                return new SyntaxTree.Initializer.ComplexInitializer(ret);
            } else {
                return new SyntaxTree.Initializer.SimpleInitializer(assignment_expression());
            }
        }

        /// <summary>
        /// 6.7.8 ������(�������q����)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Initializer> initializer_list() {
            var ret = new List<SyntaxTree.Initializer>();
            ret.Add(initializer_());
            while (lexer.ReadIf(',')) {
                if (lexer.Peek('}')) {
                    break;
                }
                ret.Add(initializer_());
            }
            return ret;
        }

        private bool is_IDENTIFIER(bool include_type_name) {
            // return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER || (include_type_name && lexer.current_token().Kind == Token.TokenKind.TYPE_NAME);
            if (include_type_name) {
                return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER;
            } else {
                return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER && !_typedefScope.ContainsKey(lexer.current_token().Raw);

            }
        }

        private string IDENTIFIER(bool include_type_name) {
            if (is_IDENTIFIER(include_type_name) == false) {
                throw new Exception();
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        /// <summary>
        /// 6.8 ���y�уu���b�N
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement statement() {
            if ((is_IDENTIFIER(true) && lexer.is_nexttoken(':')) || lexer.Peek(Token.TokenKind.CASE, Token.TokenKind.DEFAULT)) {
                return labeled_statement();
            } else if (lexer.Peek('{')) {
                return compound_statement();
            } else if (lexer.Peek(Token.TokenKind.IF, Token.TokenKind.SWITCH)) {
                return selection_statement();
            } else if (lexer.Peek(Token.TokenKind.WHILE, Token.TokenKind.DO, Token.TokenKind.FOR)) {
                return iteration_statement();
            } else if (lexer.Peek(Token.TokenKind.GOTO, Token.TokenKind.CONTINUE, Token.TokenKind.BREAK, Token.TokenKind.RETURN)) {
                return jump_statement();
            } else if (lexer.Peek(Token.TokenKind.__ASM__)) {
                return gnu_asm_statement();
            } else {
                return expression_statement();
            }

        }

        /// <summary>
        /// 6.8.1 ���x���t����
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement labeled_statement() {
            if (lexer.ReadIf(Token.TokenKind.CASE)) {
                var expr = constant_expression();
                lexer.Read(':');
                var stmt = statement();
                return new SyntaxTree.Statement.CaseStatement(expr, stmt);
            } else if (lexer.ReadIf(Token.TokenKind.DEFAULT)) {
                lexer.Read(':');
                var stmt = statement();
                return new SyntaxTree.Statement.DefaultStatement(stmt);
            } else {
                var ident = IDENTIFIER(true);
                lexer.Read(':');
                var stmt = statement();
                return new SyntaxTree.Statement.GenericLabeledStatement(ident, stmt);
            }
        }

        /// <summary>
        /// 6.8.2 ������
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement compound_statement() {
            _tagScope = _tagScope.Extend();
            _typedefScope = _typedefScope.Extend();
            _identScope = _identScope.Extend();
            lexer.Read('{');
            var decls = new List<SyntaxTree.Declaration>();
            while (is_declaration()) {
                var d = declaration();
                if (d != null) {
                    decls.AddRange(d);
                }
            }
            var stmts = new List<SyntaxTree.Statement>();
            while (lexer.Peek('}') == false) {
                stmts.Add(statement());
            }
            lexer.Read('}');
            var stmt = new SyntaxTree.Statement.CompoundStatement(decls, stmts, _tagScope, _identScope);
            _identScope = _identScope.Parent;
            _typedefScope = _typedefScope.Parent;
            _tagScope = _tagScope.Parent;
            return stmt;

        }

        /// <summary>
        /// 6.8.3 �����y�ы�
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement expression_statement() {
            SyntaxTree.Statement ret;
            if (!lexer.Peek(';')) {
                var expr = expression();
                ret = new SyntaxTree.Statement.ExpressionStatement(expr);
            } else {
                ret = new SyntaxTree.Statement.EmptyStatement();
            }
            lexer.Read(';');
            return ret;
        }

        /// <summary>
        /// 6.8.4 �I��
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement selection_statement() {
            if (lexer.ReadIf(Token.TokenKind.IF)) {
                lexer.Read('(');
                var cond = expression();
                lexer.Read(')');
                var then_stmt = statement();
                SyntaxTree.Statement else_stmt = null;
                if (lexer.ReadIf(Token.TokenKind.ELSE)) {
                    else_stmt = statement();
                }
                return new SyntaxTree.Statement.IfStatement(cond, then_stmt, else_stmt);
            }
            if (lexer.ReadIf(Token.TokenKind.SWITCH)) {
                lexer.Read('(');
                var cond = expression();
                lexer.Read(')');
                var ss = new SyntaxTree.Statement.SwitchStatement(cond);
                _breakScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                return ss;
            }
            throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"�I�𕶂� if, switch �̉��ꂩ�Ŏn�܂�܂����A { lexer.current_token().ToString() } �͂��̂�����ł�����܂���B�i�{�����n�̎����Ɍ�肪����Ǝv���܂��B�j");
        }

        /// <summary>
        /// 6.8.5 �J�Ԃ���
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement iteration_statement() {
            if (lexer.ReadIf(Token.TokenKind.WHILE)) {
                lexer.Read('(');
                var cond = expression();
                lexer.Read(')');
                var ss = new SyntaxTree.Statement.WhileStatement(cond);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                _continueScope.Pop();
                return ss;
            }
            if (lexer.ReadIf(Token.TokenKind.DO)) {
                var ss = new SyntaxTree.Statement.DoWhileStatement();
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                _continueScope.Pop();
                lexer.Read(Token.TokenKind.WHILE);
                lexer.Read('(');
                ss.Cond = expression();
                lexer.Read(')');
                lexer.Read(';');
                return ss;
            }
            if (lexer.ReadIf(Token.TokenKind.FOR)) {
                lexer.Read('(');

                var init = lexer.Peek(';') ? (SyntaxTree.Expression)null : expression();
                lexer.Read(';');
                var cond = lexer.Peek(';') ? (SyntaxTree.Expression)null : expression();
                lexer.Read(';');
                var update = lexer.Peek(')') ? (SyntaxTree.Expression)null : expression();
                lexer.Read(')');
                var ss = new SyntaxTree.Statement.ForStatement(init, cond, update);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                _continueScope.Pop();
                return ss;
            }
            throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"�J�Ԃ����� while, do, for �̉��ꂩ�Ŏn�܂�܂����A { lexer.current_token().ToString() } �͂��̂�����ł�����܂���B�i�{�����n�̎����Ɍ�肪����Ǝv���܂��B�j");
        }

        /// <summary>
        ///  6.8.6 ����
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement jump_statement() {
            if (lexer.ReadIf(Token.TokenKind.GOTO)) {
                var label = IDENTIFIER(true);
                lexer.Read(';');
                return new SyntaxTree.Statement.GotoStatement(label);
            }
            if (lexer.ReadIf(Token.TokenKind.CONTINUE)) {
                lexer.Read(';');
                return new SyntaxTree.Statement.ContinueStatement(_continueScope.Peek());
            }
            if (lexer.ReadIf(Token.TokenKind.BREAK)) {
                lexer.Read(';');
                return new SyntaxTree.Statement.BreakStatement(_breakScope.Peek());
            }
            if (lexer.ReadIf(Token.TokenKind.RETURN)) {
                var expr = lexer.Peek(';') ? null : expression();
                //���݂̊֐��̖߂�l�ƌ^�`�F�b�N
                lexer.Read(';');
                return new SyntaxTree.Statement.ReturnStatement(expr);
            }
            throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"���򕶂� goto, continue, break, return �̉��ꂩ�Ŏn�܂�܂����A { lexer.current_token().ToString() } �͂��̂�����ł�����܂���B�i�{�����n�̎����Ɍ�肪����Ǝv���܂��B�j");
        }

        /// <summary>
        /// X.X.X GCC�g���C�����C���A�Z���u��
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement gnu_asm_statement() {
            Console.Error.WriteLine("GCC�g���C�����C���A�Z���u���\���ɂ͑Ή����Ă��܂���B��������Ɠǂݔ�΂��܂��B");

            lexer.Read(Token.TokenKind.__ASM__);
            lexer.ReadIf(Token.TokenKind.__VOLATILE__);
            lexer.Read('(');
            Stack<char> parens = new Stack<char>();
            parens.Push('(');
            while (parens.Any()) {
                if (lexer.Peek('(', '[')) {
                    parens.Push((char)lexer.current_token().Kind);
                } else if (lexer.Peek(')')) {
                    if (parens.Peek() == '(') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"GCC�g���C�����C���A�Z���u���\������ �ۊ��ʕ� ) ���g�p����Ă��܂����A�Ή�����ۊ��ʊJ�� ( ������܂���B");
                    }
                } else if (lexer.Peek(']')) {
                    if (parens.Peek() == '[') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"GCC�g���C�����C���A�Z���u���\������ �p���ʕ� ] ���g�p����Ă��܂����A�Ή�����p���ʊJ�� [ ������܂���B");
                    }
                }
                lexer.next_token();
            }
            lexer.Read(';');
            return new SyntaxTree.Statement.EmptyStatement();
            ;
        }

        /// <summary>
        /// 6.5 ��
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression expression() {
            var e = assignment_expression();
            if (lexer.Peek(',')) {
                var ce = new SyntaxTree.Expression.CommaExpression();
                ce.expressions.Add(e);
                while (lexer.ReadIf(',')) {
                    e = assignment_expression();
                    ce.expressions.Add(e);
                }
                return ce;
            } else {
                return e;
            }
        }

        /// <summary>
        /// 6.5.1 �ꎟ��
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression primary_expression() {
            if (is_IDENTIFIER(false)) {
                var ident = IDENTIFIER(false);
                IdentifierScopeValue value;
                if (_identScope.TryGetValue(ident, out value) == false) {
                    Console.Error.WriteLine($"����`�̎��ʎq{ident}���ꎟ���Ƃ��ė��p����Ă��܂��B");
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression(ident);
                }
                if (value.IsVariable()) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression(ident, value.ToVariable());
                }
                if (value.IsEnumValue()) {
                    var ev = value.ToEnumValue();
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ev);
                }
                if (value.IsFunction()) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(ident, value.ToFunction());
                }
                throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"�ꎟ���Ƃ��Ďg�����`�ςݎ��ʎq�͕ϐ��A�񋓒萔�A�֐��̉��ꂩ�ł����A { lexer.current_token().ToString() } �͂��̂�����ł�����܂���B�i�{�����n�̎����Ɍ�肪����Ǝv���܂��B�j");
            }
            if (is_constant()) {
                return constant();
            }
            if (is_STRING()) {
                List<string> strings = new List<string>();
                while (is_STRING()) {
                    strings.Add(STRING());
                }
                return new SyntaxTree.Expression.PrimaryExpression.StringExpression(strings);
            }
            if (lexer.ReadIf('(')) {
                if (lexer.Peek('{')) {
                    // gcc statement expression
                    var statements = compound_statement();
                    lexer.Read(')');
                    return new SyntaxTree.Expression.GccStatementExpression(statements);
                } else {
                    var expr = new SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression(expression());
                    lexer.Read(')');
                    return expr;
                }
            }
            throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"�ꎟ���ƂȂ�v�f������ׂ��ꏊ�� { lexer.current_token().ToString() } ������܂��B");
        }

        /// <summary>
        /// 6.5.1 �ꎟ��(�萔�ƂȂ肤�邩)
        /// </summary>
        /// <returns></returns>
        private bool is_constant() {
            return is_INTEGER_CONSTANT() ||
                   is_CHARACTER_CONSTANT() ||
                   is_FLOATING_CONSTANT() ||
                   is_ENUMERATION_CONSTANT();
        }

        /// <summary>
        /// 6.5.1 �ꎟ��(�萔)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression constant() {
            // 6.5.1 �ꎟ��
            // �萔�́C�ꎟ���Ƃ���B���̌^�́C���̌`���ƒl�ɂ���Č��܂�i6.4.4 �ŋK�肷��B�j�B

            // �����萔
            if (is_INTEGER_CONSTANT()) {
                return INTEGER_CONSTANT();
            }

            // �����萔
            if (is_CHARACTER_CONSTANT()) {
                var ret = CHARACTER_CONSTANT();
                return new SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant(ret);
            }

            // ���������萔
            if (is_FLOATING_CONSTANT()) {
                return FLOATING_CONSTANT();
            }

            // �񋓒萔
            if (is_ENUMERATION_CONSTANT()) {
                var ret = ENUMERATION_CONSTANT();
                return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ret);
            }

            throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"�萔������ׂ��ꏊ�� { lexer.current_token().ToString() } ������܂��B");
        }

        /// <summary>
        /// 6.5.2 ��u���Z�q(��u���̑O��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression postfix_expression() {
            var expr = primary_expression();
            return more_postfix_expression(expr);

        }

        /// <summary>
        /// 6.5.2 ��u���Z�q(��u���̌㔼)
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        private SyntaxTree.Expression more_postfix_expression(SyntaxTree.Expression expr) {
            if (lexer.ReadIf('[')) {
                // 6.5.2.1 �z��̓Y���t��
                var index = expression();
                lexer.Read(']');
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression(expr, index));
            }
            if (lexer.ReadIf('(')) {
                // 6.5.2.2 �֐��ďo��
                List<SyntaxTree.Expression> args = null;
                if (lexer.Peek(')') == false) {
                    args = argument_expression_list();
                } else {
                    args = new List<SyntaxTree.Expression>();
                }
                lexer.Read(')');
                // ����`�̎��ʎq�̒���Ɋ֐��Ăяo���p�̌�u���Z�q '(' ������ꍇ�A
                // K&R�����C89/90�ł͈ÖٓI�֐��錾 extern int ���ʎq(); �����݂̐錾�u���b�N�̐擪�Œ�`����Ă���Ɖ��肵�Ė|�󂷂�
                if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression) {

                }
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.FunctionCallExpression(expr, args));
            }
            if (lexer.ReadIf('.')) {
                // 6.5.2.3 �\���̋y�ы��p�̂̃����o
                var ident = IDENTIFIER(false);
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.MemberDirectAccess(expr, ident));
            }
            if (lexer.ReadIf(Token.TokenKind.PTR_OP)) {
                // 6.5.2.3 �\���̋y�ы��p�̂̃����o
                var ident = IDENTIFIER(false);
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess(expr, ident));
            }
            if (lexer.Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                // 6.5.2.4 ��u�����y�ь�u�������Z�q
                var op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.INC_OP:
                        op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc;
                        break;
                    case Token.TokenKind.DEC_OP:
                        op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, "���Ԃ�����~�X�ł��B");
                }
                lexer.next_token();
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression(op, expr));
            }
            // 6.5.2.5 �������e����
            // Todo: ������
            return expr;
        }

        /// <summary>
        /// 6.5.2 ��u���Z�q(����������)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Expression> argument_expression_list() {
            var ret = new List<SyntaxTree.Expression>();
            ret.Add(assignment_expression());
            while (lexer.ReadIf(',')) {
                ret.Add(assignment_expression());
            }
            return ret;
        }


        /// <summary>
        /// 6.5.3 �P�����Z�q(�P����)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression unary_expression() {
            if (lexer.Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                var op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.INC_OP:
                        op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc;
                        break;
                    case Token.TokenKind.DEC_OP:
                        op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, "���Ԃ�����~�X�ł��B");
                }
                lexer.next_token();
                var expr = unary_expression();
                return new SyntaxTree.Expression.UnaryPrefixExpression(op, expr);
            }
            if (lexer.ReadIf('&')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryAddressExpression(expr);
            }
            if (lexer.ReadIf('*')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryReferenceExpression(expr);
            }
            if (lexer.ReadIf('+')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryPlusExpression(expr);
            }
            if (lexer.ReadIf('-')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryMinusExpression(expr);
            }
            if (lexer.ReadIf('~')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryNegateExpression(expr);
            }
            if (lexer.ReadIf('!')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryNotExpression(expr);
            }
            if (lexer.ReadIf(Token.TokenKind.SIZEOF)) {
                if (lexer.Peek('(')) {
                    // �ǂ����ɂ�'('���o�邱�Ƃ��o����̂ł���ɐ�ǂ݂���iLL(2))
                    var saveCurrent = lexer.Save();
                    lexer.Read('(');
                    if (is_type_name()) {
                        var type = type_name();
                        lexer.Read(')');
                        return new SyntaxTree.Expression.SizeofTypeExpression(type);
                    } else {
                        lexer.Restore(saveCurrent);
                        var expr = unary_expression();
                        return new SyntaxTree.Expression.SizeofExpression(expr);
                    }
                } else {
                    // ���ʂ��Ȃ��̂Ŏ�
                    var expr = unary_expression();
                    return new SyntaxTree.Expression.SizeofExpression(expr);
                }
            }
            return postfix_expression();
        }

        /// <summary>
        /// 6.5.4 �L���X�g���Z�q(�L���X�g��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression cast_expression() {
            if (lexer.Peek('(')) {
                // �ǂ���ɂ�'('�̏o����������邽�߂���ɐ�ǂ݂��s���B
                var saveCurrent = lexer.Save();
                lexer.Read('(');
                if (is_type_name()) {
                    var type = type_name();
                    lexer.Read(')');
                    var expr = cast_expression();
                    return new SyntaxTree.Expression.CastExpression(type, expr);
                } else {
                    lexer.Restore(saveCurrent);
                    return unary_expression();
                }
            } else {
                return unary_expression();
            }
        }

        /// <summary>
        /// 6.5.5 �揜���Z�q(�揜��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression multiplicitive_expression() {
            var lhs = cast_expression();
            while (lexer.Peek('*', '/', '%')) {
                SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind op;
                switch (lexer.current_token().Kind) {
                    case (Token.TokenKind)'*':
                        op = SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul;
                        break;
                    case (Token.TokenKind)'/':
                        op = SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div;
                        break;
                    case (Token.TokenKind)'%':
                        op = SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "");
                }
                lexer.next_token();
                var rhs = cast_expression();
                lhs = new SyntaxTree.Expression.MultiplicitiveExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.6 �������Z�q(������)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression additive_expression() {
            var lhs = multiplicitive_expression();
            while (lexer.Peek('+', '-')) {
                SyntaxTree.Expression.AdditiveExpression.OperatorKind op;
                switch (lexer.current_token().Kind) {
                    case (Token.TokenKind)'+':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add;
                        break;
                    case (Token.TokenKind)'-':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "");
                }
                lexer.next_token();
                var rhs = multiplicitive_expression();
                lhs = new SyntaxTree.Expression.AdditiveExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.7 �r�b�g�P�ʂ̃V�t�g���Z�q(�V�t�g��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression shift_expression() {
            var lhs = additive_expression();
            while (lexer.Peek(Token.TokenKind.LEFT_OP, Token.TokenKind.RIGHT_OP)) {
                var op = SyntaxTree.Expression.ShiftExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.LEFT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Left;
                        break;
                    case Token.TokenKind.RIGHT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Right;
                        break;
                    default:
                        throw new Exception();
                }

                lexer.next_token();
                var rhs = additive_expression();
                lhs = new SyntaxTree.Expression.ShiftExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.8 �֌W���Z�q(�֌W��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression relational_expression() {
            var lhs = shift_expression();
            while (lexer.Peek((Token.TokenKind)'<', (Token.TokenKind)'>', Token.TokenKind.LE_OP, Token.TokenKind.GE_OP)) {
                var op = SyntaxTree.Expression.RelationalExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case (Token.TokenKind)'<':
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan;
                        break;
                    case (Token.TokenKind)'>':
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan;
                        break;
                    case Token.TokenKind.LE_OP:
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual;
                        break;
                    case Token.TokenKind.GE_OP:
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual;
                        break;
                    default:
                        throw new Exception();
                }
                lexer.next_token();
                var rhs = shift_expression();
                lhs = new SyntaxTree.Expression.RelationalExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.9 �������Z�q(������)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression equality_expression() {
            var lhs = relational_expression();
            while (lexer.Peek(Token.TokenKind.EQ_OP, Token.TokenKind.NE_OP)) {
                var op = SyntaxTree.Expression.EqualityExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.EQ_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal;
                        break;
                    case Token.TokenKind.NE_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual;
                        break;
                    default:
                        throw new Exception();
                }
                lexer.next_token();
                var rhs = relational_expression();
                lhs = new SyntaxTree.Expression.EqualityExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.10 �r�b�g�P�ʂ� AND ���Z�q(AND��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression and_expression() {
            var lhs = equality_expression();
            while (lexer.ReadIf('&')) {
                var rhs = equality_expression();
                lhs = new SyntaxTree.Expression.AndExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.11 �r�b�g�P�ʂ̔r�� OR ���Z�q(�r��OR��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression exclusive_OR_expression() {
            var lhs = and_expression();
            while (lexer.ReadIf('^')) {
                var rhs = and_expression();
                lhs = new SyntaxTree.Expression.ExclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.12 �r�b�g�P�ʂ� OR ���Z�q(OR��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression inclusive_OR_expression() {
            var lhs = exclusive_OR_expression();
            while (lexer.ReadIf('|')) {
                var rhs = exclusive_OR_expression();
                lhs = new SyntaxTree.Expression.InclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.13 �_�� AND ���Z�q(�_��AND��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression logical_AND_expression() {
            var lhs = inclusive_OR_expression();
            while (lexer.ReadIf(Token.TokenKind.AND_OP)) {
                var rhs = inclusive_OR_expression();
                lhs = new SyntaxTree.Expression.LogicalAndExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.14 �_�� OR ���Z�q(�_��OR��)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression logical_OR_expression() {
            var lhs = logical_AND_expression();
            while (lexer.ReadIf(Token.TokenKind.OR_OP)) {
                var rhs = logical_AND_expression();
                lhs = new SyntaxTree.Expression.LogicalOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.15 �������Z�q(������)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression conditional_expression() {
            var cond = logical_OR_expression();
            if (lexer.ReadIf('?')) {
                var then_expr = expression();
                lexer.Read(':');
                var else_expr = conditional_expression();
                return new SyntaxTree.Expression.ConditionalExpression(cond, then_expr, else_expr);
            } else {
                return cond;
            }
        }

        /// <summary>
        /// 6.5.16 ������Z�q(�����)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression assignment_expression() {
            var lhs = conditional_expression();
            if (is_assignment_operator()) {
                var op = assignment_operator();
                var rhs = assignment_expression();
                if (op == "=") {
                    lhs = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression(op, lhs, rhs);
                } else {
                    lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(op, lhs, rhs);
                }
            }
            return lhs;
        }

        /// <summary>
        ///6.5.16 ������Z�q�i������Z�q�g�[�N���ƂȂ肤�邩�H�j
        /// </summary>
        /// <returns></returns>
        private bool is_assignment_operator() {
            return lexer.Peek((Token.TokenKind)'=', Token.TokenKind.MUL_ASSIGN, Token.TokenKind.DIV_ASSIGN, Token.TokenKind.MOD_ASSIGN, Token.TokenKind.ADD_ASSIGN, Token.TokenKind.SUB_ASSIGN, Token.TokenKind.LEFT_ASSIGN, Token.TokenKind.RIGHT_ASSIGN, Token.TokenKind.AND_ASSIGN, Token.TokenKind.XOR_ASSIGN, Token.TokenKind.OR_ASSIGN);
        }

        /// <summary>
        /// 6.5.16 ������Z�q�i������Z�q�g�[�N���j
        /// </summary>
        /// <returns></returns>
        private string assignment_operator() {
            if (is_assignment_operator() == false) {
                throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"������Z�q������ׂ��ꏊ�� { lexer.current_token().ToString() } ������܂��B");
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        /// <summary>
        /// 6.6 �萔��
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression constant_expression() {
            // �⑫����  
            // �萔���́C���s���ł͂Ȃ��|�󎞂ɕ]�����邱�Ƃ��ł���B���������āC�萔���g�p���Ă悢�Ƃ���Ȃ�΂ǂ��ł��g�p���Ă悢�B
            //
            // ����
            // - �萔���́C����C�����C�����C�֐��ďo�����̓R���}���Z�q���܂�ł͂Ȃ�Ȃ��B
            //   �������C�萔�����]������Ȃ�������(sizeof���Z�q�̃I�y�����h��)�Ɋ܂܂�Ă���ꍇ�������B
            // - �萔����]���������ʂ́C���̌^�ŕ\���\�Ȓl�͈͓̔��ɂ���萔�łȂ���΂Ȃ�Ȃ��B
            // 

            // ToDo: �������q���̒萔���̈���������
            return conditional_expression();

        }

    }
}