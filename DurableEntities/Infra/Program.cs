using System.Threading.Tasks;
using Pulumi;

namespace Infra
{
    internal class Program
    {
        static Task<int> Main() => Deployment.RunAsync<MyStack>();
    }
}
