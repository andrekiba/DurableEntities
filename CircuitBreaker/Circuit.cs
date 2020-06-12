using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CircuitBreaker
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Circuit
    {
        readonly ILogger log;
        readonly IDurableClient durableClient;
        public Circuit(IDurableClient client, ILogger log)
        {
            durableClient = client;
            this.log = log;
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public CircuitState State = CircuitState.Closed;

        // Current rolling window of failures reported for this circuit
        [JsonProperty]
        public IDictionary<string, FailureRequest> FailureWindow = new Dictionary<string, FailureRequest>();

        // The TimeSpan difference from latest to keep failures in the window
        static readonly TimeSpan windowSize = TimeSpan.Parse(
            Environment.GetEnvironmentVariable("WindowSize") ?? "00:00:30");

        // The number of failures in the window until opening the circuit
        static readonly int failureThreshold = int.Parse(
            Environment.GetEnvironmentVariable("FailureThreshold") ?? "5");

        public void CloseCircuit() => State = CircuitState.Closed;
        
        public void OpenCircuit() => State = CircuitState.Open;

        public async Task AddFailure(FailureRequest req)
        {
            if(State == CircuitState.Open)
            {
                log.LogInformation($"Tried to add additional failure to {Entity.Current.EntityKey} that is already open.");
                return;
            }

            FailureWindow.Add(req.RequestId, req);

            var cutoff = req.FailureTime.Subtract(windowSize);

            // Filter the window only to exceptions within the cutoff timespan
            FailureWindow = FailureWindow.Where(p => p.Value.FailureTime >= cutoff).ToDictionary( p => p.Key, p => p.Value);

            if(FailureWindow.Count >= failureThreshold)
            {
                log.LogCritical($"Break this circuit for entity {Entity.Current.EntityKey}!");

                await durableClient.StartNewAsync(nameof(OpenCircuitOrchestrator.OpenCircuit), req.InstanceId);

                // Mark the circuit as "open" (circuit is broken)
                State = CircuitState.Open;
            }
            else 
            {
                log.LogInformation($"The circuit {Entity.Current.EntityKey} currently has {FailureWindow.Count} exceptions in the window of {windowSize.ToString()}");
            }
        }

        [FunctionName(nameof(Circuit))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext ctx,
            [DurableClient] IDurableClient client,
            ILogger log) => ctx.DispatchAsync<Circuit>(client, log);
        
    }
}