using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins
{
    public class UserMethodsLibrary
    {
        protected static Group GetParentGroup(Group group, int number)
        {
            var i = 0;
            var parent = @group;

            while (parent.Parent != null && i < number)
            {
                parent = parent.Parent;
                i += 1;
            }

            return parent;
        }
    }
}
