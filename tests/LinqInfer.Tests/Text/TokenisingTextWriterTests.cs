using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class TokenisingTextWriterTests
    {
        [Test]
        public void DefaultConstructor_WriteLine()
        {
            var writer = new TokenisingTextWriter();
            
            int tokenCount = -1;

            writer.AddSink(t =>
            {
                tokenCount = t.Count();
                Assert.That(t.Skip(2).First().Text, Is.EqualTo("foggy"));
            });

            writer.WriteLine("a foggy day");
            
            Assert.That(tokenCount, Is.EqualTo(6));
        }

        [Test]
        public async Task DefaultConstructor_WriteLineAsync()
        {
            var writer = new TokenisingTextWriter();
            
            int tokenCount = -1;

            writer.AddSink(t =>
            {
                tokenCount = t.Count();
                Assert.That(t.Skip(2).First().Text, Is.EqualTo("foggy"));
            });

            await writer.WriteLineAsync("a foggy day");
            
            Assert.That(tokenCount, Is.EqualTo(6));
        }

        [Test]
        public async Task DefaultConstructor_Filtered_Write()
        {
            var writer = new TokenisingTextWriter();

            int tokenCount = -1;

            writer.AddSink(t =>
            {
                tokenCount = t.Count();
                Assert.That(t.Skip(3).First().Text, Is.EqualTo("day"));
            });

            writer.AddFilter(t => t.Where(x => !x.Text.StartsWith("f")));

            await writer.WriteAsync("a foggy day");

            Assert.That(tokenCount, Is.EqualTo(4));
        }

        [Test]
        public void DefaultConstructor_WriteDouble()
        {
            TestWriteNum(1234.567d, (t, v) => t.Write(v));
        }

        [Test]
        public void DefaultConstructor_WriteInt()
        {
            TestWriteNum(1234, (t, v) => t.Write(v));
        }

        [Test]
        public void DefaultConstructor_WriteFloat()
        {
            TestWriteNum(1234.6543f, (t, v) => t.Write(v));
        }

        private void TestWriteNum<T>(T value, Action<TokenisingTextWriter, T> action)
        {
            var writer = new TokenisingTextWriter();

            int tokenCount = -1;

            writer.AddSink(t =>
            {
                tokenCount = t.Count();
                Assert.That(t.Single().Text, Is.EqualTo(value.ToString()));
                Assert.That(t.Single().Type, Is.EqualTo(TokenType.Number));
            });

            action(writer, value);

            Assert.That(tokenCount, Is.EqualTo(1));
        }
    }
}