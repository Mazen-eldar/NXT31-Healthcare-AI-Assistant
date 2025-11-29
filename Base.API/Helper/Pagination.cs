namespace Base.API.Helper
{
    public class Pagination<T> where T : class
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }
        public ICollection<T> list { get; set; }

        public Pagination(int pageIndex, int pageSize, int count, ICollection<T> data)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            Count = count;
            list = data;
        }
    }
}
