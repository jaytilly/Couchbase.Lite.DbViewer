using Newtonsoft.Json;
using System;

namespace DbViewer.Shared.Couchbase
{
    public static class CbUtils
    {
        public static T ParseTo<T>(string json, Action<Exception> onError = null)
        {
            T retVal = default(T);
            try
            {
                var settings = new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTimeOffset,
                    TypeNameHandling = TypeNameHandling.All
                };
                retVal = JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }

            return retVal;
        }
    }
}
