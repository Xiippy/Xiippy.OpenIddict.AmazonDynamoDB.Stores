using System.Diagnostics;
using Amazon.DynamoDBv2;

namespace OpenIddict.AmazonDynamoDB.Tests;

public class DatabaseFixture : IDisposable
{
  public static readonly string TableName = Guid.NewGuid().ToString();
  public readonly AmazonDynamoDBClient Client = new(new AmazonDynamoDBConfig
  {
    ServiceURL = "http://localhost:8000",
  });
  private bool _disposed;

  public DatabaseFixture()
  {
    CreateTable().GetAwaiter().GetResult();
  }

  private async Task CreateTable()
  {

    Console.WriteLine($"TableName:{TableName}");
    Debug.WriteLine($"TableName:{TableName}");

    await OpenIddictDynamoDbSetup.EnsureInitializedAsync(TestUtils.GetOptions(new()
    {
      Database = Client,
      DefaultTableName = TableName,
    }));
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    Client.DeleteTableAsync(TableName).GetAwaiter().GetResult();
    Client.Dispose();
    _disposed = true;
  }
}
