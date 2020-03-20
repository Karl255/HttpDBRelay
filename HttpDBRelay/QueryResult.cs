using System;
using System.Collections.Generic;
using System.Text;

namespace HttpDBRelay
{
	public class QueryResult
	{
		public bool Error { get; set; } = false;
		public string ErrorText { get; set; } = "";
		public List<string> ColumnNames { get; set; } = new List<string>();
		public List<List<object>> Rows { get; set; } = new List<List<object>>();
	}
}
