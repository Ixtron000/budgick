namespace Infrastructure.Interfaces
{
    public interface ICommandHandler
    {
        Task ExecuteAsync();
    }
}
