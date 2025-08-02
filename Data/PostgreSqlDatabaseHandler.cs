using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace SqlAPI.Data
{
    /// <summary>
    /// Ein typsicherer, generischer Datenbank-Handler für PostgreSQL, der grundlegende CRUD-Operationen bereitstellt.
    /// </summary>
    public class PostgreSqlDatabaseHandler : IDisposable
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _connection;
        private NpgsqlTransaction? _transaction;
        private bool _disposed = false;

        public PostgreSqlDatabaseHandler(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _connection = new NpgsqlConnection(_connectionString);
        }

        // --- Verbindungs- und Transaktionsmanagement (unverändert) ---
        public async Task OpenConnectionAsync()
        {
            if (_connection != null && _connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        public async Task CloseConnectionAsync()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
        }
        
        public async Task<NpgsqlTransaction> BeginTransactionAsync()
        {
            await OpenConnectionAsync();
            if (_connection == null)
                throw new InvalidOperationException("Database connection is not available.");
            _transaction = await _connection.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                _transaction?.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction = null;
            }
        }

        // --- Low-Level Ausführungsmethoden (bleiben für komplexe Fälle erhalten) ---
        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            await OpenConnectionAsync();
            var dataTable = new DataTable();
            using (var command = CreateCommand(sql, parameters))
            using (var reader = await command.ExecuteReaderAsync())
            {
                dataTable.Load(reader);
            }
            return dataTable;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, parameters))
            {
                return await command.ExecuteScalarAsync();
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, parameters))
            {
                return await command.ExecuteNonQueryAsync();
            }
        }

        // --- NEUE, TYPSICHERE GENERISCHE METHODEN ---

        /// <summary>
        /// Ruft eine einzelne Entität anhand ihrer Id ab (Typsicher).
        /// </summary>
        public async Task<T?> GetByIdAsync<T>(object id) where T : class, new()
        {
            string tableName = GetTableName<T>();
            string sql = $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = @Id;";
            var parameters = new Dictionary<string, object> { { "@Id", id } };

            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, parameters))
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
            {
                if (await reader.ReadAsync())
                {
                    return MapToEntity<T>(reader);
                }
            }
            return null;
        }

        /// <summary>
        /// Ruft alle Entitäten eines Typs ab (Typsicher).
        /// </summary>
        public async Task<List<T>> GetAllAsync<T>() where T : class, new()
        {
            string tableName = GetTableName<T>();
            string sql = $"SELECT * FROM \"{tableName}\";";
            
            var list = new List<T>();
            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, null))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    list.Add(MapToEntity<T>(reader));
                }
            }
            return list;
        }

        /// <summary>
        /// Fügt ein typisiertes Objekt als neuen Datensatz in eine Tabelle ein (Typsicher).
        /// </summary>
        public async Task<object?> InsertAsync<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string tableName = GetTableName<T>();
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id"); // Annahme: Id wird von der DB generiert

            var columnNames = properties.Select(p => $"\"{p.Name}\"");
            var parameterNames = properties.Select(p => $"@{p.Name}");
            var queryParams = properties.ToDictionary(p => $"@{p.Name}", p => p.GetValue(entity) ?? DBNull.Value);

            string sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)}) RETURNING \"Id\";";
            return await ExecuteScalarAsync(sql, queryParams);
        }

        /// <summary>
        /// Aktualisiert einen bestehenden Datensatz in einer Tabelle basierend auf der Id (Typsicher).
        /// </summary>
        public async Task<int> UpdateAsync<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string tableName = GetTableName<T>();
            var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");
            var idProperty = typeof(T).GetProperty("Id");

            if (idProperty == null) throw new InvalidOperationException($"Der Typ {typeof(T).Name} muss eine 'Id'-Eigenschaft für das Update besitzen.");

            var idValue = idProperty.GetValue(entity);
            var setClauses = properties.Select(p => $"\"{p.Name}\" = @{p.Name}");
            var queryParams = properties.ToDictionary(p => $"@{p.Name}", p => p.GetValue(entity) ?? DBNull.Value);
#pragma warning disable CS8604 // Possible null reference argument.
            queryParams.Add("@Id", idValue);
