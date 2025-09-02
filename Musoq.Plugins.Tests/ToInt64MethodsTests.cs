using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class ToInt64MethodsTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void ToInt64_FromString_ShouldReturnLong()
        {
            var result = Library.ToInt64("9223372036854775807");
            Assert.AreEqual(9223372036854775807L, result);
        }

        [TestMethod]
        public void ToInt64_FromInvalidString_ShouldReturnNull()
        {
            var result = Library.ToInt64("invalid");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromNullString_ShouldReturnNull()
        {
            var result = Library.ToInt64((string?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromEmptyString_ShouldReturnNull()
        {
            var result = Library.ToInt64("");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromNegativeString_ShouldReturnNegativeLong()
        {
            var result = Library.ToInt64("-9223372036854775808");
            Assert.AreEqual(-9223372036854775808L, result);
        }

        [TestMethod]
        public void ToInt64_FromByte_ShouldReturnLong()
        {
            var result = Library.ToInt64((byte?)255);
            Assert.AreEqual(255L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullByte_ShouldReturnNull()
        {
            var result = Library.ToInt64((byte?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromSByte_ShouldReturnLong()
        {
            var result = Library.ToInt64((sbyte?)127);
            Assert.AreEqual(127L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullSByte_ShouldReturnNull()
        {
            var result = Library.ToInt64((sbyte?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromNegativeSByte_ShouldReturnNegativeLong()
        {
            var result = Library.ToInt64((sbyte?)-128);
            Assert.AreEqual(-128L, result);
        }

        [TestMethod]
        public void ToInt64_FromShort_ShouldReturnLong()
        {
            var result = Library.ToInt64((short?)32767);
            Assert.AreEqual(32767L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullShort_ShouldReturnNull()
        {
            var result = Library.ToInt64((short?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromNegativeShort_ShouldReturnNegativeLong()
        {
            var result = Library.ToInt64((short?)-32768);
            Assert.AreEqual(-32768L, result);
        }

        [TestMethod]
        public void ToInt64_FromUShort_ShouldReturnLong()
        {
            var result = Library.ToInt64((ushort?)65535);
            Assert.AreEqual(65535L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullUShort_ShouldReturnNull()
        {
            var result = Library.ToInt64((ushort?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromInt_ShouldReturnLong()
        {
            var result = Library.ToInt64((int?)2147483647);
            Assert.AreEqual(2147483647L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullInt_ShouldReturnNull()
        {
            var result = Library.ToInt64((int?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromNegativeInt_ShouldReturnNegativeLong()
        {
            var result = Library.ToInt64((int?)-2147483648);
            Assert.AreEqual(-2147483648L, result);
        }

        [TestMethod]
        public void ToInt64_FromUInt_ShouldReturnLong()
        {
            var result = Library.ToInt64((uint?)4294967295);
            Assert.AreEqual(4294967295L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullUInt_ShouldReturnNull()
        {
            var result = Library.ToInt64((uint?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromLong_ShouldReturnSameLong()
        {
            var result = Library.ToInt64((long?)1234567890123456789L);
            Assert.AreEqual(1234567890123456789L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullLong_ShouldReturnNull()
        {
            var result = Library.ToInt64((long?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromULong_ShouldReturnLong()
        {
            var result = Library.ToInt64((ulong?)9223372036854775807UL);
            Assert.AreEqual(9223372036854775807L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullULong_ShouldReturnNull()
        {
            var result = Library.ToInt64((ulong?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromFloat_ShouldReturnLong()
        {
            var result = Library.ToInt64((float?)123456.7f);
            Assert.AreEqual(123456L, result); // Truncated by cast
        }

        [TestMethod]
        public void ToInt64_FromNullFloat_ShouldReturnNull()
        {
            var result = Library.ToInt64((float?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromDouble_ShouldReturnLong()
        {
            var result = Library.ToInt64((double?)987654321.9);
            Assert.AreEqual(987654321L, result); // Truncated by cast
        }

        [TestMethod]
        public void ToInt64_FromNullDouble_ShouldReturnNull()
        {
            var result = Library.ToInt64((double?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromDecimal_ShouldReturnLong()
        {
            var result = Library.ToInt64((decimal?)123456789.8m);
            Assert.AreEqual(123456790L, result); // Rounded by Convert.ToInt64
        }

        [TestMethod]
        public void ToInt64_FromNullDecimal_ShouldReturnNull()
        {
            var result = Library.ToInt64((decimal?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromObject_ShouldReturnLong()
        {
            var result = Library.ToInt64((object)987654321L);
            Assert.AreEqual(987654321L, result);
        }

        [TestMethod]
        public void ToInt64_FromNullObject_ShouldReturnNull()
        {
            var result = Library.ToInt64((object?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt64_FromStringObject_ShouldReturnLong()
        {
            var result = Library.ToInt64((object)"456789123456");
            Assert.AreEqual(456789123456L, result);
        }

        [TestMethod]
        public void ToInt64_FromBooleanObject_ShouldReturnLong()
        {
            var result = Library.ToInt64((object)true);
            Assert.AreEqual(1L, result);
            
            result = Library.ToInt64((object)false);
            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        public void ToInt64_FromMaxValues_ShouldHandleCorrectly()
        {
            var result = Library.ToInt64((int?)int.MaxValue);
            Assert.AreEqual((long)int.MaxValue, result);
            
            result = Library.ToInt64((uint?)uint.MaxValue);
            Assert.AreEqual((long)uint.MaxValue, result);
        }

        [TestMethod]
        public void ToInt64_FromMinValues_ShouldHandleCorrectly()
        {
            var result = Library.ToInt64((int?)int.MinValue);
            Assert.AreEqual((long)int.MinValue, result);
            
            result = Library.ToInt64((sbyte?)sbyte.MinValue);
            Assert.AreEqual((long)sbyte.MinValue, result);
        }
    }
}