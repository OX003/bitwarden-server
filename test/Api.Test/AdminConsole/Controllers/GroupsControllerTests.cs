﻿using System.Security.Claims;
using Bit.Api.AdminConsole.Controllers;
using Bit.Api.AdminConsole.Models.Request;
using Bit.Api.Models.Request;
using Bit.Api.Vault.AuthorizationHandlers.Collections;
using Bit.Core;
using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.OrganizationFeatures.Groups.Interfaces;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.Models.Data;
using Bit.Core.Models.Data.Organizations;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Xunit;

namespace Bit.Api.Test.AdminConsole.Controllers;

[ControllerCustomize(typeof(GroupsController))]
[SutProviderCustomize]
public class GroupsControllerTests
{
    [Theory]
    [BitAutoData]
    public async Task Post_PreFCv1_Success(Organization organization, GroupRequestModel groupRequestModel, SutProvider<GroupsController> sutProvider)
    {
        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
        sutProvider.GetDependency<ICurrentContext>().ManageGroups(organization.Id).Returns(true);

        var response = await sutProvider.Sut.Post(organization.Id, groupRequestModel);

        await sutProvider.GetDependency<ICurrentContext>().Received(1).ManageGroups(organization.Id);
        await sutProvider.GetDependency<ICreateGroupCommand>().Received(1).CreateGroupAsync(
            Arg.Is<Group>(g =>
                g.OrganizationId == organization.Id && g.Name == groupRequestModel.Name &&
                g.AccessAll == groupRequestModel.AccessAll),
            organization,
            Arg.Any<ICollection<CollectionAccessSelection>>(),
            Arg.Any<IEnumerable<Guid>>());
        Assert.Equal(groupRequestModel.Name, response.Name);
        Assert.Equal(organization.Id, response.OrganizationId);
        Assert.Equal(groupRequestModel.AccessAll, response.AccessAll);
    }

