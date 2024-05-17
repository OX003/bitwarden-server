using Bit.Api.Billing.Models.Responses;
using Bit.Core.Billing.Queries;
using Bit.Core.Context;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Billing.Controllers;

[Route("organizations/{organizationId:guid}/billing")]
[Authorize("Application")]
public class OrganizationBillingController(
    IOrganizationBillingQueries organizationBillingQueries,
    ICurrentContext currentContext,
    IOrganizationRepository organizationRepository,
    IPaymentService paymentService) : Controller
{
    [HttpGet("metadata")]
    public async Task<IResult> GetMetadataAsync([FromRoute] Guid organizationId)
    {
        var metadata = await organizationBillingQueries.GetMetadata(organizationId);

        if (metadata == null)
        {
            return TypedResults.NotFound();
        }

        var response = OrganizationMetadataResponse.From(metadata);

        return TypedResults.Ok(response);
    }

    [HttpGet("history")]
    public async Task<IResult> GetHistoryAsync([FromRoute] Guid organizationId)
    {
        var organization = await organizationRepository.GetByIdAsync(organizationId);

        if (organization == null)
        {
            return TypedResults.NotFound();
        }

        var billingInfo = await paymentService.GetBillingHistoryAsync(organization);

        return TypedResults.Ok(billingInfo);
    }

    [HttpGet]
    [SelfHosted(NotSelfHostedOnly = true)]
    public async Task<IResult> GetBillingAsync(Guid organizationId)
    {
        if (!await currentContext.ViewBillingHistory(organizationId))
        {
            return TypedResults.NotFound();
        }

        var organization = await organizationRepository.GetByIdAsync(organizationId);
        if (organization == null)
        {
            return TypedResults.NotFound();
        }

        var billingInfo = await paymentService.GetBillingAsync(organization);
        return TypedResults.Ok(billingInfo);
    }
}
