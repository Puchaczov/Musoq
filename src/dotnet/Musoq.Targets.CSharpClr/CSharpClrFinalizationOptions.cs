namespace Musoq.Targets.CSharpClr;

internal sealed record CSharpClrFinalizationOptions(bool EmitPdb) : TargetFinalizationOptions;
