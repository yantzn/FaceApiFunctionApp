using System.Runtime.Serialization;

namespace FaceApiFunctionApp
{
	public class SuccessResponse
	{

		[DataMember(Name = "faceAttributes", Order = 1)]
		public faceAttributesitems faceAttributes { get; set; }

		public class faceAttributesitems
		{
			[DataMember(Name = "happiness", Order = 1)]
			public string avghappiness { get; set; }
		}

		public SuccessResponse()
		{
			faceAttributes = new faceAttributesitems();
		}
	}
}
