﻿using Bit.Core.Entities;
using Bit.Core.Enums;

namespace Bit.Core.Repositories;

public interface ISecretRepository
{
    Task<IEnumerable<Secret>> GetManyByOrganizationIdAsync(Guid organizationId);
    Task<IEnumerable<Secret>> GetManyByIds(IEnumerable<Guid> ids);
    Task<IEnumerable<Secret>> GetManyByProjectIdAsync(Guid projectId);
    Task<Secret> GetByIdAsync(Guid id);
    Task<Secret> CreateAsync(Secret secret);
    Task<Secret> UpdateAsync(Secret secret);
    Task SoftDeleteManyByIdAsync(IEnumerable<Guid> ids);
}
