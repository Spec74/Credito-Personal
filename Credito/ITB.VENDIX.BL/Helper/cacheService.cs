using System;
using System.Web;
using System.Web.Security;

namespace ITB.VENDIX.BL
{
    public class VendixCache1 : ICacheService
    {
        public T Get<T>(string cacheId, Func<T> getItemCallback) where T : class
        {
            T item = HttpRuntime.Cache.Get(cacheId) as T;
            if (item == null)
            {
                item = getItemCallback();
                HttpContext.Current.Cache.Insert(cacheId, item);
            }
            return item;
        }

        public static void Crear(string cacheId,string valor) 
        {
            HttpContext.Current.Cache.Insert(cacheId, valor);
        }

        public static string Obtener(string cacheId)
        {
            if (HttpRuntime.Cache.Get(cacheId)==null)
            {
                FormsAuthentication.SignOut();
                FormsAuthentication.RedirectToLoginPage();
            }

            return HttpRuntime.Cache.Get(cacheId) as string;
        }

    }

    interface ICacheService
    {
        T Get<T>(string cacheId, Func<T> getItemCallback) where T : class;
    }

}