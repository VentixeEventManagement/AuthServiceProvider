namespace Presentation.Interfaces
{
    public interface IAuthServiceBusHandler
    {
        Task PublishAsync(string payload);
    }
}