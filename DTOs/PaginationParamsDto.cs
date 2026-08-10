namespace API_Ecommerce.DTOs
{
    public class PaginationParamsDtos
    {
        public int PageNumber { get; set; } = 1;
        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > 50) ? 50 : (value < 1 ? 1 : value); // Limit max page size to 50
        }
    }
}