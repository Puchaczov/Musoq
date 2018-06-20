using System;

namespace Musoq.Service.Client.Core
{
    public class QueryContext
    {
        public string Query { get; set; }

        public Guid QueryId { get; set; }

        public QueryContext() { }

        private QueryContext(Guid queryId, string query) {
            QueryId = queryId;
            Query = query;
        }

        public static QueryContext FromQueryText(string query)
        {
            return new QueryContext(Guid.NewGuid(), query);
        }

        public static QueryContext FromQueryText(Guid id, string query)
        {
            return new QueryContext(id, query);
        }
    }
}