using Bit.Core.AdminConsole.OrganizationAuth.Models;
using Bit.Core.Auth.Entities;
using Bit.Core.Auth.Models.Data;
using Bit.Core.Enums;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.AdminConsole.OrganizationAuth.Models;

[SutProviderCustomize]
public class BatchAuthRequestUpdateProcessorTests
{
    [Theory]
    [BitAutoData]
    public void Process_NoProcessors_Handled(
        IEnumerable<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Action<Exception> errorHandler
    )
    {
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(null, updates, configuration);
        sut.Process(errorHandler);
    }

    [Theory]
    [BitAutoData]
    public void Process_BadInput_CallsHandler(
        List<OrganizationAdminAuthRequest> authRequests,
        IEnumerable<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Action<Exception> errorHandler
    )
    {
        // An already approved auth request should break the processor
        // immediately.
        authRequests[0].Approved = true;
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        sut.Process(errorHandler);
        errorHandler.ReceivedWithAnyArgs()(new AuthRequestUpdateProcessingException());
    }

    [Theory]
    [BitAutoData]
    public void Process_ValidInput_Works(
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Action<Exception> errorHandler
    )
    {
        (authRequests[0], updates[0], configuration) = UnrespondAndEnsureValid(authRequests[0], updates[0], configuration);
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        sut.Process(errorHandler);
        Assert.NotNull(sut.Processors.FirstOrDefault().ProcessedAuthRequest);
    }

    [Theory]
    [BitAutoData]
    public async Task Save_NoProcessedAuthRequests_IsHandled(
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<IEnumerable<AuthRequest>, Task> saveCallback
    )
    {
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.Null(sut.Processors);
        await sut.Save(saveCallback);
    }

    [Theory]
    [BitAutoData]
    public async Task Save_ProcessedAuthRequests_IsHandled(
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<IEnumerable<AuthRequest>, Task> saveCallback,
        Action<Exception> errorHandler
    )
    {
        (authRequests[0], updates[0], configuration) = UnrespondAndEnsureValid(authRequests[0], updates[0], configuration);
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.NotNull(sut.Processors);
        await sut.Process(errorHandler).Save(saveCallback);
        await saveCallback.ReceivedWithAnyArgs()(sut.Processors.Select(p => p.ProcessedAuthRequest));
    }

    [Theory]
    [BitAutoData]
    public async Task SendPushNotifications_NoProcessors_IsHandled
    (
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<AuthRequest, Task> callback
    )
    {
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.Null(sut.Processors);
        await sut.SendPushNotifications(callback);
    }

    [Theory]
    [BitAutoData]
    public async Task SendPushNotifications_HasProcessors_Sends
    (
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<AuthRequest, Task> callback,
        Action<Exception> errorHandler
    )
    {
        (authRequests[0], updates[0], configuration) = UnrespondAndEnsureValid(authRequests[0], updates[0], configuration);
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.Null(sut.Processors);
        await sut.Process(errorHandler).SendPushNotifications(callback);
        await sut.Processors.FirstOrDefault().Received().SendPushNotification(callback);
    }

    [Theory]
    [BitAutoData]
    public async Task SendNewDeviceEmails_NoProcessors_IsHandled
    (
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<AuthRequest, string, Task> callback
    )
    {
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.Null(sut.Processors);
        await sut.SendNewDeviceEmails(callback);
    }

    [Theory]
    [BitAutoData]
    public async Task SendNewDeviceEmails_HasProcessors_Sends
    (
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<AuthRequest, string, Task> callback,
        Action<Exception> errorHandler
    )
    {
        (authRequests[0], updates[0], configuration) = UnrespondAndEnsureValid(authRequests[0], updates[0], configuration);
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.Null(sut.Processors);
        await sut.Process(errorHandler).SendNewDeviceEmails(callback);
        await sut.Processors.FirstOrDefault().Received().SendNewDeviceEmail(callback);
    }

    [Theory]
    [BitAutoData]
    public async Task SendEventLogs_NoProcessors_IsHandled
    (
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<AuthRequest, EventType, Task> callback
    )
    {
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.Null(sut.Processors);
        await sut.SendEventLogs(callback);
    }

    [Theory]
    [BitAutoData]
    public async Task SendEventLogs_HasProcessors_Sends
    (
        List<OrganizationAdminAuthRequest> authRequests,
        List<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration,
        Func<AuthRequest, EventType, Task> callback,
        Action<Exception> errorHandler
    )
    {
        (authRequests[0], updates[0], configuration) = UnrespondAndEnsureValid(authRequests[0], updates[0], configuration);
        var sut = new BatchAuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequests, updates, configuration);
        Assert.NotNull(sut.Processors);
        await sut.Process(errorHandler).SendEventLogs(callback);
        await sut.Processors.FirstOrDefault().Received().SendEventLog(callback);
    }

    private (
        T authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration ProcessorConfiguration
    ) UnrespondAndEnsureValid<T>(
        T authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    ) where T : AuthRequest
    {
        authRequest.Id = update.Id;
        authRequest.OrganizationId = processorConfiguration.OrganizationId;
        authRequest.Key = null;
        authRequest.Approved = null;
        authRequest.ResponseDate = null;
        authRequest.AuthenticationDate = null;
        authRequest.CreationDate = DateTime.UtcNow;
        processorConfiguration.AuthRequestExipredAfter = DateTime.UtcNow.AddDays(1) - DateTime.UtcNow;

        update.Approved = true;
        update.Key = "key";
        return (authRequest, update, processorConfiguration);
    }
}
