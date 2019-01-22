using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;

namespace LinqInfer.UnitTests.Text
{
    public static class TestData
    {
        public static ICorpus CreateCorpus()
        {
            var docs = CreateTextDocuments();

            var tokenDocs = docs.AsTokenisedDocuments(k => k.Root.Attribute("id").Value);

            var corpus = new Corpus();

            corpus.Append(tokenDocs.SelectMany(d => d.Tokens));

            return corpus;
        }

        public static IEnumerable<XDocument> CreateTextDocuments() =>
            XmlTexts().Select(t => XDocument.Parse(t)).ToList().AsQueryable();

        public static TextReader CreateReader()
        {
            var data = CreateTextDocuments().Aggregate(new StringBuilder(), (s, d) => s.AppendLine(d.Root.Value));

            return new StringReader(data.ToString());
        }

        public static IEnumerable<string> XmlTexts()
        {
            var doc1 = @"<doc id='1'><line>Shall I compare thee to a summer’s day?</line><line>
                        Thou art more lovely and more temperate:</line><line>
                        Rough winds do shake the darling buds of May,</line><line>
                        And summer’s lease hath all too short a date:</line><line>
                        Sometimes too hot the eye of heaven shines,</line><line>
                        And often is his gold complexion dimm’d;</line><line>
                        And every fair from fair sometime declines,</line><line>
                        By chance, or nature’s changing course, untrimm’d;</line><line>
                        But thy eternal summer shall not fade,</line><line>
                        Nor lose possession of that fair thou owest,</line><line>
                        Nor shall Death brag thou wander’st in his shade,</line><line>
                        When in eternal lines to time thou growest;</line><line>
                        So long as men can breathe, or eyes can see,</line><line>
                        So long lives this, and this gives life to thee. </line></doc>";

            var doc2 = @"<doc id='2'><line>
                        Then hate me when thou wilt; if ever, now;</line><line>
                        Now while the world is bent my deeds to cross,</line><line>
                        Join with the spite of fortune, make me bow,</line><line>
                        And do not drop in for and after-loss:</line><line>
                        Ah! do not , when my heart hath scap’d this sorrow,</line><line>
                        Come in the rearward of a conquer’d woe;</line><line>
                        Give not a windy night a rainy morrow,</line><line>
                        To linger out a purpos’d overthrow.</line><line>
                        If thou wilt leave me, do not leave me last,</line><line>
                        When other petty griefs have done their spite,</line><line>
                        But in the onset come; so shall I taste</line><line>
                        At first the very worst of fortune’s might;</line><line>
                        And other strains of woe, which now seem woe,</line><line>
                        Compar’d with loss of thee will not seem so.</line></doc>";

            var doc3 = @"<doc id='3'>To be, or not to be; that is the question;  
                        Whether ‘tis nobler in the mind to suffer
                        The slings and arrows of outrageous fortune,
                        Or to take arms against a sea of troubles,
                        And by opposing end them. To die: to sleep:
                        Nor more; and by a sleep to say we end
                        The heart-ache and the thousand natural shocks
                        That flesh is heir to; ‘tis a consummation
                        Devoutly to be wished. To die; to sleep;
                        To sleep: perchance to dream: aye, there is the rub;
                        For in that sleep of death what dreams may come,
                        When we have shuffled off this mortal coil,
                        Must give us pause: there’s the respect
                        That makes calamity of so long life;
                        For who will bear the whips and scorns of time,
                        The oppressor’s wrong, the proud man’s contumely,
                        The pangs of despis’d love, the law’s delay,
                        The insolence of office, and the spurns,
                        That patient merit of the unworthy takes,
                        When he himself might his quietus make
                        With a bare bodkin? Who would fardels bear,
                        To grunt and sweat under a weary life,
                        But that the dread of something after death –
                        The undiscover’d country from whose bourn
                        No traveler returns – puzzles the will
                        And makes us rather bear those ills we have
                        Than fly to others that we know not of?
                        Thus conscience does make cowards of us all,
                        And thus the native hue of resolution
                        Is sicklied o’er with the pale cast of thought,
                        And enterprises of great pith and moment
                        With this regard their currents turn awry,
                        And lose the name of action.</doc>";

            yield return doc1;
            yield return doc2;
            yield return doc3;
        }
    }
}
