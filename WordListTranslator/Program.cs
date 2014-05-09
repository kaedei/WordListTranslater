using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace WordListTranslator
{
	class Program
	{
		private static volatile int finishedCount = 0;
		static void Main(string[] args)
		{
			var cId = ConfigurationManager.AppSettings["IdColumn"];
			var cFrom = ConfigurationManager.AppSettings["FromColumn"];
			var cTo = ConfigurationManager.AppSettings["ToColumn"];
			var tableName = ConfigurationManager.AppSettings["TableName"];

			//填充需要翻译的
			var inputContent = FillTranslationList(tableName, cId, cFrom, cTo);

			//var listener = new HttpListener();
			//listener.Prefixes.Add("http://127.0.0.1:23333/");
			//listener.Start();

			//这里需要换成自己的client id和key
			var auth = new AdmAuthentication("wordlisttranslator", "5j9oxYpwtzny93IGHzmZA3tdDbRw5zhu2bAv2jsBPdA=");

			//翻译
			using (var connection = new OleDbConnection(ConfigurationManager.ConnectionStrings["Default"].ConnectionString))
			{
				connection.Open();
				Parallel.For(0, inputContent.Count, index =>
				{
					TranslateAndSave(inputContent, index, auth, connection, tableName, cTo);
				});
			}

			Console.WriteLine("按回车键退出");
			Console.ReadLine();
		}

		private static void TranslateAndSave(List<Tuple<int, string>> inputContent, int index, AdmAuthentication auth, OleDbConnection connection,
			string tableName, string cTo)
		{
			var id = inputContent[index].Item1;
			var text = inputContent[index].Item2;
			if (string.IsNullOrEmpty(text))
				return;
			if (ContainsHtmlLabel(text))
			{
				Console.WriteLine("({0}/{1}){2}\t{3}", ++finishedCount, inputContent.Count, text, text);
			}
			else
			{
				var client = new TranslateService.LanguageServiceClient();
				try
				{
					var token = auth.GetAccessToken();
					var result = client.Translate("Bearer " + token.access_token,
						text, "en", "zh-CHS", "text/plain", "general");
					using (var command = connection.CreateCommand())
					{
						command.CommandText = string.Format(@"Update {0} Set {1}='{2}' Where ID={3}",
							tableName, cTo, result.Replace("'", "''"), id);
						command.ExecuteNonQuery();
					}
					Console.WriteLine("({0}/{1}){2}\t{3}", ++finishedCount, inputContent.Count, text, result);
				}
				catch
				{
					Console.WriteLine("({0}/{1}){2}\t{3}", ++finishedCount, inputContent.Count, text, text);
				}
			}
		}

		/// <summary>
		/// 获取需要翻译的文本列表
		/// </summary>
		/// <param name="cId">ID列名称</param>
		/// <param name="cFrom">需要翻译的列名</param>
		/// <param name="tableName">数据库中的表名</param>
		/// <param name="cTo">把翻译后的结果放到哪个列</param>
		private static List<Tuple<int, string>> FillTranslationList(string tableName, string cId, string cFrom, string cTo)
		{
			var inputContent = new List<Tuple<int, string>>();
			using (var connection = new OleDbConnection(ConfigurationManager.ConnectionStrings["Default"].ConnectionString))
			{
				connection.Open();
				using (var command = new OleDbCommand(string.Format("Select {0},{1} From {2} Where {3} is NULL or {3}=''", cId, cFrom, tableName, cTo),
							connection))
				{
					using (var reader = command.ExecuteReader())
					{
						while (reader != null && reader.Read())
						{
							try
							{
								inputContent.Add(new Tuple<int, string>(reader.GetInt32(0), reader.GetString(1)));
							}
							catch
							{
								inputContent.Add(new Tuple<int, string>(reader.GetInt32(0), ""));
							}
						}
					}
				}
			}
			return inputContent;
		}

		private static bool ContainsHtmlLabel(string text)
		{
			return (text.Contains("<html>") ||
					text.Contains("<table>") ||
					text.Contains("<tr>") ||
					text.Contains("<td>") ||
					text.Contains("class=") ||
					text.Contains("href=") ||
					text.Contains("</") ||
					text.Contains("System.") || //.net程序集
					text.Contains("<script"));
		}
	}
}
