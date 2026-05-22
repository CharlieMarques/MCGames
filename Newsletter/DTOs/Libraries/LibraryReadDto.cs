namespace Newsletter.DTOs.Libraries
{
    public class LibraryReadDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string>? GameNames { get; set; } 
    }
}
