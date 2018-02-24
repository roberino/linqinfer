using LinqInfer.Data;
using LinqInfer.Learning.Features;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.Tests
{
    internal static class TestData
    {
        public static List<double[]> LinearClassA()
        {
            return new List<double[]>() {
                new [] { -2.31964063782679,-2.15797601734259},
                new [] { -1.34486045624634,-1.59861039725831},
                new [] { -2.05326896105203,-2.71511731321129},
                new [] { -1.45861683080918,-1.4969804331625},
                new [] { -2.23323066526029,-3.60003974687425},
                new [] { -1.97354114513633,-0.742157890282601},
                new [] { -1.75169183311836,-3.1199437145827},
                new [] { -0.833434756360044,-4.88447776776708},
                new [] { -2.98958276149992,-2.00048789663743},
                new [] { -0.415749403665706,-5.04005627707889},
                new [] { -1.69967169913598,-4.11537789450129},
                new [] { -1.46078134566514,-1.7387183105175},
                new [] { -1.06934136235383,-4.03627103860973},
                new [] { -1.17916140839115,-2.70515008242172},
                new [] { -1.13653111998544,-0.843920324167995},
                new [] { -1.07936088079086,-5.67660281752595},
                new [] { -2.7548325935415,-4.44797820562157},
                new [] { -2.57320723566794,-6.83980452183208}
                };
        }

        public static List<double[]> LinearClassB()
        {
            return new List<double[]>() {
                new [] { 3.13625962299855,1.70574366130339},
                new [] { 1.44960663045533,3.78659026283509},
                new [] { 8.55287862971193,5.12352680642597},
                new [] { 3.46928844030961,4.39927016099728},
                new [] { 0.533117589158309,2.06155582715787},
                new [] { 7.52165969014327,5.99813193894026},
                new [] { 2.88789354514722,0.300457046644186},
                new [] { 6.0338327775947,4.08574954343144},
                new [] { 6.58717753603905,0.0340742429058987},
                new [] { 1.34897009219722,3.95392305512217},
                new [] { 5.87266240984335,5.77185442185486},
                new [] { 1.33686500081341,4.91909408780346},
                new [] { 2.52743382195204,1.77385969384955},
                new [] { 1.37907188663218,5.38942724616378},
                new [] { 8.77018649330205,1.83966777579162},
                new [] { 6.47881707271054,5.1231948151746},
                new [] { 6.40835281330232,3.80898336493344},
                new [] { 7.29323453008899,2.64968896925315}
                };
        }

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

            [Feature(Model = FeatureVectorModel.Categorical)]
            public string Category { get; set; }

            [Feature(Model = FeatureVectorModel.Semantic)]
            public string Text { get; set; }

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