using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCPP
{
    public class Source : System.IDisposable
    {
        private bool _handleEoF { get; set;  }
        private System.IO.TextReader _textReader { get; set;  }
        private Stack<int> UnreadStack { get; } = new Stack<int>();

        public Source(System.IO.TextReader textReader)
        {
            _textReader = textReader;
            _handleEoF = false;
        }

        public int Read(Action<string> badcharHandler)
        {
            if (disposedValue) {
                throw new System.ObjectDisposedException(nameof(Source), "Sourceは既にDisposeされています。");
            }
            if (UnreadStack.Any()) {
                return UnreadStack.Pop();
            }
            if (_handleEoF) {
                return -1;
            }
            var ch = _textReader.Read();
            if (ch == -1) {
                _handleEoF = true;
            } else if (ch == 0xFFFF) {
                // DetectEncoding内で文字コード上で表現できない文字があった場合は 前後に \uFFFFを付与しているので
                // それを表現不能文字として読み取る
                StringBuilder sb = new StringBuilder();
                while ((ch = _textReader.Read()) != 0xFFFF) {
                    sb.Append((char)ch);
                }
                badcharHandler(sb.ToString());

                ch = '?';
            }

                return ch;
        }

        public void Unread(int v)
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