namespace Lights.Models
{
	public class LightRequest
	{
		public string LightId { get; set; }
		public LightAction LightAction { get; set; }
		public string HexColor { get; set; } = "#efebd8";
	}
}
