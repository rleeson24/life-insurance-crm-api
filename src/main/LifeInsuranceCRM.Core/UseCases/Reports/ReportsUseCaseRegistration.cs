using Microsoft.Extensions.DependencyInjection;

namespace LifeInsuranceCRM.Core.UseCases.Reports;

public static class ReportsUseCaseRegistration
{
    public static IServiceCollection AddReportsUseCases(this IServiceCollection services)
    {
        services.AddScoped<IReportUseCaseHelpers, ReportUseCaseHelpers>();
        services.AddScoped<IGetBookOfBusinessReportUseCase, GetBookOfBusinessReportUseCase>();
        services.AddScoped<IGetMailingListReportUseCase, GetMailingListReportUseCase>();
        services.AddScoped<IGetProductionReportUseCase, GetProductionReportUseCase>();
        services.AddScoped<IGetRetentionReportUseCase, GetRetentionReportUseCase>();
        services.AddScoped<IExportBookOfBusinessReportUseCase, ExportBookOfBusinessReportUseCase>();
        services.AddScoped<IExportMailingListReportUseCase, ExportMailingListReportUseCase>();
        services.AddScoped<IExportProductionReportUseCase, ExportProductionReportUseCase>();
        services.AddScoped<IExportRetentionReportUseCase, ExportRetentionReportUseCase>();
        return services;
    }
}
