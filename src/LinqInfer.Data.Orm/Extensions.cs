using System;
using System.Data;

namespace LinqInfer.Data.Orm
{
    public static class Extensions
    {
        public static T Field<T>(this DataRow row, int index)
        {
            var val = row.ItemArray[index];
            var t = typeof(T);

            if (val == null) return default(T);

            try
            {
                return (T)val;
            }
            catch
            {
                return (T)Convert.ChangeType(val, t);
            }
        }
    }
}