namespace PRM_PRJ.Services.inf
{
    public interface IUserContext
    {
        Guid GetUserId();

        string GetUsername();

        string GetRole();
    }
}