namespace Newsletter.DTOs.Reviews
{
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public bool State { get; set; }
        public DateTime ReviewDate { get; set; }
    }
}
