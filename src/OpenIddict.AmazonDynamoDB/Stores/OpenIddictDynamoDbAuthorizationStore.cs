﻿using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.AmazonDynamoDB.DynamoDbTypeConverters;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIddict.AmazonDynamoDB;

public class OpenIddictDynamoDbAuthorizationStore<TAuthorization> : IOpenIddictAuthorizationStore<TAuthorization>
    where TAuthorization : OpenIddictDynamoDbAuthorization, new()
{
  private IAmazonDynamoDB _client;
  private IDynamoDBContext _context;

  public OpenIddictDynamoDbAuthorizationStore(
    IOptionsMonitor<OpenIddictDynamoDbOptions> optionsMonitor,
    IAmazonDynamoDB? database = default)
  {
    ArgumentNullException.ThrowIfNull(optionsMonitor);

    var options = optionsMonitor.CurrentValue;
    DynamoDbTableSetup.EnsureAliasCreated(options);

    if (database == default)
    {
      ArgumentNullException.ThrowIfNull(options.Database);
    }

    _client = database ?? options.Database!;
    _context = new DynamoDBContext(_client);
  }

 

  public async ValueTask<long> CountAsync(CancellationToken cancellationToken)
  {
    var count = new CountModel(CountType.Authorization);
    count = await _context.LoadAsync<CountModel>(count.PartitionKey, count.SortKey, cancellationToken);

    return count?.Count ?? 0;
  }

  public ValueTask<long> CountAsync<TResult>(Func<IQueryable<TAuthorization>, IQueryable<TResult>> query, CancellationToken cancellationToken)
  {
    throw new NotSupportedException();
  }

  public async ValueTask CreateAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    await _context.SaveAsync(authorization, cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Authorization, count + 1), cancellationToken);
  }

  public async ValueTask DeleteAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    await _context.DeleteAsync(authorization, cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Authorization, count - 1), cancellationToken);
  }

  private IAsyncEnumerable<TAuthorization> FindBySubjectAndSearchKey(string subject, string searchKey, CancellationToken cancellationToken)
  {
    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TAuthorization>(new()
      {
        IndexName = "Subject-index",
        KeyExpression = new()
        {
          ExpressionStatement = "Subject = :subject and begins_with(SearchKey, :searchKey)",
          ExpressionAttributeValues = new()
          {
            { ":subject", subject },
            { ":searchKey", searchKey },
          }
        },
      });


      do
      {
        var getNextBatch = search.GetNextSetAsync(cancellationToken);
        var docList = await getNextBatch;
        foreach (var item in docList)
        {
          yield return item;
        }


      } while (!search.IsDone);




   
    }
  }

  public IAsyncEnumerable<TAuthorization> FindAsync(
    string subject, string client, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);

    return FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}", cancellationToken);
  }

  public IAsyncEnumerable<TAuthorization> FindAsync(
    string subject, string client, string status, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);
    ArgumentNullException.ThrowIfNull(status);

    return FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}#STATUS#{status}", cancellationToken);
  }

  public IAsyncEnumerable<TAuthorization> FindAsync(
    string subject, string client, string status, string type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);
    ArgumentNullException.ThrowIfNull(status);
    ArgumentNullException.ThrowIfNull(type);

    return FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}#STATUS#{status}#TYPE#{type}", cancellationToken);
  }

  public IAsyncEnumerable<TAuthorization> FindAsync(
    string subject,
    string client,
    string status,
    string type,
    ImmutableArray<string> scopes,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);
    ArgumentNullException.ThrowIfNull(client);
    ArgumentNullException.ThrowIfNull(status);
    ArgumentNullException.ThrowIfNull(type);

    if (scopes == null)
    {
      throw new ArgumentNullException(nameof(scopes));
    }

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var authorizations = FindBySubjectAndSearchKey(subject, $"APPLICATION#{client}#STATUS#{status}#TYPE#{type}", cancellationToken);

      await foreach (var authorization in authorizations)
      {
        if (Enumerable.All(scopes, scope => authorization.Scopes!.Contains(scope)))
        {
          yield return authorization;
        }
      }
    }
  }

  public IAsyncEnumerable<TAuthorization> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TAuthorization>(new()
      {
        IndexName = "ApplicationId-index",
        KeyExpression = new()
        {
          ExpressionStatement = "ApplicationId = :applicationId",
          ExpressionAttributeValues = new()
          {
            { ":applicationId", identifier },
          }
        },
      });

      do
      {
        var getNextBatch = search.GetNextSetAsync(cancellationToken);
        var docList = await getNextBatch;
        foreach (var item in docList)
        {
          yield return item;
        }


      } while (!search.IsDone);


    }
  }

  public async ValueTask<TAuthorization?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(identifier);

    return await GetBySortKey(new() { Id = identifier }, cancellationToken);
  }

  public IAsyncEnumerable<TAuthorization> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(subject);

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var search = _context.FromQueryAsync<TAuthorization>(new()
      {
        IndexName = "Subject-index",
        KeyExpression = new()
        {
          ExpressionStatement = "Subject = :subject",
          ExpressionAttributeValues = new()
          {
            { ":subject", subject },
          }
        },
      });

      do
      {
        var getNextBatch = search.GetNextSetAsync(cancellationToken);
        var docList = await getNextBatch;
        foreach (var item in docList)
        {
          yield return item;
        }


      } while (!search.IsDone);


    }
  }

  public ValueTask<string?> GetApplicationIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.ApplicationId);
  }

  public async ValueTask<TResult?> GetAsync<TState, TResult>(Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query, TState state, CancellationToken cancellationToken)
  {
    if (query is null)
    {
      throw new ArgumentNullException(nameof(query));
    }



    var AllItems = await ListAllItemsAsync(cancellationToken);
    var AllAsQ = AllItems.AsQueryable();
    var FilteredItems = query(AllAsQ, state);
    return FilteredItems.FirstOrDefault();
  }


  public async Task<IEnumerable<TAuthorization>> ListAllItemsAsync(CancellationToken cancellationToken)
  {

    var search = _context.FromQueryAsync<TAuthorization>(new QueryOperationConfig
    {
      Select = SelectValues.AllAttributes,
      KeyExpression = new Expression
      {
        ExpressionStatement = $"{nameof(OpenIddictDynamoDbAuthorization.PartitionKey)} = :PK and begins_with({nameof(OpenIddictDynamoDbAuthorization.SortKey)}, :prefix)",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        {":PK", OpenIddictDynamoDbAuthorization.Authorization_PartitionKey },
                        { ":prefix", OpenIddictDynamoDbAuthorization.Authorization_SortKeyPrefix},
                   }
      },
    });

    List<TAuthorization> allTokens = new List<TAuthorization>();
    do
    {
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }
      var getNextBatch = search.GetNextSetAsync(cancellationToken);
      var docList = await getNextBatch;
      foreach (var item in docList)
      {
        allTokens.Add(item);
      }
    } while (!search.IsDone);

    return allTokens;

  }

  public ValueTask<DateTimeOffset?> GetCreationDateAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.CreationDate);
  }

  public ValueTask<string?> GetIdAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Id);
  }

  public ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (string.IsNullOrEmpty(authorization.Properties))
    {
      return new(ImmutableDictionary.Create<string, JsonElement>());
    }

    using var document = JsonDocument.Parse(authorization.Properties);
    var properties = ImmutableDictionary.CreateBuilder<string, JsonElement>();

    foreach (var property in document.RootElement.EnumerateObject())
    {
      properties[property.Name] = property.Value.Clone();
    }

    return new(properties.ToImmutable());
  }

  public ValueTask<ImmutableArray<string>> GetScopesAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (authorization.Scopes is not { Count: > 0 })
    {
      return new(ImmutableArray.Create<string>());
    }

    return new(authorization.Scopes.ToImmutableArray());
  }

  public ValueTask<string?> GetStatusAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Status);
  }

  public ValueTask<string?> GetSubjectAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Subject);
  }

  public ValueTask<string?> GetTypeAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    return new(authorization.Type);
  }

  public ValueTask<TAuthorization> InstantiateAsync(
    CancellationToken cancellationToken)
  {
    try
    {
      return new(Activator.CreateInstance<TAuthorization>());
    }
    catch (MemberAccessException exception)
    {
      return new(Task.FromException<TAuthorization>(
          new InvalidOperationException(OpenIddictResources
            .GetResourceString(OpenIddictResources.ID0240), exception)));
    }
  }

  public ConcurrentDictionary<int, string?> ListCursors { get; set; }
    = new ConcurrentDictionary<int, string?>();
  /*public IAsyncEnumerable<TAuthorization> ListAsync(
    int? count, int? offset, CancellationToken cancellationToken)
  {
    string? initalToken = default;
    if (offset.HasValue)
    {
      ListCursors.TryGetValue(offset.Value, out initalToken);

      if (initalToken == default)
      {
        throw new NotSupportedException("Pagination support is very limited (see documentation)");
      }
    }

    return ExecuteAsync(cancellationToken);

    async IAsyncEnumerable<TAuthorization> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
      var (token, items) = await DynamoDbUtils.Paginate<TAuthorization>(_client, count, initalToken, cancellationToken);

      if (count.HasValue)
      {
        ListCursors.TryAdd(count.Value + (offset ?? 0), token);
      }

      foreach (var item in items)
      {
        yield return item;
      }
    }
  }*/

  public async IAsyncEnumerable<TAuthorization> ListAsync(
    int? count, int? offset, CancellationToken cancellationToken)
  {
    var search = _context.FromQueryAsync<TAuthorization>(new QueryOperationConfig
    {
      Select = SelectValues.AllAttributes,
      KeyExpression = new Expression
      {
        ExpressionStatement = $"{nameof(OpenIddictDynamoDbAuthorization.PartitionKey)} = :PK and begins_with({nameof(OpenIddictDynamoDbAuthorization.SortKey)}, :prefix)",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        {":PK", OpenIddictDynamoDbAuthorization.Authorization_PartitionKey },
                        { ":prefix", OpenIddictDynamoDbAuthorization.Authorization_SortKeyPrefix},
                   }
      },
    });

    int actualCount = 0;
    int itemIndex = 0;

    bool StopWhileLoop = false;
    do
    {
      var getNextBatch = search.GetNextSetAsync(cancellationToken);
      var docList = await getNextBatch;
      foreach (var item in docList)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          StopWhileLoop = true;
          break;
        }

        if (offset.HasValue)
        {
          if (itemIndex < offset)
          {
            itemIndex++;

            continue;
          }
          else
          {
            actualCount++;
            itemIndex++;

            yield return item;
          }
        }
        else
        {
          actualCount++;
          itemIndex++;

          yield return item;
        }

        // break the loop if we must, when we have already returned enough!
        if (count.HasValue && actualCount >= count.Value)
        {
          StopWhileLoop = true;
          break;
        }
      }
    } while (!search.IsDone && !StopWhileLoop);
  }


  public async IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
    Func<IQueryable<TAuthorization>, TState, IQueryable<TResult>> query,
    TState state,
    CancellationToken cancellationToken)
  {
    if (query is null)
    {
      throw new ArgumentNullException(nameof(query));
    }



    var AllItems = await ListAllItemsAsync(cancellationToken);
    var AllAsQ = AllItems.AsQueryable();
    var FilteredItems = query(AllAsQ, state);
    foreach (var item in FilteredItems)
    {
      yield return item;
    }
  }

  // Should not be needed to run, TTL should handle the pruning

  public async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
  {
    var deleteCount = 0;
    // Get all authorizations which is older than threshold

    var search = _context.FromQueryAsync<TAuthorization>(
    new QueryOperationConfig
    {
      Select = SelectValues.AllAttributes,
      KeyExpression = new Amazon.DynamoDBv2.DocumentModel.Expression
      {
        ExpressionStatement = $"{nameof(OpenIddictDynamoDbAuthorization.PartitionKey)} = :PK and begins_with({nameof(OpenIddictDynamoDbAuthorization.SortKey)}, :prefix)",
        ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry> {
                        {":PK", OpenIddictDynamoDbAuthorization.Authorization_PartitionKey },
                        { ":prefix", OpenIddictDynamoDbAuthorization.Authorization_SortKeyPrefix},
                  }
      },
      FilterExpression = new Expression
      {
        ExpressionStatement = "CreationDate < :CreationDate",
        ExpressionAttributeValues = new()
      {
          { ":CreationDate", new DynamoDBDateTimeOffset().ToEntry( threshold) },
      }
      }

    });

    var authorizations = new List<TAuthorization>();
    do
    {
      var getNextBatch = search.GetNextSetAsync(cancellationToken);
      var docList = await getNextBatch;
      authorizations.AddRange(docList);


    } while (!search.IsDone);


    var remainingAdHocAuthorizations = new List<TAuthorization>();

    var batchDelete = _context.CreateBatchWrite<TAuthorization>();

    foreach (var authorization in authorizations)
    {
      // Add authorizations which is not Valid
      if (authorization.Status != Statuses.Valid)
      {
        batchDelete.AddDeleteItem(authorization);
        deleteCount++;
      }
      else if (authorization.Type == AuthorizationTypes.AdHoc)
      {
        remainingAdHocAuthorizations.Add(authorization);
      }
    }

    // Add authorizations which is ad hoc and has no tokens
    foreach (var authorization in remainingAdHocAuthorizations)
    {
      var tokensQuery = _context.FromQueryAsync<OpenIddictDynamoDbToken>(new()
      {
        IndexName = "AuthorizationId-index",
        KeyExpression = new()
        {
          ExpressionStatement = "AuthorizationId = :authorizationId",
          ExpressionAttributeValues = new()
          {
            { ":authorizationId", authorization.Id },
          }
        },
      });


      var tokens = new List<OpenIddictDynamoDbToken>();
      do
      {
        var getNextBatch = tokensQuery.GetNextSetAsync(cancellationToken);
        var docList = await getNextBatch;
        tokens.AddRange(docList);


      } while (!search.IsDone);


      if (tokens.Any() == false)
      {
        batchDelete.AddDeleteItem(authorization);
        deleteCount++;
      }
    }

    await batchDelete.ExecuteAsync(cancellationToken);

    var count = await CountAsync(cancellationToken);
    await _context.SaveAsync(new CountModel(CountType.Authorization, count - deleteCount), cancellationToken);
    return count - deleteCount;
  }


  public ValueTask SetApplicationIdAsync(TAuthorization authorization, string? identifier, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.ApplicationId = identifier;

    return default;
  }

  public ValueTask SetCreationDateAsync(
    TAuthorization authorization, DateTimeOffset? date, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.CreationDate = date?.UtcDateTime;

    return default;
  }

  public ValueTask SetPropertiesAsync(
    TAuthorization authorization,
    ImmutableDictionary<string, JsonElement> properties,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (properties is not { Count: > 0 })
    {
      authorization.Properties = null;

      return default;
    }

    using var stream = new MemoryStream();
    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
    {
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
      Indented = false
    });

    writer.WriteStartObject();

    foreach (var property in properties)
    {
      writer.WritePropertyName(property.Key);
      property.Value.WriteTo(writer);
    }

    writer.WriteEndObject();
    writer.Flush();

    authorization.Properties = Encoding.UTF8.GetString(stream.ToArray());

    return default;
  }

  public ValueTask SetScopesAsync(
    TAuthorization authorization, ImmutableArray<string> scopes, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    if (scopes.IsDefaultOrEmpty)
    {
      authorization.Scopes = null;

      return default;
    }

    authorization.Scopes = scopes.ToList();

    return default;
  }

  public ValueTask SetStatusAsync(
    TAuthorization authorization, string? status, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.Status = status;

    return default;
  }

  public ValueTask SetSubjectAsync(
    TAuthorization authorization, string? subject, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.Subject = subject;

    return default;
  }

  public ValueTask SetTypeAsync(
    TAuthorization authorization, string? type, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    authorization.Type = type;

    return default;
  }

  public async ValueTask UpdateAsync(
    TAuthorization authorization, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(authorization);

    // Ensure no one else is updating
    var databaseApplication = await GetBySortKey(authorization, cancellationToken);
    if (databaseApplication == default || databaseApplication.ConcurrencyToken != authorization.ConcurrencyToken)
    {
      throw new ArgumentException("Given authorization is invalid", nameof(authorization));
    }

    authorization.ConcurrencyToken = Guid.NewGuid().ToString();

    if (authorization.Status != Statuses.Valid)
    {
      authorization.TTL = DateTimeOffset.Now.AddMinutes(5);
    }

    await _context.SaveAsync(authorization, cancellationToken);
  }


  private async Task<TAuthorization?> GetBySortKey(TAuthorization authorization, CancellationToken cancellationToken)
  {
    var search = _context.FromQueryAsync<TAuthorization>(new()
    {
      KeyExpression = new()
      {
        ExpressionStatement = $"{nameof(OpenIddictDynamoDbAuthorization.PartitionKey)} = :PK and {nameof(OpenIddictDynamoDbAuthorization.SortKey)} = :sk",

        ExpressionAttributeValues = new()
        {
          { ":sk", $"{OpenIddictDynamoDbAuthorization.Authorization_SortKeyPrefix}{authorization.Id}" },
          { ":PK", OpenIddictDynamoDbAuthorization.Authorization_PartitionKey },
        }
      }


    });
    var result = await search.GetNextSetAsync(cancellationToken);

    return result.Any() ? result.First() : default;
  }

  private async Task<TAuthorization?> GetByPartitionKeyOLD(TAuthorization token, CancellationToken cancellationToken)
  {
    var search = _context.FromQueryAsync<TAuthorization>(new()
    {
      KeyExpression = new()
      {
        ExpressionStatement = "PartitionKey = :partitionKey",
        ExpressionAttributeValues = new()
        {
          { ":partitionKey", token.PartitionKey },
        }
      },
      Limit = 1,
    });
    var result = await search.GetNextSetAsync(cancellationToken);

    return result.Any() ? result.First() : default;
  }

}
