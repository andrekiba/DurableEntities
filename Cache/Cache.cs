using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cache
{
	public interface ICache<T>
	{
		Task Set(T value);
		Task<T> Get();
		Task Clear();
	}

	[JsonObject(MemberSerialization.OptIn)]
	public abstract class Cache<T> : ICache<T>
	{
		[JsonProperty]
		public T Value { get; private set; }

		public Task Set(T value)
		{
			Value = value;
			return Task.CompletedTask;
		}

		public Task<T> Get() => Task.FromResult(Value);

		public Task Clear()
		{
			Entity.Current.DeleteState();
			return Task.CompletedTask;
		}
	}

	public class ByteCache : Cache<byte[]>
	{
		readonly ILogger log;

		public ByteCache(ILogger log)
		{
			this.log = log;
		}
  
		[FunctionName(nameof(ByteCache))]
		public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger log)
			=> ctx.DispatchAsync<ByteCache>(log);
	}
}
