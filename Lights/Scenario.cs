using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Lights.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Lights
{
	public static class Scenario
	{
		[FunctionName("ScenarioOrchestrator")]
		public static async Task<bool> RunOrchestrator(
			[OrchestrationTrigger] IDurableOrchestrationContext context,
			ILogger log)
		{
			try
			{
				var scenario = context.GetInput<ScenarioRequest>();
				var lights = scenario.LightRequests.Select(lr => new EntityId(nameof(Light), lr.LightId)).ToArray();

				using (await context.LockAsync(lights))
				{
					for (var i = 0; i < lights.Length; i++)
					{
						var lightId = lights[i];
						//var lightProxy = context.CreateEntityProxy<ILight>(lightId);
						var lightRequest = scenario.LightRequests[i];

						switch (lightRequest.LightAction)
						{
							case LightAction.Off:
								//we can't use Signal here because the entity is locked!!
								//inside a critical section we can:
								//call entities that they are locked
								//signal entities that they are NOT locked
								//context.SignalEntity(lightId, "Off");
								//await lightProxy.Off();
								await context.CallEntityAsync(lightId, "Off");
								break;
							case LightAction.On:
								await context.CallEntityAsync(lightId, "On");
								//await lightProxy.On();
								break;
							case LightAction.Color:
								var currentState = await context.CallEntityAsync<LightState>(lightId, "Get");
								if (currentState == LightState.Off)
									await context.CallEntityAsync(lightId, "On");
								await context.CallEntityAsync(lightId, "Color", lightRequest.HexColor);
								//await lightProxy.Color(lightRequest.HexColor);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
				}

				return true;
			}
			catch (Exception e)
			{
				log.LogError(e.Message);
				return false;
			}
		}

		[FunctionName("ScenarioTrigger")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scenario")]HttpRequest req,
			[DurableClient] IDurableOrchestrationClient starter,
			ILogger log)
		{
			try
			{
				var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
				var scenario = JsonConvert.DeserializeObject<ScenarioRequest>(requestBody);

				var orchestratorId = await starter.StartNewAsync("ScenarioOrchestrator", scenario);

				log.LogInformation($"Started scenario with ID = '{orchestratorId}'.");

				return starter.CreateCheckStatusResponse(req, orchestratorId);
			}
			catch (Exception e)
			{
				log.LogError(e.Message);
				return new ExceptionResult(e, true);
			}
		}
	}
}