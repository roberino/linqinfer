using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Text
{
    public interface ITokeniser
    {
        IEnumerable<string> Tokenise(string body);
    }
}
