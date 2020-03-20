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

			//start http listener
			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://localhost:63321/");
			httpListener.Start();
			Console.WriteLine("Listening on https://localhost:63321/");

			while (true)
			{
				//wait for next request
				var requestContext = await httpListener.GetContextAsync();

				//read request data
				Stream requestStream = requestContext.Request.InputStream;
				string receivedQuery = new StreamReader(requestStream).ReadToEnd();
				requestStream.Dispose();

				Console.WriteLine($"Query: { receivedQuery }");

				//run recieved query
				var queryResponse = new QueryResult();

				try
				{
					queryResponse = ExecuteQuery(receivedQuery);
				}
				catch (Exception e)
				{
					queryResponse.Error = true;
					queryResponse.ErrorText = e.Message;
				}

				//construct JSON and response
				string json = JsonSerializer.Serialize(queryResponse);
				Console.WriteLine($"Json:  { json }");

				requestContext.Response.StatusCode = 200;
				var responseStreamWriter = new StreamWriter(requestContext.Response.OutputStream);
				responseStreamWriter.Write(json);

				//send response
				responseStreamWriter.Dispose();
				requestContext.Response.OutputStream.Dispose();
				requestContext.Response.Close();
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
				queryResult.ColumnNames.Add(colName);
			}

			//each row in the table
			while (dataReader.Read())
			{
				var row = new List<string>();

				//each column in row
				for (int i = 0; i < columns; i++)
				{
					object obj = dataReader.GetValue(i);

					if (obj == DBNull.Value)
					{
						obj = "NULL";
					}

					row.Add(obj.ToString());
				}
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
