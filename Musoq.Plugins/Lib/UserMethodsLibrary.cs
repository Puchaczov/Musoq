namespace Musoq.Plugins
{
    /// <summary>
    /// Methods that allows operations on groups
    /// </summary>
    public class UserMethodsLibrary
    {
        /// <summary>
        /// Gets the parent group
        /// </summary>
        /// <param name="group">The group</param>
        /// <param name="number">The number</param>
        /// <returns>Group</returns>
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
