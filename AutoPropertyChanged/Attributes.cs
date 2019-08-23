using System;

namespace AutoPropertyChanged
{
    public class NotifyChangedAttribute : Attribute { }

    public class DependsOnAttribute : Attribute
    {
        public string[] Properties { get; set; }

        public DependsOnAttribute(params string[] properties)
        {
            Properties = properties;
        }
    }
}
