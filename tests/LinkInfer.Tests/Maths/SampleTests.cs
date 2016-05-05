using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class SampleTests
    {
        [Test]
        public void Cards_Pof_Jack_Given_FaceCard()
        {
            var calc = new Sample<Card>(
                Enumerable
                .Range(1, 4)
                .Select(f =>
                {
                    switch (f)
                    {
                        case 1:
                            return 'C';
                        case 2:
                            return 'D';
                        case 3:
                            return 'H';
                        default:
                            return 'S';
                    }
                })
                .SelectMany(f =>
                    Enumerable
                    .Range(1, 13)
                    .Select(n => new Card(f, n))));

            var jacks = It.IsIn(Card_('C', 11), Card_('D', 11), Card_('H', 11), Card_('S', 11));
            var faces = It.IsAny<Card>(x => x.Item2 > 10);

            Assert.That(calc.Count(faces), Is.EqualTo(12));
            Assert.That(calc.ProbabilityOfEvent(jacks), Is.EqualTo(new Fraction(4, 52)));
            Assert.That(calc.PosterierProbabilityOfEventBGivenA(jacks, faces), Is.EqualTo(new Fraction(4, 12)));
            Assert.That(calc.ConditionalProbabilityOfEventAGivenB(jacks, faces), Is.EqualTo(new Fraction(4, 12)));

            Console.WriteLine("Probability of a jack: " + calc.ProbabilityOfEvent(jacks));
            Console.WriteLine("Number of a face cards: " + calc.Count(faces));
            Console.WriteLine("Conditional P of a face card given a jack: " + calc.ConditionalProbabilityOfEventAGivenB(jacks, faces));
            Console.WriteLine("Likelyhood of a face card given a jack: " + calc.LikelyhoodOfB(jacks, faces));
            Console.WriteLine("Probability of a jack given a face card: " + calc.PosterierProbabilityOfEventBGivenA(jacks, faces));
        }

        private static Card Card_(char c, int n) { return new Card(c, n); }

        private class Card : Tuple<char, int>
        {
            public Card(char f, int n) : base(f, n)
            {
            }

            public char Face { get { return Item1; } }
            public int Value { get { return Item2; } }
        }
    }
}
