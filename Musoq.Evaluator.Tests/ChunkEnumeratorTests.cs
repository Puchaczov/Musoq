using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ChunkEnumeratorTests
{
    [TestMethod]
    public void EnumerateAllTest()
    {
        var tokenSource = new CancellationTokenSource();
        var readChunks = new BlockingCollection<IReadOnlyList<IObjectResolver>>
        {
            new List<EntityResolver<string>>(),
            new List<EntityResolver<string>>
            {
                new("a", null, null),
                new("ab", null, null),
                new("abc", null, null),
                new("abcd", null, null)
            },
            new List<EntityResolver<string>>(),
            new List<EntityResolver<string>>
            {
                new("x", null, null),
                new("xs", null, null)
            },
            new List<EntityResolver<string>>()
        };


        var enumerator =
            new ChunkEnumerator<string>(readChunks,
                tokenSource.Token);

        tokenSource.Cancel();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Contexts[0]);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("ab", enumerator.Current.Contexts[0]);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("abc", enumerator.Current.Contexts[0]);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("abcd", enumerator.Current.Contexts[0]);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("x", enumerator.Current.Contexts[0]);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("xs", enumerator.Current.Contexts[0]);
        Assert.IsFalse(enumerator.MoveNext());
    }
}
