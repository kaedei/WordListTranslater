#region File Information
// ********************
// Translator.cs
// Solution:	WordListTranslator
// Project:	WordListTranslator
// 
// Created At: 2014-05-09 10:24
// Created User: Qing Feng Xu
// 
// 
// 
// ********************
#endregion

using System.Diagnostics;
using System.Runtime.Serialization;

namespace WordListTranslator
{
	[DataContract]
	public class AdmAccessToken
	{
		[DataMember]
		public string access_token { get; set; }
		[DataMember]
		public string token_type { get; set; }
		[DataMember]
		public string expires_in { get; set; }
		[DataMember]
		public string scope { get; set; }
	}
}