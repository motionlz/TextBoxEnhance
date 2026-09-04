using NUnit.Framework;
using TextBoxEnhance;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// The parser's job is to predict exactly how many characters TextMeshPro will
    /// emit, because every effect range is expressed in TMP character indices. Get
    /// the count wrong and effects land on the wrong letters, so most of these tests
    /// pin down counting rather than the tags themselves.
    /// </summary>
    public sealed class TextEffectParserTests
    {
        private ParsedText Parse(string source)
        {
            var result = new ParsedText();
            TextEffectParser.Parse(source, result);
            return result;
        }

        [Test]
        public void StripsEffectTagsAndLeavesTextMeshProTagsAlone()
        {
            ParsedText parsed = Parse("<b>Hi <wave>there</wave></b>");

            Assert.AreEqual("<b>Hi there</b>", parsed.Text);
            Assert.AreEqual(1, parsed.Effects.Count);
        }

        [Test]
        public void RangeIsMeasuredInCharactersNotStringOffsets()
        {
            // "Hi " is three characters; <b> contributes none.
            ParsedText parsed = Parse("<b>Hi <wave>there</wave></b>");

            Assert.AreEqual(3, parsed.Effects[0].Start);
            Assert.AreEqual(8, parsed.Effects[0].End);
        }

        [Test]
        public void BreakAndSpriteTagsEachCountAsOneCharacter()
        {
            // a, <br>, b, <sprite>, c  ->  the wave starts at index 5.
            ParsedText parsed = Parse("a<br>b<sprite=0>c<wave>d</wave>");

            Assert.AreEqual(5, parsed.Effects[0].Start);
            Assert.AreEqual(6, parsed.Effects[0].End);
        }

        [Test]
        public void NestedTagsBothCoverTheOverlap()
        {
            ParsedText parsed = Parse("<wave>ab<shake>cd</shake></wave>");

            Assert.AreEqual(2, parsed.Effects.Count);

            EffectRange shake = parsed.Effects[0];
            EffectRange wave = parsed.Effects[1];

            Assert.AreEqual(2, shake.Start, "shake starts after 'ab'");
            Assert.AreEqual(4, shake.End);
            Assert.AreEqual(0, wave.Start);
            Assert.AreEqual(4, wave.End);
            Assert.IsTrue(shake.Covers(3) && wave.Covers(3), "both cover the overlap");
        }

        [Test]
        public void UnclosedTagRunsToTheEndOfTheText()
        {
            ParsedText parsed = Parse("ab<wave>cdef");

            Assert.AreEqual("abcdef", parsed.Text);
            Assert.AreEqual(2, parsed.Effects[0].Start);
            Assert.AreEqual(6, parsed.Effects[0].End);
        }

        [Test]
        public void ClosingTagWithNoOpenerIsIgnored()
        {
            ParsedText parsed = Parse("ab</wave>cd");

            Assert.AreEqual("abcd", parsed.Text);
            Assert.IsEmpty(parsed.Effects);
        }

        [Test]
        public void TagNamesAreCaseInsensitive()
        {
            ParsedText parsed = Parse("<WAVE>x</Wave>");

            Assert.AreEqual(1, parsed.Effects.Count);
            Assert.AreEqual("x", parsed.Text);
        }

        [Test]
        public void ShorthandValueFeedsThePrimaryAttribute()
        {
            ParsedText parsed = Parse("<wave=0.5>x</wave>");

            Assert.AreEqual(0.5f, parsed.Effects[0].Parameters.GetPrimaryFloat(0f, "a"), 1e-5f);
        }

        [Test]
        public void ShorthandDoesNotLeakIntoTheOtherAttributes()
        {
            // <wave=0.5> means "amplitude 0.5", not "every attribute 0.5".
            ParsedText parsed = Parse("<wave=0.5>x</wave>");
            TagParams parameters = parsed.Effects[0].Parameters;

            Assert.AreEqual(0.5f, parameters.GetPrimaryFloat(0f, "a"), 1e-5f);
            Assert.AreEqual(9f, parameters.GetFloat(9f, "f"), 1e-5f, "frequency should keep its default");
        }

        [Test]
        public void NamedAttributesAreReadIndependently()
        {
            ParsedText parsed = Parse("<wave a=0.5 f=8 w=0.25>x</wave>");
            TagParams parameters = parsed.Effects[0].Parameters;

            Assert.AreEqual(0.5f, parameters.GetFloat(0f, "a"), 1e-5f);
            Assert.AreEqual(8f, parameters.GetFloat(0f, "f"), 1e-5f);
            Assert.AreEqual(0.25f, parameters.GetFloat(0f, "w"), 1e-5f);
        }

        [Test]
        public void MissingAttributeFallsBackToTheSuppliedDefault()
        {
            ParsedText parsed = Parse("<wave>x</wave>");

            Assert.AreEqual(3f, parsed.Effects[0].Parameters.GetFloat(3f, "a"), 1e-5f);
        }

        [Test]
        public void PauseMarkerSitsBeforeTheNextCharacter()
        {
            ParsedText parsed = Parse("ab<pause=0.4>cd");

            Assert.AreEqual("abcd", parsed.Text);
            Assert.AreEqual(1, parsed.Pauses.Count);
            Assert.AreEqual(2, parsed.Pauses[0].CharIndex);
            Assert.AreEqual(0.4f, parsed.Pauses[0].Seconds, 1e-5f);
        }

        [Test]
        public void SpeedRangeCoversItsSpan()
        {
            ParsedText parsed = Parse("ab<speed=2>cd</speed>ef");

            Assert.AreEqual(1, parsed.Speeds.Count);
            Assert.AreEqual(2, parsed.Speeds[0].Start);
            Assert.AreEqual(4, parsed.Speeds[0].End);
            Assert.AreEqual(2f, parsed.Speeds[0].Multiplier, 1e-5f);
        }

        [Test]
        public void StrayAngleBracketStaysLiteralAndStillCounts()
        {
            // TextMeshPro renders "<3 b>" as text, so the parser has to count it as
            // text too or every later range slides left.
            ParsedText parsed = Parse("a <3 b> c<wave>d</wave>");

            Assert.AreEqual("a <3 b> c<wave>d</wave>".Replace("<wave>", "").Replace("</wave>", ""), parsed.Text);
            Assert.AreEqual(9, parsed.Effects[0].Start);
        }

        [Test]
        public void NoParseBlockIsCountedButNotParsed()
        {
            // "<b>" inside noparse is three visible characters, and any effect tag in
            // there is text rather than an instruction.
            ParsedText parsed = Parse("<noparse><b></noparse><wave>x</wave>");

            Assert.AreEqual("<noparse><b></noparse>x", parsed.Text);
            Assert.AreEqual(1, parsed.Effects.Count);
            Assert.AreEqual(3, parsed.Effects[0].Start);
        }

        [Test]
        public void ColourShorthandTagIsLeftToTextMeshPro()
        {
            ParsedText parsed = Parse("<#FF8800>ab<wave>c</wave>");

            Assert.AreEqual("<#FF8800>abc", parsed.Text);
            Assert.AreEqual(2, parsed.Effects[0].Start);
        }

        [Test]
        public void ParsingIsResetBetweenCalls()
        {
            var result = new ParsedText();
            TextEffectParser.Parse("<wave>abc</wave>", result);
            TextEffectParser.Parse("plain", result);

            Assert.AreEqual("plain", result.Text);
            Assert.IsEmpty(result.Effects);
        }
    }
}
