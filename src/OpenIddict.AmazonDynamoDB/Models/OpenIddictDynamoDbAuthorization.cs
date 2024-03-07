using Amazon.DynamoDBv2.DataModel;
using OpenIddict.AmazonDynamoDB.DynamoDbTypeConverters;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbAuthorization
{
  public const string Authorization_SortKeyPrefix = "#AUTHORIZATION#";
  public const string Authorization_PartitionKey = "AUTHORIZATION";

  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => Authorization_PartitionKey;
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"{Authorization_SortKeyPrefix}{Id}";
    set { }
  }
  public virtual string Id { get; set; }
    = Guid.NewGuid().ToString();
  public virtual string? ApplicationId { get; set; }
  public virtual string? ConcurrencyToken { get; set; }
    = Guid.NewGuid().ToString();
  public virtual DateTime? CreationDate { get; set; }
  public virtual string? Properties { get; set; }
  public virtual List<string>? Scopes { get; set; } = new List<string>();
  public virtual string? Status { get; set; }
  public virtual string? Subject { get; set; }
  public virtual string? Type { get; set; }
  public string? SearchKey
  {
    get => $"APPLICATION#{ApplicationId}#STATUS#{Status}#TYPE#{Type}";
    set { }
  }
  //[DynamoDBProperty("ttl", storeAsEpoch: true)]
  [DynamoDBProperty(typeof(DynamoDBDateTimeOffset))]
  public DateTimeOffset? TTL { get; set; }
}
