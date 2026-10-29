using Backend.Domain.Common;

namespace Backend.UnitTests.Common;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" \t\u00A0 ", "")]
    [InlineData("  Rua   das  Flores ", "Rua das Flores")]
    [InlineData("Rua\u00A0das\u2009Flores", "Rua das Flores")]
    [InlineData("Rua\r\ndas\nFlores\u2028Azuis", "Rua das Flores Azuis")]
    [InlineData("Rua\u0000 das\u0007 Flores", "Rua das Flores")]
    [InlineData("\uFEFFRua\u200B das\u200E Flores\u200D", "Rua das Flores")]
    public void NormalizeLine_WithMessyText_ShouldReturnCleanSingleLine(string? value, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeLine(value));
    }

    [Fact]
    public void NormalizeLine_WithDecomposedAccents_ShouldComposeThem()
    {
        var normalized = TextNormalizer.NormalizeLine("Sa\u0303o Joa\u0303o da Ac\u0327ai\u0301");

        Assert.Equal("S\u00E3o Jo\u00E3o da A\u00E7a\u00ED", normalized);
    }

    [Fact]
    public void NormalizeLine_WithLoneSurrogate_ShouldRemoveItAndKeepValidPairs()
    {
        var normalized = TextNormalizer.NormalizeLine("Casa \uD800bonita \uD83C\uDFE1");

        Assert.Equal("Casa bonita \uD83C\uDFE1", normalized);
    }

    [Fact]
    public void NormalizeMultiline_WithParagraphs_ShouldKeepLineBreaksAndCollapseBlankLines()
    {
        var normalized = TextNormalizer.NormalizeMultiline("  Casa ampla.  \r\n\r\n\r\n\r\n  Quintal\u00A0grande.\r\nPerto do centro.\n\n");

        Assert.Equal("Casa ampla.\n\nQuintal grande.\nPerto do centro.", normalized);
    }

    [Fact]
    public void NormalizeMultiline_WithComposedEmoji_ShouldKeepZeroWidthJoiner()
    {
        var family = "\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67";

        var normalized = TextNormalizer.NormalizeMultiline($"Ideal para {family}\u0000");

        Assert.Equal($"Ideal para {family}", normalized);
    }
}
