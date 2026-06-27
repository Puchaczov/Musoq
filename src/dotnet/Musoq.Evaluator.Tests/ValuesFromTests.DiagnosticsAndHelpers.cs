using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_WithMissingField_ShouldThrow()
    {
        const string query = @"
from values {
    { Name: 'Newtonsoft.Json', Approved: true },
    { Name: 'Legacy.Package' }
} packages
select packages.Name";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "missing field 'Approved'");
        MusoqExceptionAssertions.AssertHasGuidance(ex);
    }

    [TestMethod]
    public void ValuesSource_WithDuplicateField_ShouldThrow()
    {
        const string query = @"
from values {
    { Name: 'Newtonsoft.Json', Name: 'Legacy.Package' }
} packages
select packages.Name";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "duplicate field 'Name'");
        MusoqExceptionAssertions.AssertHasGuidance(ex);
    }

    [TestMethod]
    public void ValuesSource_WithIncompatibleFieldTypes_ShouldThrow()
    {
        const string query = @"
from values {
    { Name: 'Newtonsoft.Json', Score: 10 },
    { Name: 'Legacy.Package', Score: 'high' }
} packages
select packages.Name";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "VALUES field 'Score' mixes incompatible types");
        MusoqExceptionAssertions.AssertHasGuidance(ex);
    }

    [TestMethod]
    public void ValuesSource_WithMethodExpression_ShouldThrow()
    {
        const string query = @"
from values {
    { Name: ToUpper('Newtonsoft.Json') }
} packages
select packages.Name";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "must be a constant literal expression");
        MusoqExceptionAssertions.AssertHasGuidance(ex);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>();
    }

    private static (string Name, string Literal, Type Type)[] NumericLiteralCases()
    {
        return
        [
            ("SByte", "1b", typeof(sbyte)),
            ("Byte", "2ub", typeof(byte)),
            ("Short", "3s", typeof(short)),
            ("UShort", "4us", typeof(ushort)),
            ("Int", "5", typeof(int)),
            ("UInt", "6ui", typeof(uint)),
            ("Long", "7l", typeof(long)),
            ("ULong", "8ul", typeof(ulong)),
            ("Decimal", "9d", typeof(decimal))
        ];
    }

    private static bool TryResolveExpectedValuesNumericColumnType(Type left, Type right, out Type? expectedType)
    {
        if (left == right)
        {
            expectedType = left;
            return true;
        }

        if (left == typeof(decimal) || right == typeof(decimal))
        {
            expectedType = typeof(decimal);
            return true;
        }

        if (left == typeof(ulong) || right == typeof(ulong))
        {
            if (IsSignedIntegerType(left) || IsSignedIntegerType(right))
            {
                expectedType = null;
                return false;
            }

            expectedType = typeof(ulong);
            return true;
        }

        if (left == typeof(long) || right == typeof(long))
        {
            expectedType = typeof(long);
            return true;
        }

        if (left == typeof(uint) || right == typeof(uint))
        {
            expectedType = typeof(uint);
            return true;
        }

        expectedType = typeof(int);
        return true;
    }

    private static bool IsSignedIntegerType(Type type)
    {
        return type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(int) ||
               type == typeof(long);
    }
}
