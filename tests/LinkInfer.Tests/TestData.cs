using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests
{
    internal static class TestData
    {
        public static IQueryable<Pirate> CreateQueryablePirates()
        {
            return CreatePirates().AsQueryable();
        }

        public static IEnumerable<Pirate> CreatePirates()
        {
            yield return new Pirate() { Gold = 100, Age = 25, IsCaptain = false, Ships = 1 };
            yield return new Pirate() { Gold = 1600, Age = 45, IsCaptain = true, Ships = 2 };
            yield return new Pirate() { Gold = 1250, Age = 64, IsCaptain = true, Ships = 5 };
            yield return new Pirate() { Gold = 50, Age = 21, IsCaptain = false, Ships = 1 };
            yield return new Pirate() { Gold = 60, Age = 19, IsCaptain = false, Ships = 1 };
            yield return new Pirate() { Gold = 1800, Age = 52, IsCaptain = true, Ships = 3 };
            yield return new Pirate() { Gold = 101, Age = 18, IsCaptain = false, Ships = 2 };
        }

        public class Pirate
        {
            public int Gold { get; set; }

            public int Age { get; set; }

            public int Ships { get; set; }

            public bool IsCaptain { get; set; }

            public override string ToString()
            {
                return string.Format("Age={0}, Gold={1}, IsCaptain={2}, Ships={3}", Age, Gold, IsCaptain, Ships);
            }
        }
    }
}
