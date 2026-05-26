using System.Web.Mvc;
using System.Web.Routing;


namespace Helper
{
    // Si no estamos logeado, regresamos al login
    public class AutenticadoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (!SessionHelper.ExistUserInSession())
            {
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Home",
                    action = "Login"
                }));
            }

            //string controlador = filterContext.RouteData.Values["Controller"].ToString();
            //if (controlador.ToLower() == "home") return;


            //if (!MenuBL.TienePermiso(controlador))
            //{
            //    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
            //    {
            //        controller = "Home",
            //        action = "SinPermiso"
            //    }));
            //}

        }
    }

    //Si estamos logeado ya no podemos acceder a la página de Login
    //public class NoLoginAttribute : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext filterContext)
    //    {
    //        base.OnActionExecuting(filterContext);

    //        if (SessionHelper.ExistUserInSession())
    //        {
    //            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary(new
    //            {
    //                controller = "Home",
    //                action = "Index"
    //            }));
    //        }
    //    }
    //}
}