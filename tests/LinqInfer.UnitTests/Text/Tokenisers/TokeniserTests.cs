using System.Linq;
using LinqInfer.Text;
using LinqInfer.Text.Tokenisers;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.Tokenisers
{
    [TestFixture]
    public class TokeniserTests
    {
        [Test]
        public void Tokenise_TwoCharNewLine_ReturnsSingleNewLineToken()
        {
            var tokeniser = new Tokeniser();

            var tokens = tokeniser.Tokenise("once\r\nupon").ToList();

            Assert.That(tokens.Count, Is.EqualTo(3));
            Assert.That(tokens[0].Text, Is.EqualTo("once"));
            Assert.That(tokens[1].Text, Is.EqualTo("\r\n"));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.NewLine));
            Assert.That(tokens[2].Text, Is.EqualTo("upon"));
        }

        [Test]
        public void Tokenise_NewLines_ReturnsNewLines()
        {
            var tokeniser = new Tokeniser();

            var tokens = tokeniser.Tokenise("a b c\nd e\rf").ToList();

            Assert.That(tokens[0].Text, Is.EqualTo("a"));
            Assert.That(tokens[2].Text, Is.EqualTo("b"));
            Assert.That(tokens[4].Text, Is.EqualTo("c"));
            Assert.That(tokens[5].Text, Is.EqualTo("\n"));
            Assert.That(tokens[5].Type, Is.EqualTo(TokenType.NewLine));
            Assert.That(tokens[6].Text, Is.EqualTo("d"));
            Assert.That(tokens[8].Text, Is.EqualTo("e"));
            Assert.That(tokens[9].Text, Is.EqualTo("\r"));
            Assert.That(tokens[9].Type, Is.EqualTo(TokenType.NewLine));
            Assert.That(tokens[10].Text, Is.EqualTo("f"));
        }

        [Test]
        public void Tokenise_SomeWords_ReturnsExpectedTokens()
        {
            var tokeniser = new Tokeniser();

            var tokens = tokeniser.Tokenise("a 1st example").ToList();

            Assert.That(tokens.Count, Is.EqualTo(5));

            Assert.That(tokens[0].Text, Is.EqualTo("a"));
            Assert.That(tokens[0].Index, Is.EqualTo(0));
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Word));

            Assert.That(tokens[1].Text, Is.EqualTo(" "));
            Assert.That(tokens[1].Index, Is.EqualTo(1));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Space));

            Assert.That(tokens[2].Text, Is.EqualTo("1st"));
            Assert.That(tokens[2].Index, Is.EqualTo(2));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Word));

            Assert.That(tokens[3].Text, Is.EqualTo(" "));
            Assert.That(tokens[3].Index, Is.EqualTo(5));
            Assert.That(tokens[3].Type, Is.EqualTo(TokenType.Space));

            Assert.That(tokens[4].Text, Is.EqualTo("example"));
            Assert.That(tokens[4].Index, Is.EqualTo(6));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Word));
        }

        [Test]
        public void Tokenise_IntegersAndDecimals_ReturnsExpectedNumericTokens()
        {
            var tokeniser = new Tokeniser();

            var tokens = tokeniser.Tokenise("2 77.1 p").ToList();

            Assert.That(tokens.Count, Is.EqualTo(5));

            Assert.That(tokens[0].Text, Is.EqualTo("2"));
            Assert.That(tokens[0].Index, Is.EqualTo(0));
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));

            Assert.That(tokens[2].Text, Is.EqualTo("77.1"));
            Assert.That(tokens[2].Index, Is.EqualTo(2));
            Assert.That(tokens[2].Type, Is.EqualTo(TokenType.Number));

            Assert.That(tokens[4].Text, Is.EqualTo("p"));
            Assert.That(tokens[4].Index, Is.EqualTo(7));
            Assert.That(tokens[4].Type, Is.EqualTo(TokenType.Word));
        }

        [Test]
        public void Tokenise_Currency_ReturnsNumbersAndSymbol()
        {
            var tokeniser = new Tokeniser();

            var tokens = tokeniser.Tokenise("$107.65 x").ToList();

            Assert.That(tokens.Count, Is.EqualTo(4));

            Assert.That(tokens[0].Text, Is.EqualTo("$"));
            Assert.That(tokens[0].Index, Is.EqualTo(0));
            Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Symbol));

            Assert.That(tokens[1].Text, Is.EqualTo("107.65"));
            Assert.That(tokens[1].Index, Is.EqualTo(1));
            Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Number));
        }
    }
}