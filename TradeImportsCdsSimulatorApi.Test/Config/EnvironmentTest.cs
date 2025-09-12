using Microsoft.AspNetCore.Builder;

namespace TradeImportsCdsSimulatorApi.Test.Config;

public class EnvironmentTest
{

   [Fact]
   public void IsNotDevModeByDefault()
   { 
       var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
       var isDev = TradeImportsCdsSimulatorApi.Config.Environment.IsDevMode(builder);
       Assert.False(isDev);
   }
}
