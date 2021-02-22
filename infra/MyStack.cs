using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using Pulumi;
using Pulumi.Azure.AppInsights;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

class MyStack : Stack
{
    public string ProjectStack { get; }
    public string StackSuffix { get; }
    public string MlModelVersion { get; }

    public string GetModelVersionForProductionSlot()
    {
        var currentStack = new StackReference($"igoran/{Deployment.Instance.ProjectName}/{Deployment.Instance.StackName}");

        var endpoint = (string)currentStack.RequireValueAsync("Endpoint").GetAwaiter().GetResult();

        if (string.IsNullOrEmpty(endpoint))
            return MlModelVersion;

        var uri = new UriBuilder(endpoint)
        {
            Path = "api/ping"
        }.ToString();

        using var client = new HttpClient();

        var response = client.GetAsync(uri).GetAwaiter().GetResult();

        response.EnsureSuccessStatusCode();

        var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        Console.WriteLine($"Response from {endpoint}: {content}");

        return "";
    }

    public MyStack()
    {
        ProjectStack = Deployment.Instance.ProjectName + "-" + Deployment.Instance.StackName;
        StackSuffix = Regex.Replace(Deployment.Instance.StackName, "[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase);
        MlModelVersion = System.Environment.GetEnvironmentVariable("ML_MODEL_URI") ?? string.Empty;

        var resourceGroup = new ResourceGroup(ProjectStack);

        var storageAccount = new Account("sa" + StackSuffix.ToLowerInvariant(), new AccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountReplicationType = "LRS",
            AccountTier = "Standard"
        });

        var appServicePlan = new Plan("asp" + StackSuffix.ToLowerInvariant(), new PlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "FunctionApp",
            Sku = new PlanSkuArgs
            {
                Tier = "Dynamic",
                Size = "Y1",
            }
        });

        var container = new Container("cntzip" + StackSuffix.ToLowerInvariant(), new ContainerArgs
        {
            StorageAccountName = storageAccount.Name,
            ContainerAccessType = "private"
        });

        var blob = new Blob("blobzip" + StackSuffix.ToLowerInvariant(), new BlobArgs
        {
            StorageAccountName = storageAccount.Name,
            StorageContainerName = container.Name,
            Type = "Block",
            Source = new FileArchive("../ml/Predictor/bin/Release/netcoreapp3.1/publish/")
        });

        var codeBlobUrl = SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount);

        var appInsights = new Insights("fxai" + StackSuffix.ToLowerInvariant(), new InsightsArgs
        {
            ResourceGroupName = resourceGroup.Name,
            ApplicationType = "web"
        });

        GetModelVersionForProductionSlot();

        var app = new FunctionApp("fxapp" + StackSuffix.ToLowerInvariant(), new FunctionAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = appServicePlan.Id,
            AppSettings =
            {
                {"DEP_SLOT", $"normal-{DateTime.Now:u}"},
                {"runtime", "dotnet"},
                {"WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl},
                {"AzureWebJobsStorage", storageAccount.PrimaryConnectionString},
                {"ML_MODEL_URI", MlModelVersion},
                {"APPINSIGHTS_INSTRUMENTATIONKEY", appInsights.InstrumentationKey}
            },
            SiteConfig = new FunctionAppSiteConfigArgs
            {
                Cors = new FunctionAppSiteConfigCorsArgs
                {
                    AllowedOrigins = new InputList<string>
                        {
                            "http://localhost:5500"
                        },
                    SupportCredentials = true
                }
            },
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            Version = "~3"
        });

        var stagingSlot = new FunctionAppSlot("staging", new FunctionAppSlotArgs
        {
            Name = "staging",
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = appServicePlan.Id,
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            FunctionAppName = app.Name,
            Version = "~3",
            SiteConfig = new FunctionAppSlotSiteConfigArgs
            {
                Cors = new FunctionAppSlotSiteConfigCorsArgs
                {
                    AllowedOrigins = new InputList<string>
                        {
                            "http://localhost:5500"
                        },
                    SupportCredentials = true
                }
            },
            AppSettings =
            {
                {"DEP_SLOT", $"staging-{DateTime.Now:u}"},
                {"runtime", "dotnet"},
                {"WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl},
                {"AzureWebJobsStorage", storageAccount.PrimaryConnectionString},
                {"ML_MODEL_URI", MlModelVersion},
                {"APPINSIGHTS_INSTRUMENTATIONKEY", appInsights.InstrumentationKey}
            },
        });

        StorageConnectionString = Output.Format($"{storageAccount.PrimaryConnectionString}");

        StaginEndpoint = Output.Format($"https://{stagingSlot.DefaultHostname}");

        Endpoint = Output.Format($"https://{app.DefaultHostname}");
    }


    [Output]
    public Output<string> StorageConnectionString { get; set; }
    [Output]
    public Output<string> StaginEndpoint { get; set; }
    [Output]
    public Output<string> Endpoint { get; set; }
}