    [Theory]
    [BitAutoData]
    public async Task Post_AuthorizedToGiveAccessToCollections_Success(Organization organization,
        GroupRequestModel groupRequestModel, SutProvider<GroupsController> sutProvider)
    {
        // Enable FC and v1
        sutProvider.GetDependency<IApplicationCacheService>().GetOrganizationAbilityAsync(organization.Id).Returns(
            new OrganizationAbility { Id = organization.Id, FlexibleCollections = true});
        sutProvider.GetDependency<IFeatureService>().IsEnabled(FeatureFlagKeys.FlexibleCollectionsV1).Returns(true);

       sutProvider.GetDependency<IAuthorizationService>()
           .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(),
                Arg.Any<IEnumerable<Collection>>(),
                Arg.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.Contains(BulkCollectionOperations.ModifyAccess)))
            .Returns(AuthorizationResult.Success());

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
        sutProvider.GetDependency<ICurrentContext>().ManageGroups(organization.Id).Returns(true);

        var response = await sutProvider.Sut.Post(organization.Id, groupRequestModel);

        await sutProvider.GetDependency<ICurrentContext>().Received(1).ManageGroups(organization.Id);
        await sutProvider.GetDependency<ICreateGroupCommand>().Received(1).CreateGroupAsync(
            Arg.Is<Group>(g =>
                g.OrganizationId == organization.Id && g.Name == groupRequestModel.Name &&
                g.AccessAll == groupRequestModel.AccessAll),
            organization,
            Arg.Any<ICollection<CollectionAccessSelection>>(),
            Arg.Any<IEnumerable<Guid>>());
        Assert.Equal(groupRequestModel.Name, response.Name);
        Assert.Equal(organization.Id, response.OrganizationId);
        Assert.Equal(groupRequestModel.AccessAll, response.AccessAll);
    }

    [Theory]
    [BitAutoData]
    public async Task Post_NotAuthorizedToGiveAccessToCollections_Throws(Organization organization, GroupRequestModel groupRequestModel, SutProvider<GroupsController> sutProvider)
    {
        // Enable FC and v1
        sutProvider.GetDependency<IApplicationCacheService>().GetOrganizationAbilityAsync(organization.Id).Returns(
            new OrganizationAbility { Id = organization.Id, FlexibleCollections = true});
        sutProvider.GetDependency<IFeatureService>().IsEnabled(FeatureFlagKeys.FlexibleCollectionsV1).Returns(true);

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
        sutProvider.GetDependency<ICurrentContext>().ManageGroups(organization.Id).Returns(true);

        sutProvider.GetDependency<IAuthorizationService>()
           .AuthorizeAsync(Arg.Any<ClaimsPrincipal>(),
                Arg.Any<IEnumerable<Collection>>(),
                Arg.Is<IEnumerable<IAuthorizationRequirement>>(reqs => reqs.Contains(BulkCollectionOperations.ModifyAccess)))
            .Returns(AuthorizationResult.Failed());

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => sutProvider.Sut.Post(organization.Id, groupRequestModel));

        Assert.Contains("You are not authorized to grant access to these collections.", exception.Message);

        await sutProvider.GetDependency<ICreateGroupCommand>().DidNotReceiveWithAnyArgs()
            .CreateGroupAsync(default, default, default, default);
    }

    [Theory]
    [BitAutoData]
    public async Task Put_AdminsCanAccessAllCollections_Success(Organization organization, Group group, GroupRequestModel groupRequestModel, SutProvider<GroupsController> sutProvider)
    {
        group.OrganizationId = organization.Id;

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
        sutProvider.GetDependency<IGroupRepository>().GetByIdWithCollectionsAsync(group.Id)
            .Returns(new Tuple<Group, ICollection<CollectionAccessSelection>>(group, new List<CollectionAccessSelection>()));
        sutProvider.GetDependency<ICurrentContext>().ManageGroups(organization.Id).Returns(true);
        sutProvider.GetDependency<IApplicationCacheService>().GetOrganizationAbilityAsync(organization.Id).Returns(
            new OrganizationAbility { Id = organization.Id, AllowAdminAccessToAllCollectionItems = true, FlexibleCollections = true});
        sutProvider.GetDependency<IFeatureService>().IsEnabled(FeatureFlagKeys.FlexibleCollectionsV1).Returns(true);

        var response = await sutProvider.Sut.Put(organization.Id, group.Id, groupRequestModel);

        await sutProvider.GetDependency<ICurrentContext>().Received(1).ManageGroups(organization.Id);
        await sutProvider.GetDependency<IUpdateGroupCommand>().Received(1).UpdateGroupAsync(
            Arg.Is<Group>(g =>
                g.OrganizationId == organization.Id && g.Name == groupRequestModel.Name &&
                g.AccessAll == groupRequestModel.AccessAll),
            Arg.Is<Organization>(o => o.Id == organization.Id),
            Arg.Any<ICollection<CollectionAccessSelection>>(),
            Arg.Any<IEnumerable<Guid>>());
        Assert.Equal(groupRequestModel.Name, response.Name);
        Assert.Equal(organization.Id, response.OrganizationId);
        Assert.Equal(groupRequestModel.AccessAll, response.AccessAll);
    }

    [Theory]
    [BitAutoData]
    public async Task Put_AdminsCannotAccessAllCollections_CannotAddSelfToGroup(Organization organization, Group group,
        GroupRequestModel groupRequestModel, OrganizationUser savingOrganizationUser, List<Guid> currentGroupUsers,
        SutProvider<GroupsController> sutProvider)
    {
        group.OrganizationId = organization.Id;

        // Saving user is trying to add themselves to the group
        var updatedUsers = groupRequestModel.Users.ToList();
        updatedUsers.Add(savingOrganizationUser.Id);
        groupRequestModel.Users = updatedUsers;

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
        sutProvider.GetDependency<IGroupRepository>().GetByIdWithCollectionsAsync(group.Id)
            .Returns(new Tuple<Group, ICollection<CollectionAccessSelection>>(group, new List<CollectionAccessSelection>()));
        sutProvider.GetDependency<ICurrentContext>().ManageGroups(organization.Id).Returns(true);
        sutProvider.GetDependency<IApplicationCacheService>().GetOrganizationAbilityAsync(organization.Id).Returns(
            new OrganizationAbility
            {
                Id = organization.Id,
                AllowAdminAccessToAllCollectionItems = false,
                FlexibleCollections = true
            });
        sutProvider.GetDependency<IFeatureService>().IsEnabled(FeatureFlagKeys.FlexibleCollectionsV1).Returns(true);
        sutProvider.GetDependency<IOrganizationUserRepository>()
            .GetByOrganizationAsync(organization.Id, Arg.Any<Guid>())
                .Returns(savingOrganizationUser);
        sutProvider.GetDependency<IUserService>().GetProperUserId(Arg.Any<ClaimsPrincipal>())
            .Returns(savingOrganizationUser.UserId);
        sutProvider.GetDependency<IGroupRepository>().GetManyUserIdsByIdAsync(group.Id)
            .Returns(currentGroupUsers);

        var exception = await
            Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.Put(organization.Id, group.Id, groupRequestModel));

        Assert.Contains("You cannot add yourself to groups", exception.Message);
    }

    [Theory]
    [BitAutoData]
    public async Task Put_AdminsCannotAccessAllCollections_AlreadyInGroup_Success(Organization organization, Group group,
        GroupRequestModel groupRequestModel, OrganizationUser savingOrganizationUser, List<Guid> currentGroupUsers,
        SutProvider<GroupsController> sutProvider)
    {
        group.OrganizationId = organization.Id;

        // Saving user is trying to add themselves to the group
        var updatedUsers = groupRequestModel.Users.ToList();
        updatedUsers.Add(savingOrganizationUser.Id);
        groupRequestModel.Users = updatedUsers;

        // But! they are already a member of the group
        currentGroupUsers.Add(savingOrganizationUser.Id);

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(organization.Id).Returns(organization);
        sutProvider.GetDependency<IGroupRepository>().GetByIdWithCollectionsAsync(group.Id)
            .Returns(new Tuple<Group, ICollection<CollectionAccessSelection>>(group, new List<CollectionAccessSelection>()));
        sutProvider.GetDependency<ICurrentContext>().ManageGroups(organization.Id).Returns(true);
        sutProvider.GetDependency<IApplicationCacheService>().GetOrganizationAbilityAsync(organization.Id).Returns(
            new OrganizationAbility
            {
                Id = organization.Id,
                AllowAdminAccessToAllCollectionItems = false,
                FlexibleCollections = true
            });
        sutProvider.GetDependency<IFeatureService>().IsEnabled(FeatureFlagKeys.FlexibleCollectionsV1).Returns(true);
        sutProvider.GetDependency<IOrganizationUserRepository>()
            .GetByOrganizationAsync(organization.Id, Arg.Any<Guid>())
                .Returns(savingOrganizationUser);
        sutProvider.GetDependency<IUserService>().GetProperUserId(Arg.Any<ClaimsPrincipal>())
            .Returns(savingOrganizationUser.UserId);
        sutProvider.GetDependency<IGroupRepository>().GetManyUserIdsByIdAsync(group.Id)
            .Returns(currentGroupUsers);

        // Make collection authorization pass, it's not being tested here
        groupRequestModel.Collections = Array.Empty<SelectionReadOnlyRequestModel>();

        var response = await sutProvider.Sut.Put(organization.Id, group.Id, groupRequestModel);

        await sutProvider.GetDependency<ICurrentContext>().Received(1).ManageGroups(organization.Id);
        await sutProvider.GetDependency<IUpdateGroupCommand>().Received(1).UpdateGroupAsync(
            Arg.Is<Group>(g =>
                g.OrganizationId == organization.Id && g.Name == groupRequestModel.Name &&
                g.AccessAll == groupRequestModel.AccessAll),
            Arg.Is<Organization>(o => o.Id == organization.Id),
            Arg.Any<ICollection<CollectionAccessSelection>>(),
            Arg.Any<IEnumerable<Guid>>());
        Assert.Equal(groupRequestModel.Name, response.Name);
        Assert.Equal(organization.Id, response.OrganizationId);
        Assert.Equal(groupRequestModel.AccessAll, response.AccessAll);
    }
}
