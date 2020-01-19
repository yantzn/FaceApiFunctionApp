using System.Runtime.Serialization;

namespace FaceApiFunctionApp
{
	public class ErrorResponse
	{
		[DataMember(Name = "errors", Order = 1)]
		public erroritem errors { get; set; }

		public class erroritem
		{
			[DataMember(Name = "reason", Order = 1)]
			public string reason { get; set; }

			[DataMember(Name = "message", Order = 2)]
			public string message { get; set; }
		}

		public ErrorResponse()
		{
			errors = new erroritem();
		}
	}
}
