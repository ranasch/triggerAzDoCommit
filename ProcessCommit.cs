namespace triggerAzDoCommit
{
    using System;
    using System.IO;
    using Flurl;
    using Flurl.Http;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    public class ProcessCommit
    {
        private static Appsettings _config;
        private readonly string _organizationName;
        private readonly Uri _organizationUrl;
        private readonly string _pat;
        private readonly string _apiVersion;

        public ProcessCommit(Appsettings settings)
        {
            _config = settings;
            _organizationName = settings.VSTSOrganization;
            _pat = settings.PAT;
            _apiVersion = settings.VSTSApiVersion;
            _organizationUrl = new Uri($"https://dev.azure.com/{_organizationName}");
        }

        [FunctionName("ProcessCommit")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var project = "dummyProject"; // sample project
            var pipelineId = "54"; // sample pipeline id

            log.LogInformation("C# HTTP trigger function processed a request.");
           
            // get content as string
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // parse string as json
            var payload = JsonDocument.Parse(requestBody);
            // get property values from json
            var eventKey = payload.RootElement.GetProperty("eventKey").GetString();
            var date = payload.RootElement.GetProperty("date").GetString();
            var actorDisplayName = payload.RootElement.GetProperty("actor").GetProperty("displayName").GetString();
            
            // iterate array
            foreach(var change in payload.RootElement.GetProperty("changes").EnumerateArray())
            {
                var refId = change.GetProperty("ref").GetProperty("id").GetString();
                Console.WriteLine($"ref.id={refId}");
                var fromHash = change.GetProperty("fromHash").GetString();
                Console.WriteLine($"fromHash = {fromHash}");
                
            }
            // find first array entry of type branch
            var branchRefId = payload.RootElement
                .GetProperty("changes").EnumerateArray().First(c => c.GetProperty("ref").GetProperty("type").GetString() == "BRANCH")
                .GetProperty("ref")
                .GetProperty("id")
                .GetString();

            // Debug output
            Console.WriteLine($"eventKey = {eventKey}");
            Console.WriteLine($"date = {date}");
            Console.WriteLine($"actor.DisplayName={actorDisplayName}");

            // POST https://dev.azure.com/{organization}/{project}/_apis/pipelines/{pipelineId}/runs?pipelineVersion={pipelineVersion}&api-version=6.1-preview.1
            // Request URL: https://dev.azure.com/pocit/17a631d0-df12-498b-a172-99a8a6855662/_apis/pipelines/54/runs


            // create payload request - here simple string, better: create class and pass object as .PostJsonAsync(object)
            var postPayload = $"{{\"stagesToSkip\":[],\"resources\":{{\"repositories\":{{\"self\":{{\"refName\":\"{branchRefId}\"}}}}}},\"variables\":{{}}}}";

            // trigger pipeline for specific branch
            // see https://docs.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/run%20pipeline?view=azure-devops-rest-6.1
            var pipelineTrigger = await _organizationUrl
                .AppendPathSegment(project)
                .AppendPathSegment($"_apis/pipelines/{pipelineId}/runs")
                .WithBasicAuth(string.Empty, _pat)
                .WithHeader("Content-Type", "application/json")
                .AllowAnyHttpStatus()
                .SetQueryParam("api-version", _apiVersion)
                .PostStringAsync(postPayload);

            if (pipelineTrigger.ResponseMessage.IsSuccessStatusCode)
            {
                return new OkObjectResult($"Triggered pipeline {pipelineId} successfully on branch {branchRefId}");
                
            }
            else
            {
                return new BadRequestObjectResult($"Trigger Pipeline {pipelineId} failed, reason {pipelineTrigger.ResponseMessage.StatusCode} {pipelineTrigger.ResponseMessage.ReasonPhrase}");
            }
        }
    }
}

