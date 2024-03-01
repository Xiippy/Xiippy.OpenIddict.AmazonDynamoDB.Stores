using Amazon.DynamoDBv2.DataModel;
using OpenIddict.AmazonDynamoDB.DynamoDbTypeConverters;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbToken
{
  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => $"TOKEN";
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"#TOKEN#{Id}";
    set { }
  }
  public virtual string? ApplicationId { get; set; }
  public virtual string? AuthorizationId { get; set; }
  public virtual string? ConcurrencyToken { get; set; }
    = Guid.NewGuid().ToString();
  [DynamoDBProperty(typeof(DynamoDBDateTimeOffset))]
  public virtual DateTimeOffset? CreationDate { get; set; }

  [DynamoDBProperty(typeof(DynamoDBDateTimeOffset))]

  public virtual DateTimeOffset? ExpirationDate { get; set; }
  public virtual string Id { get; set; } = Guid.NewGuid().ToString();
  public virtual string? Payload { get; set; }
  public virtual string? Properties { get; set; }
  [DynamoDBProperty(typeof(DynamoDBDateTimeOffset))]

  public virtual DateTimeOffset? RedemptionDate { get; set; }
  public virtual string? ReferenceId { get; set; }
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
