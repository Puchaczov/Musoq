# SOLID Architecture Diagram

## Class Relationship Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    LibraryBase (Facade)                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  [BindableMethod] Public API                         │  │
│  │  - TryConvertToInt32Strict()                         │  │
│  │  - TryConvertToInt64Comparison()                     │  │
│  │  - InternalApplyMultiplyOperator()                   │  │
│  │  - InternalGreaterThanOperator()                     │  │
│  │  - etc...                                            │  │
│  └───────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           │ delegates to                     │
│                           ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Static Dependencies (Dependency Injection)           │  │
│  │  - StrictConverter: ITypeConverter                    │  │
│  │  - ComparisonConverter: ITypeConverter                │  │
│  │  - NumericOnlyConverter: ITypeConverter               │  │
│  │  - RuntimeOperators: IRuntimeOperators                │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                          │                           │
        ┌─────────────────┴───────────┬──────────────┴──────────┐
        │                             │                          │
        ▼                             ▼                          ▼
┌───────────────────┐      ┌──────────────────────┐   ┌────────────────────┐
│  ITypeConverter   │      │  IRuntimeOperators   │   │   BuildMetadata    │
│  (Interface)      │      │  (Interface)         │   │   Visitor          │
├───────────────────┤      ├──────────────────────┤   │   (Consumer)       │
│ + TryConvertTo    │      │ + Add()              │   │                    │
│   Int32()         │      │ + Subtract()         │   │ Uses via           │
│ + TryConvertTo    │      │ + Multiply()         │   │ nameof() +         │
│   Int64()         │      │ + Divide()           │   │ reflection to      │
│ + TryConvertTo    │      │ + Modulo()           │   │ LibraryBase        │
│   Decimal()       │      │ + GreaterThan()      │   │ methods            │
│ + TryConvertTo    │      │ + LessThan()         │   └────────────────────┘
│   Double()        │      │ + Equal()            │
└───────────────────┘      │ + etc...             │
        △                  └──────────────────────┘
        │                           △
        │                           │
        │ implements                │ implements
        │                           │
   ┌────┴────┬────────────┬─────────┴──────────────────┐
   │         │            │                            │
   ▼         ▼            ▼                            ▼
┌──────┐ ┌────────┐ ┌──────────┐      ┌─────────────────────────────┐
│Strict│ │Compare │ │ Numeric  │      │TypePreserving               │
│Type  │ │Type    │ │ Only     │      │RuntimeOperators             │
│Conv. │ │Conv.   │ │ Type     │      ├─────────────────────────────┤
│      │ │        │ │ Conv.    │      │ Constructor:                │
│Strict│ │Lossy   │ │          │      │  + numericConverter         │
│mode  │ │for     │ │ Rejects  │      │  + comparisonConverter      │
│No    │ │>,<,>=, │ │ strings  │      │  + strictConverter          │
│preci-│ │<=      │ │          │      │                             │
│sion  │ │        │ │ For      │      │ Uses:                       │
│loss  │ │Allows  │ │ arith-   │      │  - NumericOnlyConverter     │
│      │ │preci-  │ │ metic    │      │    for arithmetic           │
│For   │ │sion    │ │          │      │  - ComparisonConverter      │
│=, != │ │loss    │ │          │      │    for comparisons          │
└──────┘ └────────┘ └──────────┘      │  - StrictConverter          │
                                       │    for equality             │
                                       │                             │
                                       │ Implements type-preserving  │
                                       │ logic (decimal>double>long) │
                                       └─────────────────────────────┘
```

## Strategy Pattern Flow

### Type Conversion Example
```
User Query: SELECT Value WHERE Value > 5
                                    │
                                    ▼
