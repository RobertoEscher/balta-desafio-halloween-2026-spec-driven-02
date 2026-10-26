using Shortener.Api.Services;

namespace Shortener.Tests.Services;

public sealed class LinkCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSevenCharactersFromAllowedAlphabet()
    {
        var generator = new LinkCodeGenerator();

        var code = generator.Generate();

        Assert.Equal(7, code.Length);
        Assert.All(code, character => Assert.Contains(character, LinkCodeGenerator.Alphabet));
    }
}
