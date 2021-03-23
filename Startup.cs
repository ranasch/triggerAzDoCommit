using Microsoft.Azure.Functions.Extensions.DependencyInjection;
[assembly: FunctionsStartup(typeof(triggerAzDoCommit.Startup))]

namespace triggerAzDoCommit
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using System;

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .AddUserSecrets<Startup>(true, true)
               .Build();

            var appSettings = new Appsettings()
            {
                PAT = config["PAT"],
                VSTSApiVersion = config["VSTSApiVersion"],
                VSTSOrganization = config["VSTSOrganization"]
            };
            builder.Services.AddSingleton(appSettings);


        }
    }
}
