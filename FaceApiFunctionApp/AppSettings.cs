using Microsoft.Extensions.Configuration;

namespace FaceApiFunctionApp
{
	class AppSettings
	{
		private readonly IConfigurationRoot _configuration;

		/// <summary>
		/// 環境変数に設定された項目を取得する
		/// </summary>
		public AppSettings()
		{
			var builder = new ConfigurationBuilder()
							.AddJsonFile("local.settings.json", true)
							.AddEnvironmentVariables();

			_configuration = builder.Build();
		}

		public string FACE_SUBSCRIPTION_KEY => _configuration[nameof(FACE_SUBSCRIPTION_KEY)];

		public string FACE_ENDPOINT => _configuration[nameof(FACE_ENDPOINT)];

		public string HAPPINESS_BONUSPOINT => _configuration[nameof(HAPPINESS_BONUSPOINT)];

		public string HAPPINESS_INDEX => _configuration[nameof(HAPPINESS_INDEX)];

		public static AppSettings Instance { get; } = new AppSettings();
	}
}
