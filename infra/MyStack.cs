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

        var app = new FunctionApp("fxapp" + StackSuffix.ToLowerInvariant(), new FunctionAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = appServicePlan.Id,
            AppSettings =
            {
                {"runtime", "dotnet"},
                {"WEBSITE_RUN_FROM_PACKAGE", codeBlobUrl},
                {"ML_MODEL_URI", MlModelVersion},
                {"APPINSIGHTS_INSTRUMENTATIONKEY", appInsights.InstrumentationKey}
            },
            StorageAccountName = storageAccount.Name,
            StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
            Version = "~3"
        });

        Endpoint = Output.Format($"https://{app.DefaultHostname}");
    }

    [Output]
    public Output<string> Endpoint { get; set; }
}