using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Text;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text
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
        public async Task DefaultConstructor_Filtered_WriteAsync()
        {
            var writer = new TokenisingTextWriter();

            var tokens = new List<IToken>();

            writer.AddSink(t =>
            {
                tokens = t.ToList();
                Assert.That(t.Skip(3).First().Text, Is.EqualTo("day"));
            });

            writer.AddFilter(t => t.Where(x => !x.Text.StartsWith("f")));

            await writer.WriteAsync("a foggy day");
            await writer.FlushTokenBuffer();

            Assert.That(tokens.Count, Is.EqualTo(4));
        }

        [Test]
        public void DefaultConstructor_WriteChars()
        {
            var writer = new TokenisingTextWriter();

            var tokens = new List<IToken>();

            writer.AddSink(t =>
            {
                tokens = t.ToList();
            });

            writer.Write('a');
            writer.Write('b');
            writer.Write('c');

            Assert.That(tokens.Count, Is.EqualTo(0));

            writer.Write('\n');

            Assert.That(tokens.Count, Is.EqualTo(2));
            Assert.That(tokens.First().Text, Is.EqualTo("abc"));
        }

        [Test]
        public void DefaultConstructor_Write_BufferIsFlushed()
        {
            var writer = new TokenisingTextWriter() { MaxBufferSize = 8 };

            var tokens = new List<IToken>();

            writer.AddSink(t =>
            {
                tokens = t.ToList();
            });

            writer.Write("abcdef");

            Assert.That(tokens.Count, Is.EqualTo(0));

            writer.Write("ghi j");

            Assert.That(tokens.Count, Is.EqualTo(2));
            Assert.That(tokens.First().Text, Is.EqualTo("abcdefghi"));            
        }

        [Test]
        public void DefaultConstructor_OverflowBuffer_ThrowsException()
        {
            var writer = new TokenisingTextWriter() { MaxBufferSize = 8 };

            var tokens = new List<IToken>();

            writer.AddSink(t =>
            {
                tokens = t.ToList();
            });

            writer.Write("abcdef");

            Assert.That(tokens.Count, Is.EqualTo(0));

            try
            {
                writer.Write("ghij");
                Assert.Fail("Error not thrown");
            }
            catch (AggregateException ex)
            {
                Assert.That(ex.InnerException, Is.InstanceOf<Exception>());
            }
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
        public void DefaultConstructor_WriteLong()
        {
            TestWriteNum((long)1234, (t, v) => t.Write(v));
        }

        [Test]
        public void DefaultConstructor_WriteDecimal()
        {
            TestWriteNum(1234.673m, (t, v) => t.Write(v));
        }

        [Test]
        public void DefaultConstructor_WriteFloat()
        {
            TestWriteNum(1234.6543f, (t, v) => t.Write(v));
        }

        [Test]
        public void ConstructorWithInnerWriter_WriteLine()
        {
            using (var stringWriter = new StringWriter())
            {
                var writer = new TokenisingTextWriter(stringWriter);

                int tokenCount = -1;

                writer.AddSink(t =>
                {
                    tokenCount = t.Count();
                });

                writer.WriteLine("a foggy day");
                writer.Flush();

                Assert.That(tokenCount, Is.EqualTo(6));
                Assert.That(stringWriter.ToString(), Is.EqualTo("a foggy day" + Environment.NewLine));
            }
        }

        void TestWriteNum<T>(T value, Action<TokenisingTextWriter, T> action)
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