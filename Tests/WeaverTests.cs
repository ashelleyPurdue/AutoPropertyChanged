using System;
using System.ComponentModel;
using System.Reflection;
using Fody;
using Xunit;

namespace Tests
{
    public class WeaverTests
    {
        private static TestResult testResult;
        private static Type pointType;
        static WeaverTests()
        {
            var weavingTask = new ModuleWeaver();
            testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
            pointType = testResult.Assembly.GetType("AssemblyToProcess.Point");
        }

        [Fact]
        public void Changing_Property_Doesnt_Crash_If_No_Subscribers()
        {
            dynamic point = Activator.CreateInstance(pointType);
            point.X = 2;
        }
        
        [Fact]
        public void Changing_Property_Fires_Event()
        {
            dynamic point = Activator.CreateInstance(pointType);
            point.X = 0;
            point.Y = 1;
            point.Z = 2;

            ((INotifyPropertyChanged)point).AssertChangesProperty("X", () => point.X = 1);
        }

        private void InvokePrivateMethod(object obj, string name, params object[] args) => obj
            .GetType()
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(obj, args);
    }

}