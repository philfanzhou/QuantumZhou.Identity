using ServiceMantle;

namespace SignaCore.Host;

/// <summary>
/// The minimal ServiceMantle composition shared by the Bootstrap, Setup, and normal hosts.
/// </summary>
/// <remarks>
/// Only the host identity is registered: the Correlation ID middleware is the single ServiceMantle
/// capability activated in this phase, so <see cref="AddSignaCoreServiceMantle"/> must be followed
/// by <c>UseServiceMantleCorrelationId</c> on the pipeline. The default ServiceMantle Bootstrap
/// store stays lazily registered and is never resolved here: BootstrapLoader, the installation
/// state, the business database, authentication, and Serilog remain owned by SignaCore. The
/// ServiceMantle request log scope adds its own ServiceName, ServiceVersion, and InstanceId fields;
/// the existing global Serilog enrichment is intentionally left unchanged.
/// </remarks>
internal static class ServiceMantleComposition
{
    internal const string ServiceIdentifier = "signacore";

    /// <summary>
    /// Registers the ServiceMantle host identity. The instance id is generated once per host build
    /// (<c>signacore-</c> plus a GUID in N format) and is used for request log scope fields only;
    /// it is not a persistent identity. The service version is left unset so ServiceMantle resolves
    /// the entry assembly version.
    /// </summary>
    internal static void AddSignaCoreServiceMantle(this IServiceCollection services) =>
        services.AddServiceMantle(
            ServiceId.Parse(ServiceIdentifier),
            InstanceId.Parse($"{ServiceIdentifier}-{Guid.NewGuid():N}"));
}
