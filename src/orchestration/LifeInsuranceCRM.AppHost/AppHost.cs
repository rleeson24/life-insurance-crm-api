using LifeInsuranceCRM.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

const string databaseName = "BrokerBook";

var sql = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = sql.AddDatabase("BrokerBook", databaseName)
    .WithCreationScript(LiveSchemaScripts.BuildCreationScript(databaseName));

var api = builder.AddProject<Projects.LifeInsuranceCRM_API>("lifeinsurancecrm-api")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.AddViteApp("lifeinsurancecrm-client", "../../../../life-insurance-crm-client/src")
    .WithHttpEndpoint(port: 5387, env: "PORT")
    .WithReference(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("https"))
    .WaitFor(api);
builder.Build().Run();
