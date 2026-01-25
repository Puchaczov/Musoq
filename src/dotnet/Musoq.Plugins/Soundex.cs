using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Musoq.Plugins;

// The MIT License (MIT)
// Copyright (c) 2015 Ravinder Singh
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//Some variables had been renamed to fit my own coding standards.
//Original code can be found at: https://github.com/rsingh85/soundex

/// <summary>
///     Provides functionality to retrieve a soundex code for a given word.
/// </summary>
internal sealed partial class Soundex
{
    /// <summary>
    ///     Returns the soundex code for a specified word.
    /// </summary>
    /// <param name="word">Word to get the soundex for.</param>
    /// <returns>Soundex code for word.</returns>
    public string For(string? word)
    {
        const int maxSoundexCodeLength = 4;

        var soundexCode = new StringBuilder();
        var previousWasHorW = false;


        word = PunctuationRegex().Replace(
            word == null ? string.Empty : word.ToUpper(),
            string.Empty);

        if (string.IsNullOrEmpty(word))
            return string.Empty.PadRight(maxSoundexCodeLength, '0');


        soundexCode.Append(word.First());

        for (var i = 1; i < word.Length; i++)
        {
            var numberCharForCurrentLetter = GetCharNumberForLetter(word[i]);


            if (i == 1 &&
                numberCharForCurrentLetter == GetCharNumberForLetter(soundexCode[0]))
                continue;


            if (soundexCode.Length > 2 && previousWasHorW &&
                numberCharForCurrentLetter == soundexCode[^2])
                continue;


            if (soundexCode.Length > 0 &&
                numberCharForCurrentLetter == soundexCode[^1])
                continue;

            soundexCode.Append(numberCharForCurrentLetter);

            previousWasHorW = "HW".Contains(word[i]);
        }

        return soundexCode
            .Replace("0", string.Empty)
            .ToString()
            .PadRight(maxSoundexCodeLength, '0')
            .Substring(0, maxSoundexCodeLength);
    }

    private static char GetCharNumberForLetter(char letter)
    {
        if ("BFPV".Contains(letter)) return '1';
        if ("CGJKQSXZ".Contains(letter)) return '2';
        if ("DT".Contains(letter)) return '3';
        if ('L' == letter) return '4';
        if ("MN".Contains(letter)) return '5';
        if ('R' == letter) return '6';

        return '0';
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex PunctuationRegex();
}
