using Musoq.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Service.Core.Tests.Schema
{
    public class TestTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[0];
    }
}
