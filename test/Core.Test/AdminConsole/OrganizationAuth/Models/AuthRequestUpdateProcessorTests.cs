﻿using Bit.Core.AdminConsole.OrganizationAuth.Models;
using Bit.Core.Auth.Entities;
using Bit.Core.Auth.Models.Data;
using Bit.Core.Enums;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.AdminConsole.OrganizationAuth.Models;

[SutProviderCustomize]
public class AuthRequestUpdateProcessorTests
{
    [Theory]
    [BitAutoData]
    public void Process_RequestIsAlreadyApproved_Throws(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        authRequest = Approve(authRequest);
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        Assert.ThrowsAny<AuthRequestUpdateCouldNotBeProcessedException>(() => sut.Process());
    }

    [Theory]
    [BitAutoData]
    public void Process_RequestIsAlreadyDenied_Throws(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        authRequest = Deny(authRequest);
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        Assert.ThrowsAny<AuthRequestUpdateCouldNotBeProcessedException>(() => sut.Process());
    }

    [Theory]
    [BitAutoData(true)]
    public void Process_RequestIsExpired_Throws(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        processorConfiguration.AuthRequestExipredAfter = TimeSpan.MinValue;
        authRequest.CreationDate = DateTime.MinValue;
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        Assert.ThrowsAny<AuthRequestUpdateCouldNotBeProcessedException>(() => sut.Process());
    }

    [Theory]
    [BitAutoData]
    public void Process_UpdateDoesNotMatch_Throws(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        while (authRequest.Id == update.Id)
        {
            authRequest.Id = new Guid();
        }
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        Assert.ThrowsAny<AuthRequestUpdateCouldNotBeProcessedException>(() => sut.Process());
    }

    [Theory]
    [BitAutoData]
    public void Process_AuthRequestAndOrganizationIdMismatch_Throws(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        while (authRequest.OrganizationId == processorConfiguration.OrganizationId)
        {
            authRequest.OrganizationId = new Guid();
        }
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        Assert.ThrowsAny<AuthRequestUpdateCouldNotBeProcessedException>(() => sut.Process());
    }

    [Theory]
    [BitAutoData]
    public void Process_RequestApproved_NoKey_Throws(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = true;
        update.Key = null;
        var isExpired = DateTime.UtcNow <
            authRequest.CreationDate
            .Add(processorConfiguration.AuthRequestExipredAfter);
        var isSpent = authRequest == null ||
            authRequest.Approved != null ||
            authRequest.ResponseDate.HasValue ||
            authRequest.AuthenticationDate.HasValue;
        var updatesMatch = authRequest.Id == update.Id;
        var orgsMatch = authRequest.OrganizationId == processorConfiguration.OrganizationId;

        Assert.False(isExpired);
        Assert.False(isSpent);
        Assert.True(updatesMatch);
        Assert.True(orgsMatch);
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        Assert.ThrowsAny<ApprovedAuthRequestIsMissingKeyException>(() => sut.Process());
    }

    [Theory]
    [BitAutoData]
    public void Process_RequestApproved_ValidInput_Works(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = true;
        update.Key = "key";
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        sut.Process();
    }

    [Theory]
    [BitAutoData]
    public void Process_RequestDenied_ValidInput_Works(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = false;
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        sut.Process();
    }

    [Theory]
    [BitAutoData]
    public async Task SendPushNotification_RequestIsDenied_DoesNotSend(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration,
        Func<AuthRequest, Task> callback
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = false;
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        await sut.Process().SendPushNotification(callback);
        await callback.DidNotReceiveWithAnyArgs()(sut.ProcessedAuthRequest);
    }

    [Theory]
    [BitAutoData]
    public async Task SendPushNotification_RequestIsApproved_DoesSend(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration,
        Func<AuthRequest, Task> callback
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = true;
        update.Key = "key";
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        await sut.Process().SendPushNotification(callback);
        await callback.Received()(sut.ProcessedAuthRequest);
    }

    [Theory]
    [BitAutoData]
    public async Task SendNewDeviceEmail_RequestIsDenied_DoesNotSend(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration,
        Func<AuthRequest, string, Task> callback
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = false;
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        await sut.Process().SendNewDeviceEmail(callback);
        await callback.DidNotReceiveWithAnyArgs()(sut.ProcessedAuthRequest, "string");
    }

    [Theory]
    [BitAutoData]
    public async Task SendNewDeviceEmail_RequestIsApproved_DoesSend(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration,
        Func<AuthRequest, string, Task> callback
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        authRequest.RequestDeviceType = DeviceType.iOS;
        authRequest.RequestDeviceIdentifier = "device-id";
        update.Approved = true;
        update.Key = "key";
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        await sut.Process().SendNewDeviceEmail(callback);
        await callback.Received()(sut.ProcessedAuthRequest, "iOS - device-id");
    }

    [Theory]
    [BitAutoData]
    public async Task SendEventLog_RequestIsApproved_Sends(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration,
        Func<AuthRequest, EventType, Task> callback
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = true;
        update.Key = "key";
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        await sut.Process().SendEventLog(callback);
        await callback.Received()(sut.ProcessedAuthRequest, EventType.OrganizationUser_ApprovedAuthRequest);
    }

    [Theory]
    [BitAutoData]
    public async Task SendEventLog_RequestIsDenied_Sends(
        OrganizationAdminAuthRequest authRequest,
        OrganizationAuthRequestUpdate update,
        AuthRequestUpdateProcessorConfiguration processorConfiguration,
        Func<AuthRequest, EventType, Task> callback
    )
    {
        (authRequest, processorConfiguration) = UnrespondAndEnsureValid(authRequest, update, processorConfiguration);
        update.Approved = false;
        var sut = new AuthRequestUpdateProcessor<OrganizationAdminAuthRequest>(authRequest, update, processorConfiguration);
        await sut.Process().SendEventLog(callback);
        await callback.Received()(sut.ProcessedAuthRequest, EventType.OrganizationUser_RejectedAuthRequest);
    }

    private static T Approve<T>(T authRequest) where T : AuthRequest
    {
        authRequest.Key = "key";
        authRequest.Approved = true;
        authRequest.ResponseDate = DateTime.UtcNow;
        return authRequest;
    }

    private static T Deny<T>(T authRequest) where T : AuthRequest
    {
        authRequest.Approved = false;
        authRequest.ResponseDate = DateTime.UtcNow;
        return authRequest;
    }

    private (
        T AuthRequest,
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
        processorConfiguration.AuthRequestExipredAfter = DateTime.UtcNow.AddDays(1) - DateTime.UtcNow.AddDays(-1);
        return (authRequest, processorConfiguration);
    }
}
