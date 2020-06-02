using Pulumi;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

namespace Infra
{
    internal class MyStack : Stack
    {
        public MyStack()
        {
            var projectName = Deployment.Instance.ProjectName.ToLower();
            var stackName = Deployment.Instance.StackName;
            
            #region Resource Group
            
            var resourceGroupName = $"{projectName}-{stackName}-rg";
            var resourceGroup = new ResourceGroup(resourceGroupName, new ResourceGroupArgs
            {
                Name = resourceGroupName
            });
            
            #endregion

            #region Azure Storage
            
            var storageAccountName = $"{projectName}{stackName}st";
            var storageAccount = new Account(storageAccountName, new AccountArgs
            {
                Name = storageAccountName,
                ResourceGroupName = resourceGroup.Name,
                AccountReplicationType = "LRS",
                AccountTier = "Standard",
                AccountKind = "StorageV2",
                EnableHttpsTrafficOnly = true
            });
            
            StorageConnectionString = storageAccount.PrimaryConnectionString;
            
            #endregion
            
            #region Func Blobs
            
            var container = new Container("zips", new ContainerArgs
            {
                StorageAccountName = storageAccount.Name,
                ContainerAccessType = "private"
            });
            
            var lightsBlob = new Blob("lightszip", new BlobArgs
            {
                StorageAccountName = storageAccount.Name,
                StorageContainerName = container.Name,
                Type = "Block",
                Source = new FileArchive("../Lights/bin/Debug/netcoreapp3.1/publish")
            });
            var lightsBlobUrl = SharedAccessSignature.SignedBlobReadUrl(lightsBlob, storageAccount);
            
            var cacheBlob = new Blob("cachezip", new BlobArgs
            {
                StorageAccountName = storageAccount.Name,
                StorageContainerName = container.Name,
                Type = "Block",
                Source = new FileArchive("../Cache/bin/Debug/netcoreapp3.1/publish")
            });
            var cacheBlobUrl = SharedAccessSignature.SignedBlobReadUrl(cacheBlob, storageAccount);
            
            
            #endregion 
            
            #region AppService Plan
            
            var planName = $"{projectName}-{stackName}-plan";
            var appServicePlan = new Plan(planName, new PlanArgs
            {
                Name = planName,
                ResourceGroupName = resourceGroup.Name,
                Kind = "FunctionApp",
                Sku = new PlanSkuArgs
                {
                    Tier = "Dynamic",
                    Size = "Y1"
                }
            });

            #endregion 
            
            #region Lights
            
            var lightsName = $"{projectName}-{stackName}-func-lights";
            var lightsFunc = new FunctionApp(lightsName, new FunctionAppArgs
            {
                Name = lightsName,
                ResourceGroupName = resourceGroup.Name,
                AppServicePlanId = appServicePlan.Id,
                AppSettings =
                {
                    {"runtime", "dotnet"},
                    {"WEBSITE_RUN_FROM_PACKAGE", lightsBlobUrl}
                },
                StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
                StorageAccountName = storageAccount.Name,
                Version = "~3"
            });
            
            LightsFunc = lightsFunc.DefaultHostname;

            #endregion
            
            #region Cache
            
            var cacheName = $"{projectName}-{stackName}-func-cache";
            var cacheFunc = new FunctionApp(cacheName, new FunctionAppArgs
            {
                Name = cacheName,
                ResourceGroupName = resourceGroup.Name,
                AppServicePlanId = appServicePlan.Id,
                AppSettings =
                {
                    {"runtime", "dotnet"},
                    {"WEBSITE_RUN_FROM_PACKAGE", cacheBlobUrl}
                },
                StorageAccountAccessKey = storageAccount.PrimaryAccessKey,
                StorageAccountName = storageAccount.Name,
                Version = "~3"
            });

            CacheFunc = cacheFunc.DefaultHostname;

            #endregion
        }
        
        [Output] public Output<string> StorageConnectionString { get; set; }
        [Output] public Output<string> CacheFunc { get; set; }
        [Output] public Output<string> LightsFunc { get; set; }
    }
}
