using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Microservices.Resources
{
    public static class ResourceExtensions
    {
        public static Resource<T> ToResource<T>(this T item)
        {
            return new Resource<T>() { Data = item };
        }

        /// <summary>
        /// Creates a resource list
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        /// <param name="items">The items</param>
        /// <param name="links">A list of links (split using an equals sign - e.g. docs=/mypath/docs</param>
        /// <returns></returns>
        public static ResourceList<T> ToResourceList<T>(this IEnumerable<T> items, params string[] links)
        {
            var list = new ResourceList<T>() { Data = items.ToList() };

            foreach (var link in links.Select(l => l.Split('=')))
            {
                list.Links[link[0]] = link[1];
            }

            return list;
        }
    }
}