using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Cache
{
	public static class CachedCall
	{
		[FunctionName("CachedCall")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "get", Route = "data/{userId}")] HttpRequest req,
			string userId,
			[DurableClient] IDurableClient client,
			ILogger log)
		{
			try
			{
				var cacheId = new EntityId(nameof(ByteCache), $"cache{userId}");
				var state = await client.ReadEntityStateAsync<ByteCache>(cacheId);

				if (state.EntityExists)
					return new OkObjectResult(state.EntityState.Value);

				//simulate call to external service to retrieve data
				await Task.Delay(TimeSpan.FromSeconds(2));
				var data = new byte[10];
				new Random().NextBytes(data);

				await client.SignalEntityAsync<ICache<byte[]>>(cacheId, async proxy => await proxy.Set(data));
				//await client.SignalEntityAsync(cacheId, "Set", data);

				var orchestratorId = await client.StartNewAsync(nameof(CacheOrchestrator), $"cache{userId}orchestrator", cacheId);
				var managementPayload = client.CreateHttpManagementPayload(orchestratorId);

				return new OkObjectResult(new { Management = managementPayload, Data = data });
			}
			catch (Exception e)
			{
				log.LogError(e.Message);
				return new ExceptionResult(e, true);
			}
		}
	}
}
