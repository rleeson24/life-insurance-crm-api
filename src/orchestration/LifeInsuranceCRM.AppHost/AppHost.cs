var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.LifeInsuranceCRM_API>("lifeinsurancecrm-api");

builder.Build().Run();
