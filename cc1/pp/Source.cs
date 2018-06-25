using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCPP
{
    public class Source : System.IDisposable
    {
        private bool _handleEoF { get; set;  }
        private Utf32Reader _textReader { get; set;  }
        private Stack<UInt32> UnreadStack { get; } = new Stack<UInt32>();

        public Source(Utf32Reader textReader)
        {
            _textReader = textReader;
            _handleEoF = false;
        }

        public UInt32 Read(Action<string> badcharHandler)
        {
            if (disposedValue) {
                throw new System.ObjectDisposedException(nameof(Source), "Sourceは既にDisposeされています。");
            }
            if (UnreadStack.Any()) {
                return UnreadStack.Pop();
            }
            if (_handleEoF) {
                return UInt32.MaxValue;
            }
            var ch = _textReader.Read();
            if (ch == Utf32Reader.EoF) {
                _handleEoF = true;
            } else if (ch == Utf32Reader.BadCharStart) {
                // DetectEncoding内で元の文字コード上で表現できない文字があった場合は 前後に マーカー を付与しているのでその範囲を表現不能文字として読み取る。
                List<UInt32> sb = new List<UInt32>();
                while ((ch = _textReader.Read()) != Utf32Reader.BadCharEnd) {
                    sb.Add(ch);
                }
                badcharHandler(System.Text.Encoding.UTF32.GetString(sb.Select(x => BitConverter.GetBytes(x)).SelectMany((x) => x).ToArray()));

                ch = '?';
            }

                return ch;
        }

        public void Unread(uint v)
        {
            UnreadStack.Push(v);
        }

        #region System.IDisposable で必要な実装

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (_textReader != null) {
                        _textReader.Close();
                        _textReader.Dispose();
                        _textReader = null;
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        #endregion
    }
}