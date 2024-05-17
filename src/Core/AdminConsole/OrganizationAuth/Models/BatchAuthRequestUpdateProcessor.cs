using Bit.Core.Auth.Entities;
using Bit.Core.Enums;

namespace Bit.Core.AdminConsole.OrganizationAuth.Models;

public class BatchAuthRequestUpdateProcessor<T> where T : AuthRequest
{
    public List<AuthRequestUpdateProcessor<T>> Processors { get; } = new List<AuthRequestUpdateProcessor<T>>();
    private List<AuthRequestUpdateProcessor<T>> _processed { get; set; } = new List<AuthRequestUpdateProcessor<T>>();

    public BatchAuthRequestUpdateProcessor(
        ICollection<T> authRequests,
        IEnumerable<OrganizationAuthRequestUpdate> updates,
        AuthRequestUpdateProcessorConfiguration configuration
    )
    {
        Processors = authRequests.Select(ar =>
        {
            return new AuthRequestUpdateProcessor<T>(
                ar,
                updates.FirstOrDefault(u => u.Id == ar.Id),
                configuration
            );
        }).ToList();
    }

    public BatchAuthRequestUpdateProcessor<T> Process(Action<Exception> errorHandlerCallback)
    {
        _processed = new List<AuthRequestUpdateProcessor<T>>();
        foreach (var processor in Processors)
        {
            try
            {
                _processed.Add(processor.Process());
            }
            catch (AuthRequestUpdateProcessingException e)
            {
                errorHandlerCallback(e);
            }
        }
        return this;
    }

    public async Task<BatchAuthRequestUpdateProcessor<T>> Save(Func<IEnumerable<AuthRequest>, Task> callback)
    {
        if (_processed.Count < 1)
        {
            return this;
        }
        await callback(_processed.Select(p => p?.ProcessedAuthRequest));
        return this;
    }

    // Currently events like notifications, emails, and event logs are still
    // done per-request in a loop, which is different than saving updates to
    // the database. Saving can be done in bulk all the way through to the
    // repository.
    //
    // Perhaps these operations should be extended to be more batch-friendly
    // as well.
    public async Task<BatchAuthRequestUpdateProcessor<T>> SendPushNotifications(Func<T, Task> callback)
    {
        foreach (var processor in _processed)
        {
            await processor.SendPushNotification(callback);
        }
        return this;
    }

    public async Task<BatchAuthRequestUpdateProcessor<T>> SendNewDeviceEmails(Func<T, string, Task> callback)
    {
        foreach (var processor in _processed)
        {
            await processor.SendNewDeviceEmail(callback);
        }
        return this;
    }

    public async Task<BatchAuthRequestUpdateProcessor<T>> SendEventLogs(Func<T, EventType, Task> callback)
    {
        foreach (var processor in _processed)
        {
            await processor.SendEventLog(callback);
        }
        return this;
    }
}
