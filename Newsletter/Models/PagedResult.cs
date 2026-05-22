namespace Newsletter.Models
{
    public class PagedResult<T>

    {
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<T> Items { get; set; } = new List<T>();
    }
}
