using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Plugins.Tests
{
    [TestClass]
    public class ToInt32MethodsTests : LibraryBaseBaseTests
    {
        [TestMethod]
        public void ToInt32_FromString_ShouldReturnInteger()
        {
            var result = Library.ToInt32("123");
            Assert.AreEqual(123, result);
        }

        [TestMethod]
        public void ToInt32_FromInvalidString_ShouldReturnNull()
        {
            var result = Library.ToInt32("invalid");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromNullString_ShouldReturnNull()
        {
            var result = Library.ToInt32((string)null!);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromEmptyString_ShouldReturnNull()
        {
            var result = Library.ToInt32("");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromNegativeString_ShouldReturnNegativeInteger()
        {
            var result = Library.ToInt32("-456");
            Assert.AreEqual(-456, result);
        }

        [TestMethod]
        public void ToInt32_FromByte_ShouldReturnInteger()
        {
            var result = Library.ToInt32((byte?)255);
            Assert.AreEqual(255, result);
        }

        [TestMethod]
        public void ToInt32_FromNullByte_ShouldReturnNull()
        {
            var result = Library.ToInt32((byte?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromSByte_ShouldReturnInteger()
        {
            var result = Library.ToInt32((sbyte?)127);
            Assert.AreEqual(127, result);
        }

        [TestMethod]
        public void ToInt32_FromNullSByte_ShouldReturnNull()
        {
            var result = Library.ToInt32((sbyte?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromNegativeSByte_ShouldReturnNegativeInteger()
        {
            var result = Library.ToInt32((sbyte?)-50);
            Assert.AreEqual(-50, result);
        }

        [TestMethod]
        public void ToInt32_FromShort_ShouldReturnInteger()
        {
            var result = Library.ToInt32((short?)32767);
            Assert.AreEqual(32767, result);
        }

        [TestMethod]
        public void ToInt32_FromNullShort_ShouldReturnNull()
        {
            var result = Library.ToInt32((short?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromNegativeShort_ShouldReturnNegativeInteger()
        {
            var result = Library.ToInt32((short?)-1000);
            Assert.AreEqual(-1000, result);
        }

        [TestMethod]
        public void ToInt32_FromUShort_ShouldReturnInteger()
        {
            var result = Library.ToInt32((ushort?)65535);
            Assert.AreEqual(65535, result);
        }

        [TestMethod]
        public void ToInt32_FromNullUShort_ShouldReturnNull()
        {
            var result = Library.ToInt32((ushort?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromInt_ShouldReturnSameInteger()
        {
            var result = Library.ToInt32((int?)42);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void ToInt32_FromNullInt_ShouldReturnNull()
        {
            var result = Library.ToInt32((int?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromUInt_ShouldReturnInteger()
        {
            var result = Library.ToInt32((uint?)123456);
            Assert.AreEqual(123456, result);
        }

        [TestMethod]
        public void ToInt32_FromNullUInt_ShouldReturnNull()
        {
            var result = Library.ToInt32((uint?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromLong_ShouldReturnInteger()
        {
            var result = Library.ToInt32((long?)987654);
            Assert.AreEqual(987654, result);
        }

        [TestMethod]
        public void ToInt32_FromNullLong_ShouldReturnNull()
        {
            var result = Library.ToInt32((long?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromULong_ShouldReturnInteger()
        {
            var result = Library.ToInt32((ulong?)789123);
            Assert.AreEqual(789123, result);
        }

        [TestMethod]
        public void ToInt32_FromNullULong_ShouldReturnNull()
        {
            var result = Library.ToInt32((ulong?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromFloat_ShouldReturnInteger()
        {
            var result = Library.ToInt32((float?)123.7f);
            Assert.AreEqual(124, result); // Rounded by Convert.ToInt32
        }

        [TestMethod]
        public void ToInt32_FromNullFloat_ShouldReturnNull()
        {
            var result = Library.ToInt32((float?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromDouble_ShouldReturnInteger()
        {
            var result = Library.ToInt32((double?)456.2);
            Assert.AreEqual(456, result); // Truncated by Convert.ToInt32
        }

        [TestMethod]
        public void ToInt32_FromNullDouble_ShouldReturnNull()
        {
            var result = Library.ToInt32((double?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromDecimal_ShouldReturnInteger()
        {
            var result = Library.ToInt32((decimal?)789.9m);
            Assert.AreEqual(790, result); // Rounded by Convert.ToInt32
        }

        [TestMethod]
        public void ToInt32_FromNullDecimal_ShouldReturnNull()
        {
            var result = Library.ToInt32((decimal?)null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromObject_ShouldReturnInteger()
        {
            var result = Library.ToInt32((object)246);
            Assert.AreEqual(246, result);
        }

        [TestMethod]
        public void ToInt32_FromNullObject_ShouldReturnNull()
        {
            var result = Library.ToInt32((object)null!);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ToInt32_FromStringObject_ShouldReturnInteger()
        {
            var result = Library.ToInt32((object)"135");
            Assert.AreEqual(135, result);
        }

        [TestMethod]
        public void ToInt32_FromBooleanObject_ShouldReturnInteger()
        {
            var result = Library.ToInt32((object)true);
            Assert.AreEqual(1, result);
            
            result = Library.ToInt32((object)false);
            Assert.AreEqual(0, result);
        }
    }
}