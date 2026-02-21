namespace diplom.API.DTOs
{
    public enum UserActivityStateDto
    {
        Offline = 0,
        OnlineIdle = 1,
        OnlineActive = 2
    }

    public sealed class UserActivityDto
    {
        public int UserId { get; set; }
        public UserActivityStateDto State { get; set; }
    }
}

