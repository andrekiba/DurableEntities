using Pulumi;
using Pulumi.Azure.Core;
using Pulumi.Azure.Storage;

namespace Infra
{
    internal class MyStack : Stack
    {
        public MyStack()
        {
            var projectName = Deployment.Instance.ProjectName;
            var stackName = Deployment.Instance.StackName;
            
            // Create an Azure Resource Group
            var resourceGroup = new ResourceGroup("resourceGroup");

            // Create an Azure Storage Account
            var storageAccount = new Account("storage", new AccountArgs
            {
                ResourceGroupName = resourceGroup.Name,
                AccountReplicationType = "LRS",
                AccountTier = "Standard"
            });

            // Export the connection string for the storage account
            this.ConnectionString = storageAccount.PrimaryConnectionString;
        }

        [Output]
        public Output<string> ConnectionString { get; set; }
    }
}
