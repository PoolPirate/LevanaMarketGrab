using ContractSchemas.Market;
using ContractSchemas.Factory;
using Cosm.Net.Client;
using Cosm.Net.Extensions;
using Grpc.Net.Client;
using LevanaMarketGrab;
using System.Diagnostics;
using System.Text.Json;

var channel = GrpcChannel.ForAddress("http://osmosis-grpc.polkachu.com:12590");
var client = new CosmClientBuilder()
    .WithChannel(channel)
    .InstallOsmosis()
    .AddWasmd(wasm => wasm
        .RegisterContractSchema<ILevanaFactory>()
        .RegisterContractSchema<ILevanaMarket>())
    .BuildReadClient();

var factory = client.Contract<ILevanaFactory>("osmo1ssw6x553kzqher0earlkwlxasfm2stnl3ms3ma2zz4tnajxyyaaqlucd45");
var marketNames = await factory.MarketsAsync(limit: 1000);

var markets = new List<Market>();

foreach(var marketName in marketNames.Markets)
{
    await Task.Delay(200); //Avoid 429's

    var marketInfo = await factory.MarketInfoAsync(marketName);
    var market = client.Contract<ILevanaMarket>(marketInfo.MarketAddr);

    var status = await market.StatusAsync();

    Console.WriteLine($"Market: {marketName} - {market.ContractAddress}");

    markets.Add(new Market(marketName, market.ContractAddress, status.Config));
}

var filePath = Path.Combine(Environment.CurrentDirectory, "markets.json");

Console.WriteLine($"Writing Output to {filePath}");
await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(markets));
