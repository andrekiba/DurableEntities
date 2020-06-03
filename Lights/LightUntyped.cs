using System;
using System.Threading.Tasks;
using Lights.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lights
{
	public static class LightUntyped
	{
		[FunctionName("LightUntyped")]
		public static Task LightUntypedRun(
			[EntityTrigger] IDurableEntityContext ctx,
			ILogger log)
		{
			try
			{
				if (!ctx.HasState)
					//ctx.SetState(new { State = LightState.Off, HexColor = "#eeeeee"});
					ctx.SetState(new Props{ State = LightState.Off, HexColor = "#eeeeee"});
				
				//var currentState = ctx.GetState<dynamic>();
				var currentState = ctx.GetState<Props>();

				switch (ctx.OperationName)
				{
					case "On":
						currentState.State = LightState.On;
						break;
					case "Off":
						currentState.State = LightState.Off;
						break;
					case "Color":
						var hexColor = ctx.GetInput<string>();
						currentState.HexColor = hexColor;
						break;
					case "Get":
						ctx.Return(currentState);
						break;
					case "End":
						ctx.DeleteState();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			return Task.CompletedTask;
		}
	}

	internal class Props
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public LightState State { get; set; }
		public string HexColor { get; set; }
	}
}
