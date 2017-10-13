using System;

namespace mssqlstats
{
    class Query
    {
        public enum QueryTarget
        {
            System,
            Database,
            Instance
        }

        String querytext = String.Empty;
        QueryTarget querytarget = QueryTarget.Instance;

        public Query(String querytext, QueryTarget querytarget)
        {
            this.Text = querytext;
            this.Target = querytarget;
        }

        public string Text
        {
            get
            {
                return querytext;
            }
            set
            {
                querytext = value;
            }
        }
        public QueryTarget Target
        {
            get
            {
                return querytarget;
            }
            set
            {
                querytarget = value;
            }
        }
    }
}