#pragma warning restore CS8604 // Possible null reference argument.

            string sql = $"UPDATE \"{tableName}\" SET {string.Join(", ", setClauses)} WHERE \"Id\" = @Id;";
            return await ExecuteNonQueryAsync(sql, queryParams);
        }

        /// <summary>
        /// Löscht einen Datensatz aus einer Tabelle anhand seiner Id (Typsicher).
        /// </summary>
        public async Task<int> DeleteAsync<T>(object id) where T : class
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            string tableName = GetTableName<T>();
            var queryParams = new Dictionary<string, object> { { "@Id", id } };
            string sql = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @Id;";
            return await ExecuteNonQueryAsync(sql, queryParams);
        }

        /// <summary>
        /// Fügt eine Entität in die Datenbank ein und schließt automatisch die Verbindung.
        /// Diese Methode ist ideal für einmalige Einfügungen ohne weitere Operationen.
        /// </summary>
        public async Task<object?> InsertAndCloseAsync<T>(T entity) where T : class
        {
            try
            {
                if (entity == null) throw new ArgumentNullException(nameof(entity));

                string tableName = GetTableName<T>();
                var properties = typeof(T).GetProperties().Where(p => p.Name != "Id");

                var columnNames = properties.Select(p => $"\"{p.Name}\"");
                var parameterNames = properties.Select(p => $"@{p.Name}");
                var queryParams = properties.ToDictionary(p => $"@{p.Name}", p => p.GetValue(entity) ?? DBNull.Value);

                string sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)}) RETURNING \"Id\";";
                
                await OpenConnectionAsync();
                using (var command = CreateCommand(sql, queryParams))
                {
                    return await command.ExecuteScalarAsync();
                }
            }
            finally
            {
                await CloseConnectionAsync();
            }
        }

        /// <summary>
        /// Erstellt eine Tabelle basierend auf dem angegebenen Entity-Typ.
        /// Diese Methode analysiert die Eigenschaften des Typs und erstellt entsprechende Spalten.
        /// </summary>
        public async Task CreateTableAsync<T>() where T : class
        {
            try
            {
                string tableName = GetTableName<T>();
                var properties = typeof(T).GetProperties();
                
                var columns = new List<string>();
                
                foreach (var property in properties)
                {
                    string columnDefinition = GetColumnDefinition(property);
                    columns.Add(columnDefinition);
                }

                string sql = $@"
                    CREATE TABLE IF NOT EXISTS ""{tableName}"" (
                        {string.Join(",\n                        ", columns)}
                    );";

                await OpenConnectionAsync();
                using (var command = CreateCommand(sql, null))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await CloseConnectionAsync();
            }
        }

        // --- Private Helper-Methoden ---

        private string GetTableName<T>()
        {
            // Einfache Annahme: Tabellenname ist der Klassenname im Plural.
            // Für komplexere Fälle könnte man hier Attribute verwenden.
            return typeof(T).Name + "s";
        }

        private T MapToEntity<T>(IDataRecord record) where T : new()
        {
            var entity = new T();
            var properties = typeof(T).GetProperties();

            for (int i = 0; i < record.FieldCount; i++)
            {
                var property = properties.FirstOrDefault(p => p.Name.Equals(record.GetName(i), StringComparison.OrdinalIgnoreCase));
                if (property != null && !record.IsDBNull(i))
                {
                    var value = record.GetValue(i);
                    property.SetValue(entity, value);
                }
            }
            return entity;
        }

        private string GetColumnDefinition(System.Reflection.PropertyInfo property)
        {
            string columnName = $"\"{property.Name}\"";
            string dataType = GetPostgreSqlDataType(property.PropertyType);
            
            // Spezielle Behandlung für Id-Spalte
            if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                return $"{columnName} SERIAL PRIMARY KEY";
            }
            
            // Überprüfe ob die Eigenschaft nullable ist
            bool isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null || 
                             !property.PropertyType.IsValueType;
            
            string nullConstraint = isNullable ? "" : " NOT NULL";
            
            return $"{columnName} {dataType}{nullConstraint}";
        }

        private string GetPostgreSqlDataType(Type type)
        {
            // Handle nullable types
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            
            return underlyingType.Name switch
            {
                nameof(Int32) => "INTEGER",
                nameof(Int64) => "BIGINT",
                nameof(String) => "TEXT",
                nameof(Boolean) => "BOOLEAN",
                nameof(DateTime) => "TIMESTAMP",
                nameof(Decimal) => "DECIMAL",
                nameof(Double) => "DOUBLE PRECISION",
                nameof(Single) => "REAL",
                nameof(Guid) => "UUID",
                _ => "TEXT" // Fallback für unbekannte Typen
            };
        }

        private NpgsqlCommand CreateCommand(string sql, Dictionary<string, object>? parameters)
        {
            var command = new NpgsqlCommand(sql, _connection);
            if (_transaction != null) command.Transaction = _transaction;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            return command;
        }

        // --- IDisposable Implementierung ---
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}