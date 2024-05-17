namespace Bit.Core.AdminConsole.OrganizationAuth.Models;

public class AuthRequestUpdateProcessorConfiguration
{
    public Guid OrganizationId { get; set; }
    public TimeSpan AuthRequestExipredAfter { get; set; }
}

