using Codeuctivity.OpenXmlPowerTools;
using SkiaSharp;
using Xunit;

namespace Codeuctivity.Tests
{
    public class ColorParserTests
    {
        [Theory]
        [InlineData("red", 255, 0, 0)]
        [InlineData("#00FF00", 0, 255, 0)]
        public void ShouldParseColors(string input, byte r, byte g, byte b)
        {
            var result = ColorParser.FromName(input);
            Assert.Equal(r, result.Red);
            Assert.Equal(g, result.Green);
            Assert.Equal(b, result.Blue);
        }
    }
}
