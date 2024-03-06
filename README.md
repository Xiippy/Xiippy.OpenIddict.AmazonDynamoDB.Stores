# Xiippy.OpenIddict.AmazonDynamoDB.Stores


Xiippy's port of https://github.com/ganhammar/OpenIddict.AmazonDynamoDB/ to OpenIDDict 5.1 with new interface implementations

Critical: Data schema for tokens and authorizations have been modified to improve performance and reduce costs by avoiding the use of the DynamoDB SCAN operation. Feel free to use for fresh starts but if you have a running system, you may need to migrate data to make everything work as before smoothly after upgrading to this version! 

![Build Status](https://github.com/ganhammar/OpenIddict.AmazonDynamoDB/actions/workflows/ci-cd.yml/badge.svg) [![codecov](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB/branch/main/graph/badge.svg?token=S4M1VCX8J6)](https://codecov.io/gh/ganhammar/OpenIddict.AmazonDynamoDB) [![NuGet](https://img.shields.io/nuget/v/Community.OpenIddict.AmazonDynamoDB)](https://www.nuget.org/packages/Community.OpenIddict.AmazonDynamoDB)

A [DynamoDB](https://aws.amazon.com/dynamodb/) integration for [OpenIddict](https://github.com/openiddict/openiddict-core).

## Getting Started

You can install the latest version via [Nuget](https://www.nuget.org/packages/OpenIddict.AmazonDynamoDB):

```
> dotnet add package Community.OpenIddict.AmazonDynamoDB
```

Then you use the stores by calling `AddDynamoDbStores` on `OpenIddictBuilder`:

```c#
services
    .AddOpenIddict()
    .AddCore()
    .UseDynamoDb()
    .Configure(options =>
    {
        options.BillingMode = BillingMode.PROVISIONED; // Default is BillingMode.PAY_PER_REQUEST
        options.ProvisionedThroughput = new ProvisionedThroughput
        {
            ReadCapacityUnits = 5, // Default is 1
            WriteCapacityUnits = 5, // Default is 1
        };
        options.UsersTableName = "CustomOpenIddictTable"; // Default is openiddict
    });
```

Finally, you need to ensure that tables and indexes have been added:

```c#
OpenIddictDynamoDbSetup.EnsureInitialized(serviceProvider);
```

Or asynchronously:

```c#
await OpenIddictDynamoDbSetup.EnsureInitializedAsync(serviceProvider);
```


## Tests

In order to run the tests, you need to have DynamoDB running locally on `localhost:8000`. This can easily be done using [Docker](https://www.docker.com/) and the following command:

```
docker run -p 8000:8000 amazon/dynamodb-local
```
