using System.Collections.Generic;

namespace HttpDBRelay
{
	public class QueryResult
	{
		public bool Error { get; set; } = false;
		public string ErrorText { get; set; } = "";
		public List<string> ColumnNames { get; set; } = new List<string>();
		public List<List<string>> Rows { get; set; } = new List<List<string>>();
	}
}
