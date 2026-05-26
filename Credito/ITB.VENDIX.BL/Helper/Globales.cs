using System;
using System.Collections.Generic;
using System.Web;

namespace ITB.VENDIX.BL
{
    public class VendixGlobal<T1>
    {
        public static Dictionary<string, Dictionary<string, T1>> DiccionarioSession = new Dictionary<string, Dictionary<string, T1>>();

        #region "Contructor"
        private VendixGlobal()
        {
            HttpContext.Current.Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            HttpContext.Current.Response.Cache.SetNoStore();
        }
        #endregion

        #region "Métodos Globales"
        public static void Crear(string param, T1 obj)
        {
            if (HttpContext.Current == null)
                return;
            string sessionid = HttpContext.Current.Session.SessionID;

            if (!DiccionarioSession.ContainsKey(sessionid))
            {
                DiccionarioSession.Add(sessionid, new Dictionary<string, T1>());
            }
            if (DiccionarioSession[sessionid].ContainsKey(param))
            {
                DiccionarioSession[sessionid].Remove(param);
            }
            DiccionarioSession[sessionid].Add(param, obj);

        }

        public static void Eliminar(string param)
        {
            if (HttpContext.Current == null)
                return;
            string sessionid = HttpContext.Current.Session.SessionID;

            if (DiccionarioSession.ContainsKey(sessionid))
            {
                if (DiccionarioSession[sessionid].ContainsKey(param))
                {
                    DiccionarioSession[sessionid].Remove(param);
                }
            }

        }
        #endregion

        #region "Funciones Globales"
        public static T1 Obtener(string param)
        {
            T1 functionReturnValue = default(T1);
            if (HttpContext.Current == null)
                return functionReturnValue;

            string sessionid = HttpContext.Current.Session.SessionID;

            if (!DiccionarioSession.ContainsKey(sessionid))
            {
                return functionReturnValue;
            }
            if (!DiccionarioSession[sessionid].ContainsKey(param))
            {
                return functionReturnValue;
            }
            return DiccionarioSession[sessionid][param];
        }

        public static bool Existe(string param)
        {
            if (HttpContext.Current == null)
                return false;
            string sessionid = HttpContext.Current.Session.SessionID;

            if (!DiccionarioSession.ContainsKey(sessionid))
            {
                return false;
            }
            if (DiccionarioSession[sessionid].ContainsKey(param))
            {
                return true;
            }
            return false;
        }

        public static bool ExisteDiccionarioAplication(string param, T1 obj)
        {
            bool existe = false;
            foreach (Dictionary<string, T1> dc in DiccionarioSession.Values)
            {
                if (!existe && dc[param].Equals(obj))
                {
                    existe = true;
                }
            }
            return existe;
        }

        public static void EliminarSesion(string pSessionid)
        {
            if (pSessionid != null && DiccionarioSession != null && DiccionarioSession.ContainsKey(pSessionid))
            {
                DiccionarioSession[pSessionid].Clear();
                DiccionarioSession.Remove(pSessionid);
            }
        }

        #endregion

        #region "Elimnar Toda la Clase Global"
        public static void EliminarTodo()
        {
            if (HttpContext.Current == null)
                return;
            string sessionid = HttpContext.Current.Session.SessionID;

            if (sessionid != null && DiccionarioSession != null && DiccionarioSession.ContainsKey(sessionid))
            {
                DiccionarioSession[sessionid].Clear();
                DiccionarioSession.Remove(sessionid);
            }
        }

        #endregion
    }
    public class VendixGlobal
    {
        public static int GetUsuarioId()
        {
            return VendixGlobal<int>.Obtener("UsuarioId");
        }
        public static int GetOficinaId()
        {
            return VendixGlobal<int>.Obtener("OficinaId");
        }
        public static int GetCajaDiarioId()
        {
            return VendixGlobal<int>.Obtener("CajadiarioId");
        }

        public static int GetBovedaId()
        {
            return VendixGlobal<int>.Obtener("BovedaId");
        }

        public static DateTime GetFecha()
        {
            //return DateTime.Now.AddHours(3);
            return CreditoBL.ObtenerFechaBD();
        }
        public static string GetNombreMes(int nroMes)
        {
            switch (nroMes)
            {
                case 1: return "ENERO";
                case 2: return "FEBRERO";
                case 3: return "MARZO";
                case 4: return "ABRIL";
                case 5: return "MAYO";
                case 6: return "JUNIO";
                case 7: return "JULIO";
                case 8: return "AGOSTO";
                case 9: return "SETIEMBRE";
                case 10: return "OCTUBRE";
                case 11: return "NOVIEMBRE";
                case 12: return "DICIEMBRE";
                default: return "";                    
            }            
        }
    }
}



