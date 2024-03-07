using Amazon.DynamoDBv2.DataModel;

namespace OpenIddict.AmazonDynamoDB;

[DynamoDBTable(Constants.DefaultTableName)]
public class OpenIddictDynamoDbApplication
{

  public const string APPLICATION_SortKeyPrefix = "APPLICATION#";
  public const string APPLICATION_PartitionKey = "APPLICATION";


  [DynamoDBHashKey]
  public string PartitionKey
  {
    get => APPLICATION_PartitionKey;
    private set { }
  }
  [DynamoDBRangeKey]
  public string? SortKey
  {
    get => $"{APPLICATION_SortKeyPrefix}{Id}";
    set { }
  }
  public virtual string Id { get; set; } = Guid.NewGuid().ToString();
  public virtual string? ClientId { get; set; }
  public virtual string? ClientSecret { get; set; }
  public virtual string ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();
  public virtual string? ConsentType { get; set; }
  public virtual string? DisplayName { get; set; }
  public virtual Dictionary<string, string>? DisplayNames { get; set; }
    = new Dictionary<string, string>();
  public virtual List<string>? Permissions { get; set; } = new List<string>();
  public virtual List<string>? PostLogoutRedirectUris { get; set; } = new List<string>();
  public virtual string? Properties { get; set; }
  public virtual List<string>? RedirectUris { get; set; } = new List<string>();
  public virtual List<string>? Requirements { get; set; } = new List<string>();
  public virtual string? Type { get; set; }

  public virtual string? ApplicationType { get; set; }

  

  public virtual Dictionary<string, string>? Settings { get; set; }
    = new Dictionary<string, string>();


  //[DynamoDBProperty(typeof(DynamoDBJsonWebKeySetConverter))]
  public virtual string? JsonWebKeySetStr { get; set; }

}
