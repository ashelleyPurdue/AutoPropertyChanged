using System;

namespace AutoPropertyChanged
{
    public class NotifyChangedAttribute : Attribute { }

    public class DependsOnAttribute : Attribute
    {
        public string Property { get; set; }
    }
}
