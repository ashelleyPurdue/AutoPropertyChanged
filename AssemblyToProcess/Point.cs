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

        [DependsOn("X")]//, "Y", "Z")]
        public double Magnitude => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
