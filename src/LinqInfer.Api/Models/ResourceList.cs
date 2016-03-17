using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Api.Models
{
    public class ResourceList<T>
    {
        public ResourceList(IEnumerable<T> items = null)
        {
            Items = items == null ? new List<T>() : items.ToList();
        }

        public ICollection<T> Items { get; private set; }
    }
}