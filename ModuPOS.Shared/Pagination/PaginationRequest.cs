using System;
using System.Collections.Generic;
using System.Text;

namespace ModuPOS.Shared.Pagination
{
    public class PaginationRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
