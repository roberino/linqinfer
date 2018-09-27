using System;
using System.Text;

namespace LinqInfer.Utility.Expressions
{
    class StringNavigator<T> where T : struct
    {
        readonly Func<T, char, T> _tokenClassifier;
        readonly Func<T, bool> _accumulationRule;
        readonly StringBuilder _buffer;

        T _tokenClass;
        int _startPos;
        int _pos;

        public StringNavigator(string input, Func<T, char, T> tokenClassifier, Func<T, bool> accumulationRule)
        {
            _tokenClassifier = tokenClassifier;
            _accumulationRule = accumulationRule;
            _buffer = new StringBuilder(16);

            Input = input;
        }

        public string Input { get; }

        public int StartPosition => _startPos;

        public T TokenClass => _tokenClass;

        public string CurrentToken => _buffer.ToString();

        public bool ReadNextToken()
        {
            var more = true;
            var read = true;
            var first = true;

            if (_pos == Input.Length) return false;

            _buffer.Clear();

            _startPos = _pos;

            while (more && read)
            {
                var c = Input[_pos];

                var tokenClass = _tokenClassifier(_tokenClass, c);

                read = first || _tokenClass.Equals(tokenClass) && _accumulationRule(tokenClass);

                if (read)
                {
                    first = false;

                    _buffer.Append(c);

                    _tokenClass = tokenClass;

                    _pos++;
                }

                more = _pos < Input.Length;
            }

            return true;
        }
    }
}