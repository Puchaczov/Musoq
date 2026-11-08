using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionWindowQueryTestsFixtures
{

    public sealed class CleanupLibrary : LibraryBase
    {
        public static int OrderKeyCalls;
        [BindableMethod, NonDeterministic]
        public static int CaptureOrderKey(int value)
        {
            Interlocked.Increment(ref OrderKeyCalls);
            return value;
        }
    }

}
