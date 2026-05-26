using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ITB.VENDIX.BL
{
    public class GridDataRequest
    {
        public string sidx { get; set; }
        public string sord { get; set; }
        public int page { get; set; }
        public int rows { get; set; }
        public string rowList { get; set; }
        
        private string _filters;

        public string filters
        {
            get
            {
                return _filters;
            }
            set
            {
                _filters = value;
                _DataFilters = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GridDataRequest"/> class.
        /// </summary>
        public GridDataRequest()
        {
            //Setting Default Values
            sidx = string.Empty;
            sord = "asc";
            page = 1;
            rows = 50;
            filters = string.Empty;
            rowList = "10,50,100";
        }

        private Dictionary<string, string> _DataFilters;

        /// <summary>
        /// Datas the filters.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> DataFilters()
        {
            if (_DataFilters == null)
            {
                _DataFilters=ParseFilters();
            }

            return _DataFilters;
        }

        /// <summary>
        /// Parses the filters.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> ParseFilters()
        {
            Dictionary<string, string> dict = null;
            if (string.IsNullOrEmpty(filters))
            {
                dict = new Dictionary<string, string>();
            }
            else
            {
                dict = Deserialise<Dictionary<string, string>>(_filters);
            }
            return dict;
        }

        /// <summary>
        /// Deserialises the specified json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        private T Deserialise<T>(string json)
        {
            T obj = Activator.CreateInstance<T>();
            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }
    }
}