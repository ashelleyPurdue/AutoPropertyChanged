using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xunit;

namespace Tests
{
    public static class INPCExtensions
    {
        public static void AssertChangesProperty(this INotifyPropertyChanged obj, string property, Action action)
        {
            bool changed = false;
            void DetectChanges(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == property)
                    changed = true;
            }

            obj.PropertyChanged += DetectChanges;
            action();
            obj.PropertyChanged -= DetectChanges;

            Assert.True(changed);
        }
    }
}
