using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Schema.StructuralInputs;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator;

/// <summary>
/// Captures script parameters at the execution boundary. Structural values are
/// copied into Core-owned trees/arrays before a generated query can open a
/// datasource. The boundary is intentionally the only place where reflection
/// over host collection representations is used.
/// </summary>
public static class StructuralParameterSnapshotter
{
    /// <summary>
    /// Captures execution parameters through the generated provider. A
    /// structural executable without that provider is not a supported target;
    /// the boxed normalizer remains available only to direct host-boundary and
    /// metadata callers.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> CaptureForExecution(
        IReadOnlyList<ScriptParameterDefinition> definitions,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken,
        IStructuralParameterSnapshotProvider? generatedProvider)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(parameters);

        if (generatedProvider != null)
            return generatedProvider.CaptureParameterSnapshot(parameters, cancellationToken);

        if (definitions.Any(static definition => definition.Contract.IsStructured))
            throw new InvalidOperationException(
                "A target that executes structural parameters must implement " +
                "IStructuralParameterSnapshotProvider.");

        return Capture(definitions, parameters, cancellationToken);
    }

    public static IReadOnlyDictionary<string, object?> Capture(
        IReadOnlyList<ScriptParameterDefinition> definitions,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        return CaptureWithMetrics(definitions, parameters, cancellationToken).Values;
    }

    /// <summary>Captures owned parameter values and their measured structural usage.</summary>
    public static StructuralParameterSnapshot CaptureWithMetrics(
        IReadOnlyList<ScriptParameterDefinition> definitions,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(parameters);

        if (definitions.Count == 0)
            return new StructuralParameterSnapshot(
                parameters,
                new Dictionary<string, StructuralInputMetrics>(StringComparer.Ordinal),
                StructuralInputMetrics.Empty);

        var snapshot = new Dictionary<string, object?>(StringComparer.Ordinal);
        var budget = new NormalizationBudget(cancellationToken);

        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (parameters.TryGetValue(definition.Name, out var supplied))
            {
                snapshot.Add(definition.Name, NormalizeSupplied(definition, supplied, budget));
                continue;
            }

            if (definition.IsRequired)
                throw ScriptParameterBindingException.MissingRequired(definition.Name);

            // Keep scalar defaults in the generated parameter binder. Structural
            // defaults are copied here so their arrays/dictionaries cannot be
            // observed through a mutable host object on a later run.
            if (definition.Contract.IsStructured && definition.HasDefaultValue)
                snapshot.Add(
                    definition.Name,
                    NormalizeStructural(definition, definition.DefaultValue, budget, isConstantDefault: true));
        }

        ScriptParameterBinder.ValidateNoUnknownParameters(parameters, definitions.Select(static definition => definition.Name).ToArray());

        IReadOnlyDictionary<string, object?> values = snapshot.Count == 0
            ? ParameterSnapshot.EmptyReadOnly
            : new ReadOnlyDictionary<string, object?>(snapshot);
        return new StructuralParameterSnapshot(values, budget.MetricsByRoot, budget.AggregateMetrics);
    }

    private static object? NormalizeSupplied(
        ScriptParameterDefinition definition,
        object? supplied,
        NormalizationBudget budget)
    {
        if (!definition.Contract.IsStructured)
        {
            ValidateLegacyValue(definition, supplied);
            return supplied;
        }

        return NormalizeStructural(definition, supplied, budget, isConstantDefault: false);
    }

    private static object? NormalizeStructural(
        ScriptParameterDefinition definition,
        object? supplied,
        NormalizationBudget budget,
        bool isConstantDefault)
    {
        var descriptor = definition.Contract.StructuralType ??
                         throw new InvalidOperationException(
                             $"Structured parameter '{definition.Name}' is missing its shape descriptor.");
        StructuralInputMetrics constantMetrics = default;
        if (isConstantDefault &&
            !TryMeasureConstant(supplied, descriptor, out constantMetrics, out var measureError))
        {
            throw ScriptParameterBindingException.TypeMismatch(
                definition.Name,
                definition.ParameterType,
                supplied,
                new InvalidOperationException(measureError));
        }
        if (isConstantDefault &&
            !budget.CanFit(definition.Name, constantMetrics, definition.Contract.Limits, out var budgetError))
        {
            throw ScriptParameterBindingException.TypeMismatch(
                definition.Name,
                definition.ParameterType,
                supplied,
                new InvalidOperationException(budgetError));
        }

        var state = new NormalizationState(budget, definition.Contract.Limits, definition.Name);

        if (!TryNormalizeCore(
                supplied,
                descriptor,
                definition.Name,
                depth: 1,
                state,
                out var normalized,
                out var error))
        {
            if (supplied == null && !descriptor.IsNullable)
                throw ScriptParameterBindingException.NullNotAllowed(definition.Name, definition.ParameterType);

            throw ScriptParameterBindingException.TypeMismatch(
                definition.Name,
                definition.ParameterType,
                supplied,
                new InvalidOperationException(error));
        }

        return normalized;
    }

    private static void ValidateLegacyValue(ScriptParameterDefinition definition, object? value)
    {
        var expected = definition.ParameterType;
        var bindingExpected = expected.IsArray
            ? typeof(IReadOnlyList<>).MakeGenericType(expected.GetElementType() ?? typeof(object))
            : expected;
        if (value == null)
        {
            if (expected.IsArray || (expected.IsValueType && Nullable.GetUnderlyingType(expected) == null))
                throw ScriptParameterBindingException.NullNotAllowed(definition.Name, bindingExpected);
            return;
        }

        if (expected.IsArray)
        {
            var element = expected.GetElementType() ?? typeof(object);
            if (value is Array array)
            {
                if (array.Rank != 1)
                    throw ScriptParameterBindingException.TypeMismatch(
                        definition.Name,
                        bindingExpected,
                        value,
                        new InvalidCastException());
                return;
            }

            var readOnlyList = FindGenericInterface(value.GetType(), typeof(IReadOnlyList<>));
            if (readOnlyList == null || readOnlyList.GetGenericArguments()[0] != element)
                throw ScriptParameterBindingException.TypeMismatch(
                    definition.Name,
                    bindingExpected,
                    value,
                    new InvalidCastException());
            return;
        }

        if (!expected.IsInstanceOfType(value))
            throw ScriptParameterBindingException.TypeMismatch(
                definition.Name,
                expected,
                value,
                new InvalidCastException());
    }

    private static bool TryNormalizeCore(
        object? rawValue,
        StructuralTypeDescriptor descriptor,
        string path,
        int depth,
        NormalizationState state,
        out object? normalized,
        out string error)
    {
        state.Budget.CancellationToken.ThrowIfCancellationRequested();
        if (!state.Budget.TryReserveNode(state.RootName, depth, state.Limits, out error))
        {
            normalized = null;
            return false;
        }

        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            var scalar = rawValue is StructuralValue scalarTree && scalarTree.Kind == StructuralTypeKind.Scalar
                ? scalarTree.Scalar
                : rawValue;
            var conversion = ScriptValueConverter.ConvertValue(
                "Script parameter",
                path,
                descriptor.ToCanonicalSql(),
                descriptor.CoreValueType,
                scalar);
            if (!conversion.Success)
            {
                normalized = null;
                error = conversion.Error;
                return false;
            }

            if (conversion.Value is string text && !state.Budget.TryReserveString(state.RootName, text, state.Limits, out error))
            {
                normalized = null;
                return false;
            }

            normalized = conversion.Value;
            error = string.Empty;
            return true;
        }

        if (rawValue == null || rawValue is StructuralValue { Kind: StructuralTypeKind.Scalar, Scalar: null })
        {
            if (!descriptor.IsNullable)
            {
                normalized = null;
                error = $"Structural value '{path}' is null but type '{descriptor.ToCanonicalSql()}' is not nullable.";
                return false;
            }

            normalized = null;
            error = string.Empty;
            return true;
        }

        if (!state.TryEnter(rawValue, out error))
        {
            normalized = null;
            return false;
        }

        try
        {
            return descriptor.Kind == StructuralTypeKind.Record
                ? TryNormalizeRecord(rawValue, descriptor, path, depth, state, out normalized, out error)
                : TryNormalizeCollection(rawValue, descriptor, path, depth, state, out normalized, out error);
        }
        finally
        {
            state.Exit(rawValue);
        }
    }

    private static bool TryNormalizeRecord(
        object rawValue,
        StructuralTypeDescriptor descriptor,
        string path,
        int depth,
        NormalizationState state,
        out object? normalized,
        out string error)
    {
        if (!TryReadRecordFields(rawValue, out var suppliedFields, out error))
        {
            normalized = null;
            return false;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fields = new List<KeyValuePair<string, StructuralValue>>(suppliedFields.Count);
        foreach (var supplied in suppliedFields)
        {
            if (!seen.Add(supplied.Name))
            {
                normalized = null;
                error = $"Structural value '{path}' contains duplicate field '{supplied.Name}'.";
                return false;
            }

            var field = descriptor.Fields.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, supplied.Name, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                normalized = null;
                error = $"Structural value '{path}' contains unexpected field '{supplied.Name}'.";
                return false;
            }

            if (!TryNormalizeCore(
                    supplied.Value,
                    field.Type,
                    $"{path}.{field.Name}",
                    depth + 1,
                    state,
                    out var fieldValue,
                    out error))
            {
                normalized = null;
                return false;
            }

            fields.Add(new KeyValuePair<string, StructuralValue>(
                field.Name,
                ToStructuralValue(fieldValue, field.Type)));
        }

        foreach (var field in descriptor.Fields)
        {
            if (seen.Contains(field.Name))
                continue;

            if (field.Required)
            {
                normalized = null;
                error = $"Structural value '{path}' is missing required field '{field.Name}'.";
                return false;
            }

            if (!field.HasDefault)
                continue;

            // Declaration defaults are part of the normalized value. Keeping
            // them present is what makes declaration defaults win over a
            // different receiving-constructor default while still preserving
            // omission for fields without a declaration default.
            if (!TryMeasureConstant(field.Default.Value, field.Type, out var defaultMetrics, out error) ||
                !state.Budget.CanFit(state.RootName, defaultMetrics, state.Limits, out error) ||
                !TryNormalizeCore(
                    field.Default.Value,
                    field.Type,
                    $"{path}.{field.Name}",
                    depth + 1,
                    state,
                    out var defaultValue,
                    out error))
            {
                normalized = null;
                return false;
            }

            fields.Add(new KeyValuePair<string, StructuralValue>(
                field.Name,
                ToStructuralValue(defaultValue, field.Type)));
        }

        normalized = StructuralValue.FromRecord(fields);
        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeCollection(
        object rawValue,
        StructuralTypeDescriptor descriptor,
        string path,
        int depth,
        NormalizationState state,
        out object? normalized,
        out string error)
    {
        if (!TryReadStableList(rawValue, out var count, out var read, out error))
        {
            normalized = null;
            return false;
        }

        if (!state.Budget.TryReserveCollectionStorage(state.RootName, count, state.Limits, out error))
        {
            normalized = null;
            return false;
        }

        var elementType = descriptor.ElementType ??
                          throw new InvalidOperationException("A structural collection is missing its element type.");
        var array = Array.CreateInstance(elementType.CoreValueType, count);
        for (var index = 0; index < count; index++)
        {
            state.Budget.CancellationToken.ThrowIfCancellationRequested();
            object? item;
            try
            {
                item = read(index);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException cancellation)
            {
                throw cancellation;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                normalized = null;
                error = $"Structural value '{path}' could not read element {index}: {exception.Message}";
                return false;
            }

            if (!TryNormalizeCore(
                    item,
                    elementType,
                    $"{path}[{index}]",
                    depth + 1,
                    state,
                    out var element,
                    out error))
            {
                normalized = null;
                return false;
            }

            try
            {
                array.SetValue(element, index);
            }
            catch (Exception exception) when (exception is InvalidCastException or ArgumentException)
            {
                normalized = null;
                error = $"Structural value '{path}[{index}]' has an incompatible element type: {exception.Message}";
                return false;
            }
        }

        normalized = array;
        error = string.Empty;
        return true;
    }

    private static StructuralValue ToStructuralValue(object? value, StructuralTypeDescriptor descriptor)
    {
        if (value == null)
            return StructuralValue.FromScalar(null);

        return descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => StructuralValue.FromScalar(value),
            StructuralTypeKind.Record => value as StructuralValue ??
                throw new InvalidOperationException("A normalized record must be a StructuralValue."),
            StructuralTypeKind.Collection => StructuralValue.FromCollection(ToStructuralElements((Array)value, descriptor.ElementType!)),
            _ => throw new InvalidOperationException("Unknown structural value kind.")
        };
    }

    private static IEnumerable<StructuralValue> ToStructuralElements(
        Array values,
        StructuralTypeDescriptor elementType)
    {
        for (var index = 0; index < values.Length; index++)
            yield return ToStructuralValue(values.GetValue(index), elementType);
    }

    private static bool TryReadRecordFields(
        object rawValue,
        out IReadOnlyList<HostField> fields,
        out string error)
    {
        try
        {
            return TryReadRecordFieldsCore(rawValue, out fields, out error);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException cancellation)
        {
            throw cancellation;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            fields = [];
            error = $"Structural dictionary could not be read: {exception.Message}";
            return false;
        }
    }

    private static bool TryMeasureConstant(
        object? rawValue,
        StructuralTypeDescriptor descriptor,
        out StructuralInputMetrics metrics,
        out string error)
    {
        try
        {
            metrics = MeasureConstant(rawValue, descriptor, 1);
            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is OverflowException or InvalidOperationException)
        {
            metrics = StructuralInputMetrics.Empty;
            error = exception.Message;
            return false;
        }
    }

    private static StructuralInputMetrics MeasureConstant(
        object? rawValue,
        StructuralTypeDescriptor descriptor,
        int depth)
    {
        var metrics = new StructuralInputMetrics(depth, 1, 0);
        if (rawValue is StructuralValue scalar && scalar.Kind == StructuralTypeKind.Scalar)
            rawValue = scalar.Scalar;

        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            if (rawValue is string text)
                metrics = metrics with { StringBytes = checked((long)text.Length * sizeof(char)) };
            return metrics;
        }

        if (rawValue == null)
            return metrics;

        if (descriptor.Kind == StructuralTypeKind.Record &&
            rawValue is StructuralValue { Kind: StructuralTypeKind.Record } record)
        {
            foreach (var field in descriptor.Fields)
            {
                if (record.Fields.TryGetValue(field.Name, out var fieldValue))
                    metrics = MergeMetrics(metrics, MeasureConstant(fieldValue, field.Type, depth + 1));
                else if (field.HasDefault)
                    metrics = MergeMetrics(metrics, MeasureConstant(field.Default.Value, field.Type, depth + 1));
            }

            return metrics;
        }

        if (descriptor.Kind == StructuralTypeKind.Collection)
        {
            IEnumerable<object?> elements;
            if (rawValue is StructuralValue { Kind: StructuralTypeKind.Collection } collection)
                elements = collection.Elements.Cast<object?>();
            else if (rawValue is Array array)
                elements = array.Cast<object?>();
            else
                throw new InvalidOperationException("A constant structural collection has an unsupported representation.");
            var elementType = descriptor.ElementType ?? throw new InvalidOperationException("A structural collection is missing its element type.");
            foreach (var element in elements)
                metrics = MergeMetrics(metrics, MeasureConstant(element, elementType, depth + 1));
        }

        return metrics;
    }

    private static StructuralInputMetrics MergeMetrics(
        StructuralInputMetrics left,
        StructuralInputMetrics right) =>
        new(
            Math.Max(left.MaxDepth, right.MaxDepth),
            checked(left.NodeCount + right.NodeCount),
            checked(left.StringBytes + right.StringBytes));

    private static bool TryReadRecordFieldsCore(
        object rawValue,
        out IReadOnlyList<HostField> fields,
        out string error)
    {
        if (rawValue is StructuralValue { Kind: StructuralTypeKind.Record } record)
        {
            fields = record.Fields.Select(static field => new HostField(field.Key, field.Value)).ToArray();
            error = string.Empty;
            return true;
        }

        if (rawValue is IDictionary dictionary)
        {
            var result = new List<HostField>(dictionary.Count);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    fields = [];
                    error = "Structural records require string dictionary keys.";
                    return false;
                }

                result.Add(new HostField(key, entry.Value));
            }

            fields = result;
            error = string.Empty;
            return true;
        }

        var dictionaryInterface = FindGenericInterface(rawValue.GetType(), typeof(IReadOnlyDictionary<,>)) ??
                                   FindGenericInterface(rawValue.GetType(), typeof(IDictionary<,>));
        if (dictionaryInterface == null || dictionaryInterface.GetGenericArguments()[0] != typeof(string) ||
            rawValue is not IEnumerable entries)
        {
            fields = [];
            error = $"Structural value must be a Core record or a string-keyed dictionary, but received '{rawValue.GetType().FullName}'.";
            return false;
        }

        var list = new List<HostField>();
        foreach (var entry in entries)
        {
            if (entry == null)
            {
                fields = [];
                error = "Structural dictionary enumeration produced a null entry.";
                return false;
            }

            var entryType = entry.GetType();
            var keyProperty = entryType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            var valueProperty = entryType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            if (keyProperty?.PropertyType != typeof(string) || valueProperty == null)
            {
                fields = [];
                error = "Structural records require string-keyed dictionaries.";
                return false;
            }

            list.Add(new HostField(
                (string)keyProperty.GetValue(entry)!,
                valueProperty.GetValue(entry)));
        }

        fields = list;
        error = string.Empty;
        return true;
    }

    private static bool TryReadStableList(
        object rawValue,
        out int count,
        out Func<int, object?> read,
        out string error)
    {
        try
        {
            return TryReadStableListCore(rawValue, out count, out read, out error);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is OperationCanceledException cancellation)
        {
            throw cancellation;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            count = 0;
            read = null!;
            error = $"Structural collection could not be read: {exception.Message}";
            return false;
        }
    }

    private static bool TryReadStableListCore(
        object rawValue,
        out int count,
        out Func<int, object?> read,
        out string error)
    {
        if (rawValue is StructuralValue { Kind: StructuralTypeKind.Collection } collection)
        {
            count = collection.Elements.Count;
            read = index => collection.Elements[index];
            error = string.Empty;
            return true;
        }

        if (rawValue is Array array)
        {
            if (array.Rank != 1)
            {
                count = 0;
                read = null!;
                error = "Structural collections must be one-dimensional.";
                return false;
            }

            count = array.Length;
            read = index => array.GetValue(index);
            error = string.Empty;
            return true;
        }

        // Non-generic IList/ArrayList values are intentionally rejected. A LINQ
        // iterator can expose list interfaces internally while still being lazy;
        // its enumerable/enumerator identity is outside the structural input ABI.
        if (rawValue is IEnumerator)
        {
            count = 0;
            read = null!;
            error = "Lazy-only structural enumerables are not supported.";
            return false;
        }

        // The host ABI accepts arrays and stable generic indexed collections.

        var readOnlyListInterface = FindGenericInterface(rawValue.GetType(), typeof(IReadOnlyList<>));
        if (readOnlyListInterface != null)
        {
            var countProperty = FindCollectionCountProperty(rawValue.GetType(), readOnlyListInterface);
            var itemProperty = readOnlyListInterface.GetProperty("Item");
            if (countProperty?.GetValue(rawValue) is int readOnlyCount && itemProperty != null)
            {
                count = readOnlyCount;
                read = index => itemProperty.GetValue(rawValue, [index]);
                error = string.Empty;
                return true;
            }
        }

        var listInterface = FindGenericInterface(rawValue.GetType(), typeof(IList<>));
        if (listInterface != null)
        {
            var countProperty = FindCollectionCountProperty(rawValue.GetType(), listInterface);
            var itemProperty = listInterface.GetProperty("Item");
            if (countProperty?.GetValue(rawValue) is int listCount && itemProperty != null)
            {
                count = listCount;
                read = index => itemProperty.GetValue(rawValue, [index]);
                error = string.Empty;
                return true;
            }
        }

        count = 0;
        read = null!;
        error = $"Structural collections require an array, List<T>, or IReadOnlyList<T>; lazy-only IEnumerable<T> values are not supported (received '{rawValue.GetType().FullName}').";
        return false;
    }

    private static PropertyInfo? FindCollectionCountProperty(Type concreteType, Type listInterface)
    {
        return listInterface.GetProperty("Count") ??
               listInterface.GetInterfaces()
                   .Select(static @interface => @interface.GetProperty("Count"))
                   .FirstOrDefault(static property => property != null) ??
               concreteType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
    private static Type? FindGenericInterface(Type type, Type genericDefinition)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition)
            return type;

        return type.GetInterfaces()
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == genericDefinition);
    }

    private readonly record struct HostField(string Name, object? Value);

    private sealed class NormalizationState
    {
        private readonly HashSet<object> _ancestors = new(ReferenceEqualityComparer.Instance);

        public NormalizationState(NormalizationBudget budget, StructuralInputLimits limits, string rootName)
        {
            Budget = budget;
            Limits = limits;
            RootName = rootName;
        }

        public NormalizationBudget Budget { get; }
        public StructuralInputLimits Limits { get; }
        public string RootName { get; }

        public bool TryEnter(object value, out string error)
        {
            if (_ancestors.Add(value))
            {
                error = string.Empty;
                return true;
            }

            error = "Structural input contains a cyclic host reference.";
            return false;
        }

        public void Exit(object value) => _ancestors.Remove(value);
    }

    private sealed class NormalizationBudget
    {
        private long _nodes;
        private long _stringBytes;
        private int _maxDepth;
        private readonly Dictionary<string, RootMetrics> _roots = new(StringComparer.Ordinal);

        public NormalizationBudget(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public IReadOnlyDictionary<string, StructuralInputMetrics> MetricsByRoot =>
            _roots.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.ToMetrics(),
                StringComparer.Ordinal);

        public StructuralInputMetrics AggregateMetrics => new(_maxDepth, _nodes, _stringBytes);

        public bool TryReserveNode(string rootName, int depth, StructuralInputLimits limits, out string error)
        {
            CancellationToken.ThrowIfCancellationRequested();
            if (depth > limits.MaxDepth)
            {
                error = $"Structural input exceeds maximum depth {limits.MaxDepth}.";
                return false;
            }

            var root = GetRoot(rootName);
            if (root.NodeCount >= limits.MaxNodes || _nodes >= StructuralInputLimits.Default.MaxNodes)
            {
                error = $"Structural input exceeds maximum value nodes {limits.MaxNodes}.";
                return false;
            }

            _nodes++;
            root.NodeCount++;
            root.MaxDepth = Math.Max(root.MaxDepth, depth);
            _maxDepth = Math.Max(_maxDepth, depth);
            error = string.Empty;
            return true;
        }

        public bool TryReserveCollectionStorage(string rootName, int count, StructuralInputLimits limits, out string error)
        {
            if (count < 0)
            {
                error = "Structural collection count cannot be negative.";
                return false;
            }

            // The per-element node reservations are performed before each
            // element is stored. This check prevents a clearly impossible
            // allocation without charging internal array copies twice.
            var root = GetRoot(rootName);
            if (count > limits.MaxNodes - root.NodeCount ||
                count > StructuralInputLimits.Default.MaxNodes - _nodes)
            {
                error = "Structural input exceeds maximum value nodes.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryReserveString(string rootName, string text, StructuralInputLimits limits, out string error)
        {
            try
            {
                var bytes = checked((long)text.Length * sizeof(char));
                _stringBytes = checked(_stringBytes + bytes);
                var root = GetRoot(rootName);
                root.StringBytes = checked(root.StringBytes + bytes);
                if (_stringBytes > StructuralInputLimits.Default.MaxStringBytes || root.StringBytes > limits.MaxStringBytes)
                {
                    error = $"Structural input exceeds maximum string payload {limits.MaxStringBytes} bytes.";
                    return false;
                }
            }
            catch (OverflowException)
            {
                error = "Structural input string payload overflowed the configured limit.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool CanFit(
            string rootName,
            StructuralInputMetrics metrics,
            StructuralInputLimits limits,
            out string error)
        {
            var root = GetRoot(rootName);
            var maxDepth = metrics.NodeCount == 0 ? 0 : metrics.MaxDepth;
            if (maxDepth > limits.MaxDepth ||
                maxDepth > StructuralInputLimits.Default.MaxDepth ||
                metrics.NodeCount > limits.MaxNodes - root.NodeCount ||
                metrics.NodeCount > StructuralInputLimits.Default.MaxNodes - _nodes ||
                metrics.StringBytes > limits.MaxStringBytes - root.StringBytes ||
                metrics.StringBytes > StructuralInputLimits.Default.MaxStringBytes - _stringBytes)
            {
                error = $"Structural input exceeds a configured resource limit for '{rootName}'.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private RootMetrics GetRoot(string rootName)
        {
            if (!_roots.TryGetValue(rootName, out var root))
            {
                root = new RootMetrics();
                _roots.Add(rootName, root);
            }

            return root;
        }

        private sealed class RootMetrics
        {
            public int MaxDepth { get; set; }
            public long NodeCount { get; set; }
            public long StringBytes { get; set; }

            public StructuralInputMetrics ToMetrics() => new(MaxDepth, NodeCount, StringBytes);
        }
    }
}
