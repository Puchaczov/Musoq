using System;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests.Schema
{
    public class TestLibrary : LibraryBase
    {
        private readonly Random _random = new Random();

        [BindableMethod]
        public string Name([InjectSource] BasicEntity entity)
        {
            return entity.Name;
        }

        [BindableMethod]
        public string MyName([InjectSource] BasicEntity entity)
        {
            return entity.Name;
        }

        [BindableMethod]
        public string Extension([InjectSource] BasicEntity entity)
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
    }
}