var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sql.AddDatabase("LifeInsuranceCRM");

var api = builder.AddProject<Projects.LifeInsuranceCRM_API>("lifeinsurancecrm-api")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.AddViteApp("lifeinsurancecrm-client", "../../../../life-insurance-crm-client/src")
    .WithReference(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"))
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
