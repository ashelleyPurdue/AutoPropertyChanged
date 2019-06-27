using System;
using System.ComponentModel;
using Fody;
using Xunit;

namespace Tests
{
    public class WeaverTests
    {
        static TestResult testResult;

        static WeaverTests()
        {
            var weavingTask = new ModuleWeaver();
            testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
        }

        [Fact]
        public void Changing_Property_Fires_Event()
        {
            var type = testResult.Assembly.GetType("AssemblyToProcess.Point");
            var point = (dynamic)Activator.CreateInstance(type);
            point.X = 0;
            point.Y = 1;
            point.Z = 2;

            ((INotifyPropertyChanged)point).AssertChangesProperty("X", () => point.x = 1);
        }
    }

}