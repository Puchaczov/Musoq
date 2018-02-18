using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class ChunkEnumeratorTests
    {
        [TestMethod]
        public void EnumerateAllTest()
        {
            var tokenSource = new CancellationTokenSource();
            var readedChunks = new BlockingCollection<IReadOnlyList<EntityResolver<string>>>
            {
                new List<EntityResolver<string>>(),
                new List<EntityResolver<string>>()
                {
                    new EntityResolver<string>("a", null, null),
                    new EntityResolver<string>("ab", null, null),
                    new EntityResolver<string>("abc", null, null),
                    new EntityResolver<string>("abcd", null, null)
                },
                new List<EntityResolver<string>>(),
                new List<EntityResolver<string>>()
                {
                    new EntityResolver<string>("x", null, null),
                    new EntityResolver<string>("xs", null, null)
                },
                new List<EntityResolver<string>>()
            };


            var enumerator =
                new ChunkEnumerator<string>(readedChunks,
                    tokenSource.Token);

            tokenSource.Cancel();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("a", enumerator.Current.Context);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("ab", enumerator.Current.Context);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("abc", enumerator.Current.Context);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("abcd", enumerator.Current.Context);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("x", enumerator.Current.Context);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("xs", enumerator.Current.Context);
            Assert.IsFalse(enumerator.MoveNext());
        }
    }
}