┌────────────────────────────────────────────────────────┐
│ Visitor detects: object > int                          │
│ Creates: InternalGreaterThanOperator(object, object)   │
└────────────────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────────┐
│ LibraryBase.InternalGreaterThanOperator(left, right)  │
│   → RuntimeOperators.GreaterThan(left, right)          │
└────────────────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────────┐
│ TypePreservingRuntimeOperators.GreaterThan()           │
│   → ComparisonConverter.TryConvertToDecimal(left)      │
│   → ComparisonConverter.TryConvertToDecimal(right)     │
│   → return left > right                                │
└────────────────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────────┐
│ ComparisonTypeConverter.TryConvertToDecimal()          │
│   → Allows lossy conversion                            │
│   → Returns decimal?                                   │
└────────────────────────────────────────────────────────┘
```

### Runtime Arithmetic Example
```
User Query: SELECT Value * 2 WHERE Value IS NOT NULL
                      │
                      ▼
┌────────────────────────────────────────────────────────┐
│ Visitor detects: object * int                          │
│ Creates: InternalApplyMultiplyOperator(object, object) │
└────────────────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────────┐
│ LibraryBase.InternalApplyMultiplyOperator(left, right) │
│   → RuntimeOperators.Multiply(left, right)             │
└────────────────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────────┐
│ TypePreservingRuntimeOperators.Multiply()              │
│   1. Determine target type (long/double/decimal)       │
│   2. NumericOnlyConverter.TryConvertToX(left)          │
│   3. NumericOnlyConverter.TryConvertToX(right)         │
│   4. return left * right in appropriate type           │
└────────────────────────────────────────────────────────┘
                    │
                    ▼
┌────────────────────────────────────────────────────────┐
│ NumericOnlyTypeConverter.TryConvertToX()               │
│   → Rejects strings                                    │
│   → Strict precision preservation                      │
│   → Returns long?, double?, or decimal?                │
└────────────────────────────────────────────────────────┘
```

## Dependency Injection Points

### Current Implementation (Static Singletons)
```csharp
private static readonly ITypeConverter StrictConverter = new StrictTypeConverter();
private static readonly ITypeConverter ComparisonConverter = new ComparisonTypeConverter();
private static readonly ITypeConverter NumericOnlyConverter = new NumericOnlyTypeConverter();
private static readonly IRuntimeOperators RuntimeOperators = new TypePreservingRuntimeOperators(
    NumericOnlyConverter,
    ComparisonConverter,
    StrictConverter);
```

### Future Enhancement (Constructor Injection)
```csharp
public partial class LibraryBase
{
    private readonly ITypeConverter _strictConverter;
    private readonly ITypeConverter _comparisonConverter;
    private readonly ITypeConverter _numericOnlyConverter;
    private readonly IRuntimeOperators _runtimeOperators;

    public LibraryBase(
        ITypeConverter strictConverter,
        ITypeConverter comparisonConverter,
        ITypeConverter numericOnlyConverter,
        IRuntimeOperators runtimeOperators)
    {
        _strictConverter = strictConverter;
        _comparisonConverter = comparisonConverter;
        _numericOnlyConverter = numericOnlyConverter;
        _runtimeOperators = runtimeOperators;
    }
}
```

## SOLID Principles Applied

### ✅ Single Responsibility Principle (SRP)
- **StrictTypeConverter**: Only handles strict conversions
- **ComparisonTypeConverter**: Only handles comparison conversions
- **NumericOnlyTypeConverter**: Only handles numeric-only conversions
- **TypePreservingRuntimeOperators**: Only handles runtime operations
- **LibraryBase**: Only acts as facade/coordinator

### ✅ Open/Closed Principle (OCP)
- Open for extension: Can add new ITypeConverter implementations
- Closed for modification: Existing classes don't need changes
- Example: Add `FastUnsafeTypeConverter` without touching existing code

### ✅ Liskov Substitution Principle (LSP)
- Any `ITypeConverter` can replace another
- Any `IRuntimeOperators` can replace another
- Behavior remains consistent

### ✅ Interface Segregation Principle (ISP)
- `ITypeConverter`: 4 focused methods
- `IRuntimeOperators`: 11 operator methods
- No fat interfaces forcing unused dependencies

### ✅ Dependency Inversion Principle (DIP)
- High-level (LibraryBase) depends on abstraction (ITypeConverter, IRuntimeOperators)
- Low-level (concrete converters) depend on abstractions
- Both depend on interfaces, not concretions
```
