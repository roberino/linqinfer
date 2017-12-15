using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths.Probability;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            yield return new Pirate() { Gold = 100, Age = 25, IsCaptain = false, Ships = 1, Category = "a" };
            yield return new Pirate() { Gold = 1600, Age = 45, IsCaptain = true, Ships = 2, Category = "b" };
            yield return new Pirate() { Gold = 1250, Age = 64, IsCaptain = true, Ships = 5, Category = "b" };
            yield return new Pirate() { Gold = 50, Age = 21, IsCaptain = false, Ships = 1, Category = "c" };
            yield return new Pirate() { Gold = 60, Age = 19, IsCaptain = false, Ships = 1, Category = "c" };
            yield return new Pirate() { Gold = 1800, Age = 52, IsCaptain = true, Ships = 3, Category = "c" };
            yield return new Pirate() { Gold = 101, Age = 18, IsCaptain = false, Ships = 2, Category = "d" };
            yield return new Pirate() { Gold = 1100, Age = 58, IsCaptain = true, Ships = 4, Category = "a" };
        }

        public class Pirate : IBinaryPersistable
        {
            public int Gold { get; set; }

            public int Age { get; set; }

            public int Ships { get; set; }

            public bool IsCaptain { get; set; }

            [Feature(Model = DistributionModel.Categorical)]
            public string Category { get; set; }

            public override string ToString()
            {
                return string.Format("Age={0}, Gold={1}, IsCaptain={2}, Ships={3}", Age, Gold, IsCaptain, Ships);
            }

            public void Save(Stream output)
            {
                using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
                {
                    writer.Write(Gold);
                    writer.Write(Age);
                    writer.Write(Ships);
                    writer.Write(IsCaptain);
                }
            }

            public void Load(Stream input)
            {
                using (var reader = new BinaryReader(input, Encoding.UTF8, true))
                {
                    Gold = reader.ReadInt32();
                    Age = reader.ReadInt32();
                    Ships = reader.ReadInt32();
                    IsCaptain = reader.ReadBoolean();
                }
            }
        }
    }
}