using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace HttpDBRelay
{
	public static class Program
	{
		static SqlConnection dbConnection;
		static HttpListener httpListener;

		static async Task Main(string[] args)
		{
			//event handlers for when app is getting closed (ctrl+C or button X in window)
			AppDomain.CurrentDomain.ProcessExit += OnExit;
			Console.CancelKeyPress += OnExit;

			//create DB connection
			dbConnection = new SqlConnection("Server=localhost;Database=master;Trusted_Connection=True;");
			dbConnection.Open();

			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://localhost:63321/");
			httpListener.Start();
			Console.WriteLine("Listening on https://localhost:63321/");

			while (true)
			{
				//wait for next request
				var c = await httpListener.GetContextAsync();

				//read request data
				Stream requestStream = c.Request.InputStream;
				string recivedQuery = new StreamReader(requestStream).ReadToEnd();
				requestStream.Dispose();

				Console.WriteLine(recivedQuery);

				//run recieved query
				var queryResponse = new QueryResult();

				try
				{
					queryResponse = ExecuteQuery(recivedQuery);
				}
				catch (Exception e)
				{
					queryResponse.Error = true;
					queryResponse.ErrorText = e.ToString();
				}

				//construct JSON and response
				string json = JsonSerializer.Serialize(queryResponse);
				Console.WriteLine(json);

				var responseStreamWriter = new StreamWriter(c.Response.OutputStream);
				responseStreamWriter.Write(json);

				//send response
				responseStreamWriter.Dispose();
				c.Response.OutputStream.Dispose();
				c.Response.Close();
			}
		}

		static QueryResult ExecuteQuery(string queryString)
		{
			//create and execute sql query command
			var sqlCommand = new SqlCommand(queryString, dbConnection);
			var dataReader = sqlCommand.ExecuteReader();

			//fill QueryResult with the query result
			var queryResult = new QueryResult();
			int columns = dataReader.FieldCount;

			//column names
			for (int i = 0; i < columns; i++)
			{
				string colName = dataReader.GetName(i);
				Console.Write($"{colName,8}");
				queryResult.ColumnNames.Add(colName);
			}
			Console.WriteLine();

			//each row in the table
			while (dataReader.Read())
			{
				var row = new List<object>();

				//each column in row
				for (int i = 0; i < columns; i++)
				{
					object obj = dataReader.GetValue(i);

					if (obj == DBNull.Value)
					{
						obj = null;
					}

					Console.Write($"{ obj ?? "null",8 }");
					row.Add(obj);
				}
				Console.WriteLine();
				queryResult.Rows.Add(row);
			}

			dataReader.Close();
			sqlCommand.Dispose();

			return queryResult;
		}

		static void OnExit(object sender, EventArgs e)
		{
			dbConnection.Close();
			httpListener.Stop();
		}
	}
}
