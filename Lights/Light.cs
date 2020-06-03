using System.Threading.Tasks;
using Lights.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lights
{
    public interface ILight
    {
        Task On();
        Task Off();
        Task<LightState> Get();
        Task Color(string hexColor);
        Task End();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Light : ILight
    {
        readonly ILogger log;
        public Light(ILogger log)
        {
            this.log = log;
        }

        #region Properties

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public LightState State { get; private set; } = LightState.Off;
        [JsonProperty]
        public string HexColor { get; private set; } = "#efebd8";

        #endregion

        #region Methods

        public Task On()
        {
            State = LightState.On;
            return Task.CompletedTask;
        }

        public Task Off()
        {
            State = LightState.Off;
            return Task.CompletedTask;
        }

        public Task<LightState> Get() => Task.FromResult(State);
        public Task Color(string hexColor)
        {
            HexColor = hexColor;
            return Task.CompletedTask;
        }

        public Task End()
        {
            Entity.Current.DeleteState();
            return Task.CompletedTask;
        }

        #endregion

        [FunctionName(nameof(Light))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext ctx,
            ILogger log)
            => ctx.DispatchAsync<Light>(log);
    }
}
