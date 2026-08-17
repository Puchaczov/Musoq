namespace Musoq.Evaluator.IR.Execution;

[Flags]
internal enum CteOutputFlags
{
    None = 0,
    OrderSensitive = 1,
    Aggregate = 2,
    Window = 4,
    SetOperation = 8,
    SideEffectSensitive = 16
}
