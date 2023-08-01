using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema.Basic
{
    public class Library : LibraryBase
    {
        private readonly Random _random = new();
        
        [BindableMethod]
        public T DoNothing<T>(T value)
        {
            return value;
        }

        [BindableMethod]
        public string Name([InjectSpecificSource(typeof(BasicEntity))] BasicEntity entity)
        {
            return entity.Name;
        }

        [BindableMethod]
        public string MyName([InjectSpecificSource(typeof(BasicEntity))] BasicEntity entity)
        {
            return entity.Name;
        }

        [BindableMethod]
        public string Extension([InjectSpecificSource(typeof(BasicEntity))] BasicEntity entity)
        {
            return ".txt";
        }

        [BindableMethod]
        public int RandomNumber()
        {
            return _random.Next(0, 100);
        }

        [BindableMethod]
        public decimal GetOne()
        {
            return 1;
        }

        [BindableMethod]
        public string GetTwo(decimal a, string b)
        {
            return 2.ToString();
        }

        [BindableMethod]
        public decimal Inc(decimal number)
        {
            return number + 1;
        }

        [BindableMethod]
        public long Inc(long number)
        {
            return number + 1;
        }

        [BindableMethod]
        public BasicEntity NothingToDo(BasicEntity entity)
        {
            return entity;
        }

        [BindableMethod]
        public int? NullableMethod(int? value)
        {
            return value;
        }

        public new string ToString(object obj)
        {
            return obj.ToString();
        }

        [BindableMethod]
        public int PrimitiveArgumentsMethod(long a, decimal b, bool tr, bool fl, string text)
        {
            Assert.AreEqual(1L, a);
            Assert.AreEqual(2m, b);
            Assert.AreEqual(true, tr);
            Assert.AreEqual(false, fl);
            Assert.AreEqual("text", text);
            return 1;
        }
        
        [BindableMethod]
        public string GetCity([InjectSpecificSource(typeof(BasicEntity))] BasicEntity entity)
        {
            return entity.City;
        }
        
        [BindableMethod]
        public string GetCountry([InjectSpecificSource(typeof(BasicEntity))] BasicEntity entity)
        {
            return entity.Country;
        }
        
        [BindableMethod]
        public decimal GetPopulation([InjectSpecificSource(typeof(BasicEntity))] BasicEntity entity)
        {
            return entity.Population;
        }
    }
}
