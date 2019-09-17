using System;
using System.IO;
using Master40.DB.Data.Helper;
using Xunit;
using Zpp.Mrp;

namespace Zpp.Test.Integration_Tests
{
    public class TestTrackObjects : AbstractTest
    {
        
        public TestTrackObjects() : base(initDefaultTestConfig: false)
        {
            Master40.DB.Configuration.TrackObjects = true;
            InitTestScenario(DefaultTestScenario);
        }
    
        [Fact]
        public void TestTrackingOfObjects()
        {

            MrpRun.Start(ProductionDomainContext);
            
            string usedIdsFileName = IdGenerator.WriteToFile();
            
            Assert.True( File.Exists(usedIdsFileName),
                $"Tracking created object hasn't worked: File '{usedIdsFileName}' was not created.");
        }
    }
}