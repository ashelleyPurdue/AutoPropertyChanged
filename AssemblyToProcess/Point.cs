using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using AutoPropertyChanged;

namespace AssemblyToProcess
{
    public class Point : INotifyPropertyChanged
    {
        [NotifyChanged] public int X { get; set; }
        [NotifyChanged] public int Y { get; set; }
        [NotifyChanged] public int Z { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
