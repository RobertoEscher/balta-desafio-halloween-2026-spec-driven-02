using System.Security.Cryptography;

namespace Shortener.Api.Services;

public sealed class LinkCodeGenerator : ILinkCodeGenerator
{
    public const int CodeLength = 7;
    public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

    public string Generate()
    {
        return string.Create(CodeLength, Alphabet, static (code, alphabet) =>
        {
            for (var index = 0; index < code.Length; index++)
            {
                code[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }
        });
    }
}
