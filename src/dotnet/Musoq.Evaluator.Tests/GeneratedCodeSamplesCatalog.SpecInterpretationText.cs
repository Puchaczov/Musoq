namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSpecificationTextInterpretationSamples()
    {
        return
        [
            TextInterpretationSample(
                "Q314_SpecTextPatternsLiteralsTokens",
                @"
                    text Patterned {
                        Ip: pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',
                        Separator: literal ':',
                        User: token,
                        _: whitespace+,
                        Message: rest
                    };
                    select p.Ip, p.User, p.Message
                    from #test.lines() f
                    cross apply Parse<Patterned>(f.Line) p"),
            TextInterpretationSample(
                "Q315_SpecTextUntilBetweenCharsRestNested",
                @"
                    text Structured {
                        Prefix: chars[2],
                        Tag: between '[' ']' nested,
                        Key: until '=',
                        Value: rest
                    };
                    select s.Prefix, s.Tag, s.Key, s.Value
                    from #test.lines() f
                    cross apply Parse<Structured>(f.Line) s"),
            TextInterpretationSample(
                "Q316_SpecTextOptionalRepeatWhitespaceModifiers",
                @"
                    text Item { Value: token };
                    text Document {
                        Prefix: token,
                        _: whitespace*,
                        OptionalCode: optional pattern '[0-9]+',
                        _: whitespace?,
                        Items: repeat Item until end,
                        Tail: rest trim
                    };
                    select d.Prefix, d.OptionalCode, i.Value, d.Tail
                    from #test.lines() f
                    cross apply Parse<Document>(f.Line) d
                    cross apply d.Items i"),
            TextInterpretationSample(
                "Q317_SpecTextSwitch",
                @"
                    text Comment { _: literal '#', Text: rest };
                    text KeyValue { Key: until '=', Value: rest };
                    text ConfigLine {
                        Content: switch {
                            pattern '#' => Comment,
                            _ => KeyValue
                        }
                    };
                    select c.Content.Key, c.Content.Text
                    from #test.lines() f
                    cross apply Parse<ConfigLine>(f.Line) c") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            },
            TextInterpretationSample(
                "Q318_SpecTextTryParseAndPartialParse",
                @"
                    text KeyValue {
                        Key: until '=',
                        Value: rest
                    };
                    select t.Key, t.Value, p.ErrorField, p.ErrorMessage, p.BytesConsumed
                    from #test.lines() f
                    outer apply TryParse<KeyValue>(f.Line) t
                    cross apply PartialParse<KeyValue>(f.Line) p") with
            {
                CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false)
                    .WithTableResultMaterialization()
                    .WithStabilityAwareScalarReuse()
            }
        ];
    }
}
