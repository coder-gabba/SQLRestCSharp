using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SqlAPI.Data
{
    /// <summary>
    /// Ein typsicherer, generischer Datenbank-Handler für PostgreSQL, der grundlegende CRUD-Operationen bereitstellt.
    /// Optimiert mit Caching für bessere Performance.
    /// </summary>
    public class PostgreSqlDatabaseHandler : IDisposable
    {
        // --- Caching for Reflection Performance ---
        private static readonly ConcurrentDictionary<Type, string> TableNameCache = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> NonIdPropertiesCache = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdPropertyCache = new();

        private readonly string _connectionString;
        private readonly ILogger<PostgreSqlDatabaseHandler> _logger;
        private NpgsqlConnection? _connection;
        private NpgsqlTransaction? _transaction;
        private bool _disposed = false;

        public PostgreSqlDatabaseHandler(string connectionString, ILogger<PostgreSqlDatabaseHandler> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
            _connection = new NpgsqlConnection(_connectionString);
        }

        // --- Verbindungs- und Transaktionsmanagement ---
        public async Task OpenConnectionAsync()
        {
            if (_connection != null && _connection.State != ConnectionState.Open)
            {
                _logger.LogDebug("Opening database connection");
                await _connection.OpenAsync();
                _logger.LogDebug("Database connection opened successfully");
            }
        }

        public async Task CloseConnectionAsync()
        {
            if (_connection != null && _connection.State == ConnectionState.Open)
            {
                _logger.LogDebug("Closing database connection");
                await _connection.CloseAsync();
                _logger.LogDebug("Database connection closed successfully");
            }
        }
        
        public async Task<NpgsqlTransaction> BeginTransactionAsync()
        {
            _logger.LogDebug("Beginning database transaction");
            await OpenConnectionAsync();
            if (_connection == null)
                throw new InvalidOperationException("Database connection is not available.");
            _transaction = await _connection.BeginTransactionAsync();
            _logger.LogInformation("Database transaction started");
            return _transaction;
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogDebug("Committing database transaction");
                await _transaction.CommitAsync();
                _transaction?.Dispose();
                _transaction = null;
                _logger.LogInformation("Database transaction committed successfully");
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogWarning("Rolling back database transaction");
                await _transaction.RollbackAsync();
                _transaction = null;
                _logger.LogInformation("Database transaction rolled back");
            }
        }

        // --- Low-Level Ausführungsmethoden ---
        public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            _logger.LogDebug("Executing query: {Sql}", sql);
            await OpenConnectionAsync();
            var dataTable = new DataTable();
            using (var command = CreateCommand(sql, parameters))
            using (var reader = await command.ExecuteReaderAsync())
            {
                dataTable.Load(reader);
                _logger.LogInformation("Query executed successfully. {RowCount} rows returned.", dataTable.Rows.Count);
            }
            return dataTable;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            _logger.LogDebug("Executing scalar query: {Sql}", sql);
            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, parameters))
            {
                try
                {
                    var result = await command.ExecuteScalarAsync();
                    _logger.LogInformation("Scalar query executed successfully. Result: {Result}", result);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing scalar query: {Sql}", sql);
                    throw;
                }
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            _logger.LogDebug("Executing NonQuery: {Sql}", sql);
            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, parameters))
            {
                try
                {
                    var affectedRows = await command.ExecuteNonQueryAsync();
                    _logger.LogInformation("NonQuery executed successfully. {AffectedRows} rows affected.", affectedRows);
                    return affectedRows;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing NonQuery: {Sql}", sql);
                    throw;
                }
            }
        }

        // --- TYPSICHERE GENERISCHE METHODEN ---

        /// <summary>
        /// Ruft eine einzelne Entität anhand ihrer Id ab (Typsicher).
        /// </summary>
        public async Task<T?> GetByIdAsync<T>(object id) where T : class, new()
        {
            string tableName = GetTableName<T>();
            _logger.LogDebug("Getting entity by ID. Type: {Type}, Table: {Table}, Id: {Id}", typeof(T).Name, tableName, id);
            
            string sql = $"SELECT * FROM \"{tableName}\" WHERE \"Id\" = @Id;";
            var parameters = new Dictionary<string, object> { { "@Id", id } };

            await OpenConnectionAsync();
            using (var command = CreateCommand(sql, parameters))
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
            {
                if (await reader.ReadAsync())
                {
                    var entity = MapToEntity<T>(reader);
                    _logger.LogInformation("Entity found. Type: {Type}, Id: {Id}", typeof(T).Name, id);
                    return entity;
                }
            }
            _logger.LogInformation("Entity not found. Type: {Type}, Id: {Id}", typeof(T).Name, id);
            return null;
        }

        /// <summary>
        /// Ruft alle Entitäten eines Typs ab (Typsicher).
        /// </summary>
        public async Task<List<T>> GetAllAsync<T>() where T : class, new()
        {
            string tableName = GetTableName<T>();
            _logger.LogDebug("Getting all entities. Type: {Type}, Table: {Table}", typeof(T).Name, tableName);
            
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
            _logger.LogInformation("Retrieved {Count} entities. Type: {Type}", list.Count, typeof(T).Name);
            return list;
        }

        /// <summary>
        /// Fügt ein typisiertes Objekt als neuen Datensatz in eine Tabelle ein (Typsicher).
        /// </summary>
        public async Task<object?> InsertAsync<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string tableName = GetTableName<T>();
            var properties = GetNonIdProperties<T>();
            
            _logger.LogDebug("Inserting entity. Type: {Type}, Table: {Table}", typeof(T).Name, tableName);

            var columnNames = properties.Select(p => $"\"{p.Name}\"");
            var parameterNames = properties.Select(p => $"@{p.Name}");
            var queryParams = properties.ToDictionary(p => $"@{p.Name}", p => p.GetValue(entity) ?? DBNull.Value);

            string sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)}) RETURNING \"Id\";";
            
            try
            {
                var result = await ExecuteScalarAsync(sql, queryParams);
                _logger.LogInformation("Entity inserted successfully. Type: {Type}, Id: {Id}", typeof(T).Name, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting entity. Type: {Type}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Aktualisiert einen bestehenden Datensatz in einer Tabelle basierend auf der Id (Typsicher).
        /// </summary>
        public async Task<int> UpdateAsync<T>(T entity) where T : class
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string tableName = GetTableName<T>();
            var properties = GetNonIdProperties<T>();
            var idProperty = GetIdProperty<T>();

            if (idProperty == null) throw new InvalidOperationException($"Der Typ {typeof(T).Name} muss eine 'Id'-Eigenschaft für das Update besitzen.");

            var idValue = idProperty.GetValue(entity);
            _logger.LogDebug("Updating entity. Type: {Type}, Table: {Table}, Id: {Id}", typeof(T).Name, tableName, idValue);
            
            var setClauses = properties.Select(p => $"\"{p.Name}\" = @{p.Name}");
            var queryParams = properties.ToDictionary(p => $"@{p.Name}", p => p.GetValue(entity) ?? DBNull.Value);
            queryParams.Add("@Id", idValue!);

            string sql = $"UPDATE \"{tableName}\" SET {string.Join(", ", setClauses)} WHERE \"Id\" = @Id;";
            
            try
            {
                var result = await ExecuteNonQueryAsync(sql, queryParams);
                _logger.LogInformation("Entity updated successfully. Type: {Type}, Id: {Id}, Rows affected: {RowsAffected}", 
                    typeof(T).Name, idValue, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity. Type: {Type}, Id: {Id}", typeof(T).Name, idValue);
                throw;
            }
        }

        /// <summary>
        /// Löscht einen Datensatz aus einer Tabelle anhand seiner Id (Typsicher).
        /// </summary>
        public async Task<int> DeleteAsync<T>(object id) where T : class
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            string tableName = GetTableName<T>();
            _logger.LogDebug("Deleting entity. Type: {Type}, Table: {Table}, Id: {Id}", typeof(T).Name, tableName, id);
            
            var queryParams = new Dictionary<string, object> { { "@Id", id } };
            string sql = $"DELETE FROM \"{tableName}\" WHERE \"Id\" = @Id;";
            
            try
            {
                var result = await ExecuteNonQueryAsync(sql, queryParams);
                _logger.LogInformation("Entity deleted successfully. Type: {Type}, Id: {Id}, Rows affected: {RowsAffected}", 
                    typeof(T).Name, id, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity. Type: {Type}, Id: {Id}", typeof(T).Name, id);
                throw;
            }
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
                var properties = GetNonIdProperties<T>();
                
                _logger.LogDebug("Inserting entity with auto-close. Type: {Type}, Table: {Table}", typeof(T).Name, tableName);

                var columnNames = properties.Select(p => $"\"{p.Name}\"");
                var parameterNames = properties.Select(p => $"@{p.Name}");
                var queryParams = properties.ToDictionary(p => $"@{p.Name}", p => p.GetValue(entity) ?? DBNull.Value);

                string sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)}) RETURNING \"Id\";";
                
                await OpenConnectionAsync();
                using (var command = CreateCommand(sql, queryParams))
                {
                    var result = await command.ExecuteScalarAsync();
                    _logger.LogInformation("Entity inserted successfully with auto-close. Type: {Type}, Id: {Id}", typeof(T).Name, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting entity with auto-close. Type: {Type}", typeof(T).Name);
                throw;
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
                var properties = GetProperties<T>();
                
                _logger.LogDebug("Creating table. Type: {Type}, Table: {Table}", typeof(T).Name, tableName);
                
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
                    _logger.LogInformation("Table created successfully. Type: {Type}, Table: {Table}", typeof(T).Name, tableName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating table. Type: {Type}", typeof(T).Name);
                throw;
            }
            finally
            {
                await CloseConnectionAsync();
            }
        }

        // --- Private Helper-Methoden mit Caching ---

        private string GetTableName<T>()
        {
            return TableNameCache.GetOrAdd(typeof(T), type =>
            {
                var tableName = type.Name + "s";
                _logger.LogTrace("Cached table name for type {Type}: {TableName}", type.Name, tableName);
                return tableName;
            });
        }

        private PropertyInfo[] GetProperties<T>()
        {
            return PropertiesCache.GetOrAdd(typeof(T), type =>
            {
                var properties = type.GetProperties();
                _logger.LogTrace("Cached {Count} properties for type {Type}", properties.Length, type.Name);
                return properties;
            });
        }

        private PropertyInfo[] GetNonIdProperties<T>()
        {
            return NonIdPropertiesCache.GetOrAdd(typeof(T), type =>
            {
                var properties = type.GetProperties().Where(p => p.Name != "Id").ToArray();
                _logger.LogTrace("Cached {Count} non-ID properties for type {Type}", properties.Length, type.Name);
                return properties;
            });
        }

        private PropertyInfo? GetIdProperty<T>()
        {
            return IdPropertyCache.GetOrAdd(typeof(T), type =>
            {
                var property = type.GetProperty("Id");
                _logger.LogTrace("Cached ID property for type {Type}: {Found}", type.Name, property != null);
                return property;
            });
        }

        private T MapToEntity<T>(IDataRecord record) where T : new()
        {
            var entity = new T();
            var properties = GetProperties<T>();

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

        private string GetColumnDefinition(PropertyInfo property)
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
                    _logger.LogDebug("Disposing PostgreSqlDatabaseHandler");
                    _transaction?.Dispose();
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}