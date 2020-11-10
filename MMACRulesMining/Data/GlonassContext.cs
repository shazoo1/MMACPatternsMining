using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace MMACRulesMining.Data
{
	public partial class GlonassContext : DbContext
	{
		private string _connectionString;

		public GlonassContext(string connectionString = "host=localhost;database=glonass;user id=postgres;password=12345678")
		{
			_connectionString = connectionString;
		}

		public GlonassContext(DbContextOptions<GlonassContext> options)
			: base(options)
		{
		}

		public virtual DbSet<Wfilled> Wfilled { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
				optionsBuilder.UseNpgsql(_connectionString);
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Wfilled>(entity =>
			{
				entity.HasNoKey();

				entity.ToTable("wfilled");

				entity.Property(e => e.Clheight).HasColumnName("clheight");

				entity.Property(e => e.Cloudiness).HasColumnName("cloudiness");

				entity.Property(e => e.Datetime).HasColumnName("datetime");

				entity.Property(e => e.Droppoint)
					.HasColumnName("droppoint")
					.HasColumnType("numeric");

				entity.Property(e => e.Event1).HasColumnName("event1");

				entity.Property(e => e.Event2).HasColumnName("event2");

				entity.Property(e => e.Humid).HasColumnName("humid");

				entity.Property(e => e.Maxtemp)
					.HasColumnName("maxtemp")
					.HasColumnType("numeric");

				entity.Property(e => e.Mingroundtemp)
					.HasColumnName("mingroundtemp")
					.HasColumnType("numeric");

				entity.Property(e => e.Mintemp)
					.HasColumnName("mintemp")
					.HasColumnType("numeric");

				entity.Property(e => e.Precip).HasColumnName("precip");

				entity.Property(e => e.Press)
					.HasColumnName("press")
					.HasColumnType("numeric");

				entity.Property(e => e.Sight)
					.HasColumnName("sight")
					.HasColumnType("numeric");

				entity.Property(e => e.Snowdepth).HasColumnName("snowdepth");

				entity.Property(e => e.Surface).HasColumnName("surface");

				entity.Property(e => e.Temp)
					.HasColumnName("temp")
					.HasColumnType("numeric");

				entity.Property(e => e.Winddir).HasColumnName("winddir");

				entity.Property(e => e.Windgust).HasColumnName("windgust");

				entity.Property(e => e.Windspeed).HasColumnName("windspeed");
			});

			OnModelCreatingPartial(modelBuilder);
		}

		public List<string> GetTableColumnNames(DataTable table, string prefix = "")
		{
			List<string> columnNames = new List<string>();
			foreach (DataColumn col in table.Columns)
			{
				columnNames.Add(prefix + col.ColumnName);
			}
			return columnNames;
		}

		public void CreateAsJoin(string leftTable, string leftKey, string rightTable, string rightKey, 
			string newTableName, bool replace = false)
		{
			using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
			{
				string commandText = "CREATE TABLE @newTable AS SELECT * FROM @leftTable " +
					"INNER JOIN @rightTable ON @leftKey = @rightKey;";
				using(NpgsqlCommand command = new NpgsqlCommand(commandText, connection))
				{
					command.Parameters.Add(new NpgsqlParameter("@newTable", newTableName));
					command.Parameters.Add(new NpgsqlParameter("@leftTable", leftTable));
					command.Parameters.Add(new NpgsqlParameter("@rightTable", rightTable));
					command.Parameters.Add(new NpgsqlParameter("@leftKey", leftKey));
					command.Parameters.Add(new NpgsqlParameter("@rightKey", rightKey));

					command.ExecuteNonQuery();
				}
			}
		}

		public void SaveToTable(DataTable table, bool replace = false)
		{
			using (NpgsqlConnection con = new Npgsql.NpgsqlConnection(_connectionString))
			{
				bool exists;
				con.Open();

				using (NpgsqlCommand command = new NpgsqlCommand("SELECT EXISTS ( "
					+ "SELECT FROM information_schema.tables "
					+ "WHERE  table_schema = 'public' "
					+ "AND    table_name = @tableName "
					+ "); ", con))
				{
					command.Parameters.Add(new NpgsqlParameter("@tableName", table.TableName));
					exists = bool.Parse(command.ExecuteScalar().ToString());
				}

				if (exists && replace)
				{
					using (NpgsqlCommand command = new NpgsqlCommand("DROP TABLE " + table.TableName, con))
					{
						command.ExecuteNonQuery();
						exists = false;
					}
				}

				List<string> columnNames = GetTableColumnNames(table);
				List<NpgsqlParameter> columnNamesParameters = GetTableColumnNamesAsParameters(table);

				string createCommand = "CREATE TABLE " + table.TableName + "(";
				foreach (var par in columnNamesParameters)
				{
					if (par.Value == "DateTime")
						createCommand += par.Value + " TIMESTAMP, ";
					else
						createCommand += par.Value + " TEXT, ";
				}
				createCommand = createCommand.TrimEnd(',', ' ');
				createCommand += ");";

				using (NpgsqlCommand command = new NpgsqlCommand(createCommand, con))
				{
					command.ExecuteNonQuery();
				}

				string insertCommandFormat = "INSERT INTO " + table.TableName + " VALUES ({0})";

				foreach (DataRow row in table.Rows)
				{
					string paramString = "";

					List<(string colName, NpgsqlParameter param)> values = GetParametersFromRow(row);
					for (int i = 0; i < values.Count; i++)
					{
						paramString += values[i].param.ParameterName + ", ";
					}
					paramString = paramString.TrimEnd(',', ' ');

					string insertCommand = string.Format(insertCommandFormat, paramString);

					using (NpgsqlCommand command = new NpgsqlCommand(insertCommand, con))
					{
						command.Parameters.AddRange(values.Select(x => x.param).ToArray());
						command.ExecuteNonQuery();
					}
				}
			}
		}

		private List<NpgsqlParameter> GetTableColumnNamesAsParameters(DataTable table, string prefix = "")
		{
			List<NpgsqlParameter> columnNameParameters = new List<NpgsqlParameter>();
			for (int i = 0; i < table.Columns.Count; i++)
			{
				columnNameParameters.Add(new NpgsqlParameter("@" + i, table.Columns[i].ColumnName));
			}
			return columnNameParameters;
		}

		private List<(string, NpgsqlParameter)> GetParametersFromRow(DataRow row)
		{
			List<(string, NpgsqlParameter)> parameters = new List<(string, NpgsqlParameter)>();
			for (int i = 0; i < row.Table.Columns.Count; i++)
			{
				var colName = row.Table.Columns[i].ColumnName;
				parameters.Add((colName, new NpgsqlParameter("@" + i, row[i])));
			}
			return parameters;
		}

		public List<string> GetTableColumnNames(string tableName, NpgsqlConnection connection = null)
		{
			bool dispose;
			if (dispose = connection == null)
				connection = new Npgsql.NpgsqlConnection("Server=127.0.0.1;Port=5432;Database=glonass;" +
				"User Id=postgres;Password=12345678;");
			try
			{
				if (connection.State != ConnectionState.Open)
					connection.Open();
				List<string> columnNames = new List<string>();
				using (NpgsqlCommand command = new NpgsqlCommand("SELECT column_name "
					+ "FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName; ", connection))
				{
					command.Parameters.Add(new NpgsqlParameter("@tableName", tableName));
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							columnNames.Add(reader.GetString(0));
						}
						return columnNames;
					}
				}
			}
			catch (Exception e)
			{
				
			}
			finally
			{
				if (dispose)
				{
					connection.Close();
					connection.Dispose();
				}
			}
			return null;
		}

		public bool SelectWithinPolygon(string polygon, string latCol, string lonCol, out DataTable dataTable)
		{
			string prequery = "SELECT * FROM \"Dataset_Kazan_filtered\" INNER JOIN featured_weather wf " +
				"ON ceil_time_3h(\"Dataset_Kazan_filtered\".datetime) = wf.datetime " +
				"WHERE {0} @> point({1}, {2})";
			string query = string.Format(prequery, polygon, lonCol, latCol);

			using(var connection = new Npgsql.NpgsqlConnection("Server=127.0.0.1;Port=5432;Database=glonass;" +
				"User Id=postgres;Password=12345678;"))
			{
				if (connection.State != ConnectionState.Open)
					connection.Open();

				try
				{
					using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
					using (var reader = command.ExecuteReader())
					{
						var dbColumns = reader.GetColumnSchema();
						dataTable = new DataTable();
						foreach (var col in dbColumns)
						{
							if (!dataTable.Columns.Contains(col.ColumnName))
								dataTable.Columns.Add(col.ColumnName, col.DataType);
						}

						while (reader.Read())
						{
							DataRow row = dataTable.NewRow();
							foreach (var col in dbColumns)
							{
								row[col.ColumnName] = reader[col.ColumnName];
							}
							dataTable.Rows.Add(row);
						}
						return true;
					}
				}
				catch(Exception e)
				{
					Debug.WriteLine(e);
					dataTable = null;
					return false;
				}
			}
		}

		partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
	}
}
