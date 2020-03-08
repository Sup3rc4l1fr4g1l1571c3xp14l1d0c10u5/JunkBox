using System;
using System.Collections.Generic;

namespace X86Asm.ast.operand {
    using X86Asm.model;

    /// <summary>
    /// 文字列リテラルオペランド
    /// </summary>
    public class StringLiteral : IImmediate {

        /// <summary>
        /// 文字列リテラル
        /// </summary>
        public string Body { get; }

        /// <summary>
        /// 文字列リテラルのバイト表現
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="body">文字列本体</param>
        public StringLiteral(string body) {
            if (body == null) {
                throw new ArgumentNullException(nameof(body));
            }
            Body = body;
            Bytes = unescapeString(body);
        }

        private static byte[] unescapeString(string body) {
            using (var sb = new System.IO.MemoryStream()) {
                for (var i = 0; i < body.Length; i++) {
                    if (body[i] != '\\') {
                        var bytes = System.Text.Encoding.Default.GetBytes(new[] { body[i] });
                        sb.Write(bytes, 0, bytes.Length);
                        continue;
                    } else {
                        i += 1;
                        switch (body[i]) {
                            case 'a': sb.WriteByte(0x07); continue;
                            case 'b': sb.WriteByte(0x08); continue;
                            case 'e': sb.WriteByte(0x1B); continue;
                            case 'f': sb.WriteByte(0x0C); continue;
                            case 'n': sb.WriteByte(0x0A); continue;
                            case 'r': sb.WriteByte(0x0D); continue;
                            case 't': sb.WriteByte(0x09); continue;
                            case 'v': sb.WriteByte(0x0B); continue;
                            case '\\': sb.WriteByte(0x5C); continue;
                            case '"': sb.WriteByte(0x22); continue;
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7': {
                                    uint ch = 0;
                                    for (var j = 0; j < 3; j++,i++) {
                                        if (body.Length <= i) {
                                            break;
                                        } else if ('0' <= body[i] && body[i] <= '7') {
                                            ch = (uint)(ch << 3) | (uint)(body[i] - '0');
                                        } else {
                                            break;
                                        }
                                    }
                                    sb.WriteByte((byte)ch);
                                    continue;
                                }
                            case 'x': {
                                    i += 1;
                                    uint ch = 0;
                                    for (var j = 0; j < 2; j++,i++) {
                                        if (body.Length <= i) {
                                            throw new Exception();
                                        } else if ('0' <= body[i] && body[i] <= '9') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - '0');
                                        } else if ('A' <= body[i] && body[i] <= 'F') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - 'A' + 10);
                                        } else if ('a' <= body[i] && body[i] <= 'f') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - 'a' + 10);
                                        }
                                    }
                                    sb.WriteByte((byte)ch);
                                    continue;
                                }
                            case 'u': {
                                    i += 1;
                                    uint ch = 0;
                                    for (var j = 0; j < 4; j++, i++) {
                                        if (body.Length <= i) {
                                            throw new Exception();
                                        } else if ('0' <= body[i] && body[i] <= '9') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - '0');
                                        } else if ('A' <= body[i] && body[i] <= 'F') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - 'A' + 10);
                                        } else if ('a' <= body[i] && body[i] <= 'f') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - 'a' + 10);
                                        }
                                    }
                                    var codePoints = BitConverter.GetBytes((ushort)ch);
                                    var chars = System.Text.Encoding.UTF32.GetChars(codePoints);
                                    var bytes = System.Text.Encoding.Default.GetBytes(chars);
                                    sb.Write(bytes, 0, bytes.Length);
                                    continue;
                                }
                            case 'U': {
                                    i += 1;
                                    uint ch = 0;
                                    for (var j = 0; j < 8; j++, i++) {
                                        if (body.Length <= i) {
                                            throw new Exception();
                                        } else if ('0' <= body[i] && body[i] <= '9') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - '0');
                                        } else if ('A' <= body[i] && body[i] <= 'F') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - 'A' + 10);
                                        } else if ('a' <= body[i] && body[i] <= 'f') {
                                            ch = (uint)(ch << 4) | (uint)(body[i] - 'a' + 10);
                                        }
                                    }
                                    var codePoints = BitConverter.GetBytes((uint)ch);
                                    var chars = System.Text.Encoding.UTF32.GetChars(codePoints);
                                    var bytes = System.Text.Encoding.Default.GetBytes(chars);
                                    sb.Write(bytes, 0, bytes.Length);
                                    continue;
                                }
                            default: {
                                    throw new Exception("");
                                }

                        }
                    }

                }
                return sb.ToArray();
            }
        }

        /// <summary>
        /// 即値オペランドの値を返す
        /// Labelクラスの値は再配置可能なシンボルである。
        /// </summary>
        /// <param name="symbolTable"> シンボル表 </param>
        /// <returns>ラベルに対応するオフセットを値として持つ即値</returns>
        public ImmediateValue GetValue(IDictionary<string, Symbol> symbolTable) {
            throw new NotSupportedException("文字列リテラルは即値ではないため値を得られません。");
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="obj">比較対象</param>
        /// <returns>ラベル文字列が一致すれば真</returns>
        public override bool Equals(object obj) {
            if (!(obj is StringLiteral)) {
                return false;
            } else {
                return Body.Equals(((StringLiteral)obj).Body);
            }
        }

        /// <summary>
        /// このオブジェクトのハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode() {
            return Body.GetHashCode();
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            return Body;
        }

    }

}