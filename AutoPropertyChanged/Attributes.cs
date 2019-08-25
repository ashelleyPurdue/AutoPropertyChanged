using System;

namespace AutoPropertyChanged
{
    public class NotifyChangedAttribute : Attribute { }

    public class DependsOnAttribute : Attribute
    {
        public DependsOnAttribute(string dependency)
        {
        }

        public DependsOnAttribute(string dependency, params string[] dependencies)
        {
        }
    }
}
