namespace Newsletter.Models
{
    public class Library
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        public List<GameLibrary>? GameLibraries { get; set; }
    }
}
