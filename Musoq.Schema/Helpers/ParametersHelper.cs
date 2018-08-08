using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Schema.Helpers
{
    public static class ParametersHelper
    {
        public static object[] ExpandParameters(this object[] parameters, params object[] additionalParameters)
        {
            var objects = new List<object>();

            foreach (var obj in parameters)
                objects.Add(obj);

            objects.AddRange(additionalParameters);

            return objects.ToArray();
        }
    }
}
