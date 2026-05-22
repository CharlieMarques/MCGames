namespace Newsletter.DTOs.Reviews
{
    public class ReviewUpdateDto
    {
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public bool State { get; set; }
    }
}
