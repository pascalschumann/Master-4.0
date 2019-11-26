using System;
using System.IO;
using Master40.DB.Data.Helper;
using Xunit;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestObjectTracking : AbstractTest
    {
        
        public TestObjectTracking() : base(initDefaultTestConfig: false)
        {
            Master40.DB.Configuration.TrackObjects = true;
            InitTestScenario(DefaultTestScenario);
        }
    
        [Fact]
        public void TestTrackingOfObjects()
        {

            IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
            
            string usedIdsFileName = IdGenerator.WriteToFile();
            
            Assert.True( File.Exists(usedIdsFileName),
                $"Tracking created object hasn't worked: File '{usedIdsFileName}' was not created.");
        }
    }
}