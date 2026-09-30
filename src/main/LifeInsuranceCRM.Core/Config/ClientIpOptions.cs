namespace LifeInsuranceCRM.Core.Config;

public sealed class ClientIpOptions
{
    public const string SectionName = "ClientIp";

    /// <summary>
    /// When true, the last address in X-Forwarded-For is the client observed by the platform proxy.
    /// Leave false unless the app is reachable only through that proxy. A caller can otherwise spoof the header.
    /// </summary>
    public bool TrustForwardedFor { get; set; }
}
