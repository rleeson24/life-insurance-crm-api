var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sql.AddDatabase("LifeInsuranceCRM");

builder.AddProject<Projects.LifeInsuranceCRM_API>("lifeinsurancecrm-api")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
