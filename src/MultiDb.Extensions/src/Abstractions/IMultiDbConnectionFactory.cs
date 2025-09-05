using System.Data.Common;

namespace MultiDb.Extensions.Abstractions
{
    public interface IMultiDbConnectionFactory
    {
        DbConnection CreateConnection(string databaseName);
    }
}

