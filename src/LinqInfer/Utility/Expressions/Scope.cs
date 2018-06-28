using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    internal class Scope
    {
        public Scope(Expression currentContext, Scope globalContext = null)
        {
            CurrentContext = currentContext;
            GlobalContext = globalContext ?? this;
            IsRoot = globalContext == null;
        }

        public bool IsRoot { get; }

        public Scope GlobalContext { get; }

        public Expression CurrentContext { get; }

        public Scope NewScope(Expression newContext)
        {
            return new Scope(newContext, GlobalContext);
        }
    }
}