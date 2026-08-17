using System.Collections.Frozen;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static readonly char[] Separator = ['\n'];

    private static readonly ConcurrentDictionary<string, Regex> StringRegexCache = new();

    private static readonly FrozenDictionary<char, string> MorseCodeMap = new Dictionary<char, string>()
    {
        { 'A', ".-" }, { 'B', "-..." }, { 'C', "-.-." }, { 'D', "-.." }, { 'E', "." },
        { 'F', "..-." }, { 'G', "--." }, { 'H', "...." }, { 'I', ".." }, { 'J', ".---" },
        { 'K', "-.-" }, { 'L', ".-.." }, { 'M', "--" }, { 'N', "-." }, { 'O', "---" },
        { 'P', ".--." }, { 'Q', "--.-" }, { 'R', ".-." }, { 'S', "..." }, { 'T', "-" },
        { 'U', "..-" }, { 'V', "...-" }, { 'W', ".--" }, { 'X', "-..-" }, { 'Y', "-.--" },
        { 'Z', "--.." }, { '0', "-----" }, { '1', ".----" }, { '2', "..---" }, { '3', "...--" },
        { '4', "....-" }, { '5', "....." }, { '6', "-...." }, { '7', "--..." }, { '8', "---.." },
        { '9', "----." }, { ' ', "/" }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, char> ReverseMorseCodeMap =
        MorseCodeMap.ToFrozenDictionary(kvp => kvp.Value, kvp => kvp.Key);

    private readonly Soundex _soundex = new();
}
