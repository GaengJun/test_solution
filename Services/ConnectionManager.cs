using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Threading;

namespace MES.Solution.Services
{
    public sealed class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> lazy =
            new Lazy<ConnectionManager>(() => new ConnectionManager());

        public static ConnectionManager Instance => lazy.Value;

        private readonly string _connectionString;
        private readonly ConcurrentQueue<MySqlConnection> _connectionPool;
        private readonly SemaphoreSlim _poolSemaphore;
        private const int MAX_POOL_SIZE = 20;
        private const int MIN_POOL_SIZE = 5;

        private ConnectionManager()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            _connectionPool = new ConcurrentQueue<MySqlConnection>();
            _poolSemaphore = new SemaphoreSlim(MAX_POOL_SIZE);
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < MIN_POOL_SIZE; i++)
            {
                var connection = new MySqlConnection(_connectionString);
                _connectionPool.Enqueue(connection);
            }
        }

        public MySqlConnection GetConnection()
        {
            _poolSemaphore.Wait();

            MySqlConnection connection;
            if (_connectionPool.TryDequeue(out connection))
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    try
                    {
                        connection.Open();
                    }
                    catch (Exception)
                    {
                        connection = new MySqlConnection(_connectionString);
                        connection.Open();
                    }
                }
                return connection;
            }

            connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public void ReleaseConnection(MySqlConnection connection)
        {
            if (connection == null) return;

            try
            {
                if (_connectionPool.Count < MAX_POOL_SIZE)
                {
                    _connectionPool.Enqueue(connection);
                }
                else
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
            finally
            {
                _poolSemaphore.Release();
            }
        }

        public void CloseAllConnections()
        {
            while (_connectionPool.TryDequeue(out MySqlConnection connection))
            {
                try
                {
                    connection.Close();
                    connection.Dispose();
                }
                catch { }
            }
        }
    }
}