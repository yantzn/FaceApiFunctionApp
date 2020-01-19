using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace FaceApiFunctionApp
{
	public static class FaceApiFunction
	{

		private const string RECOGNITION_MODEL1 = RecognitionModel.Recognition01;
		private const string BAD_REQUEST = "BadRequest";
		private const string NOT_FOUND = "NotFound";

		private static IList<DetectedFace> detectedFaces;
		private static string imageUrl;
		private static string errorReason;
		private static string errorMsg;

		[FunctionName("FaceApi")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/FaceApi")] HttpRequest req, ILogger log)
		{

			// POSTデータからパラメータを取得
			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			dynamic data = JsonConvert.DeserializeObject(requestBody);

			log.LogInformation($"Request:{data}");

			// 必須チェック
			if (!Validation(data?.url))
			{
				errorReason = BAD_REQUEST;
				errorMsg = "リクエストパラメータが不正です。";
				log.LogInformation($"Response:{Serialize(CreateErrorResponse())}");
				return new BadRequestObjectResult(CreateErrorResponse());
			}

			// 画像のURL
			imageUrl = data?.url;

			// FaseAPIの認証を行う
			IFaceClient client = Authenticate(AppSettings.Instance.FACE_ENDPOINT, AppSettings.Instance.FACE_SUBSCRIPTION_KEY);

			try
			{
				// 感情分析を行う
				await DetectFaceExtract(client, imageUrl, RECOGNITION_MODEL1);
			}
			catch (APIErrorException ex)
			{
				errorReason = BAD_REQUEST;
				errorMsg = ex.Message;
				log.LogWarning($"Response:{Serialize(CreateErrorResponse())}");
				log.LogWarning($"StackTrace:{ex.StackTrace}");
				return new BadRequestObjectResult(CreateErrorResponse());

			}
			catch (Exception ex)
			{
				errorReason = NOT_FOUND;
				errorMsg = ex.Message;
				log.LogError($"Response:{Serialize(CreateErrorResponse())}");
				log.LogError($"StackTrace:{ex.StackTrace}");
				return new NotFoundObjectResult(CreateErrorResponse());
			}

			var res = JsonConvert.SerializeObject(CreateSuccessResponse());
			log.LogInformation($"Response:{res}");
			return new OkObjectResult(res);
		}

		/// <summary>
		/// 受信したPOSTデータのバリデーションチェック処理
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		private static dynamic Validation(dynamic val)
		{
			// 値の存在確認
			if (string.IsNullOrWhiteSpace(val.ToString()))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Faceサービスのインスタンス生成処理
		/// </summary>
		/// <param name="endpoint">FaceサービスのURL</param>
		/// <param name="key">Faseサービスの認証キー</param>
		/// <returns></returns>
		private static IFaceClient Authenticate(string endpoint, string key)
		{
			return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
		}

		/// <summary>
		/// 感情分析処理
		/// </summary>
		/// <param name="client">Faseサービスインスタンス</param>
		/// <param name="url">画像のURL</param>
		/// <param name="recognitionModel">検出モデル</param>
		/// <returns></returns>
		private static async Task DetectFaceExtract (IFaceClient client, string url, string recognitionModel)
		{
			detectedFaces = await client.Face.DetectWithUrlAsync($"{url}",
					returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.Emotion },
					recognitionModel: recognitionModel);
		}

		/// <summary>
		/// シリアライズ化処理してJSON形式に変換
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string Serialize(object obj)
		{
			using (var stream = new System.IO.MemoryStream())
			{
				var serializer = new DataContractJsonSerializer(obj.GetType());
				serializer.WriteObject(stream, obj);
				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}

		/// <summary>
		/// 異常系レスポンスJSONを作成
		/// </summary>
		/// <returns></returns>
		private static ErrorResponse CreateErrorResponse()
		{
			var response = new ErrorResponse();
			response.errors.reason = errorReason;
			response.errors.message = errorMsg;
			return response;
		}

		/// <summary>
		/// 正常系レスポンスJSONを作成
		/// </summary>
		/// <returns></returns>
		private static SuccessResponse CreateSuccessResponse()
		{
			var response = new SuccessResponse();
			response.faceAttributes.happiness = GetHapinessPoint();
			return response;
		}

		/// <summary>
		/// 笑顔指数の算出処理
		/// </summary>
		private static string GetHapinessPoint()
		{
			double sum = 0 , avg = 0;
			// 感情分析結果が1件も無い場合、0ptで返却する
			if (detectedFaces.Count == 0)
			{
				return "0";
			}

			// Happiness数の加算して合計を算出
			foreach (var item in detectedFaces)
			{
				sum += item.FaceAttributes.Emotion.Happiness;
			}

			// 000.000の形式に変換して返却する
			sum = (sum * 100000) / 1000;
			
			return sum.ToString("00.000");
		}
	}
}
