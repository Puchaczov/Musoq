namespace Musoq.Evaluator.IR.Planning;

[Flags]
internal enum CteOutputCharacteristics
{
    None = 0,
    OrderSensitive = 1,
    Aggregate = 2,
    Window = 4,
    SetOperation = 8,
    SideEffectSensitive = 16
}
