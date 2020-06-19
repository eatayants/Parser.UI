using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser.DataModel
{
    public class ResultItemComparer : IEqualityComparer<ResultItem>
    {
        public bool Equals(ResultItem x, ResultItem y)
        {
            return x.Url == y.Url;
        }

        public int GetHashCode(ResultItem obj)
        {
            return obj.Url.GetHashCode();
        }
    }

    public class UrlComparer : IEqualityComparer<Uri>
    {
        public bool Equals(Uri x, Uri y)
        {
            return x.AbsoluteUri == y.AbsoluteUri;
        }

        public int GetHashCode(Uri obj)
        {
            return obj.AbsoluteUri.GetHashCode();
        }
    }

}
