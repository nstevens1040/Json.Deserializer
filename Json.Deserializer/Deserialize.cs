namespace Json
{
    using System;
    using System.Collections.Generic;
    using System.Web.Script.Serialization;
    using System.Globalization;
    using System.Management.Automation;
    using System.Reflection;
    internal class JsonObjectTypeResolver : JavaScriptTypeResolver
    {
        public override Type ResolveType(string id) => typeof(Dictionary<string, object>);

        public override string ResolveTypeId(Type type) => string.Empty;
    }
    public class JsonObject
    {
        private const int maxDepthAllowed = 1000;
        private static ICollection<object> PopulateFromList(
            ICollection<object> list,
            out ErrorRecord error
        )
        {
            error = (ErrorRecord)null;
            List<object> objectList = new List<object>();
            foreach (object obj in (IEnumerable<object>)list)
            {
                switch (obj)
                {
                    case IDictionary<string, object> _:
                        Dictionary<string, object> psObject = JsonObject.PopulateFromDictionary(obj as IDictionary<string, object>, out error);
                        if (error != null)
                        {
                            return (ICollection<object>)null;
                        }
                        objectList.Add((object)psObject);
                        continue;
                    case ICollection<object> _:
                        ICollection<object> objects = JsonObject.PopulateFromList(obj as ICollection<object>, out error);
                        if (error != null)
                        {
                            return (ICollection<object>)null;
                        }
                        objectList.Add((object)objects);
                        continue;
                    default:
                        objectList.Add(obj);
                        continue;
                }
            }
            return (ICollection<object>)objectList.ToArray();
        }
        private static Dictionary<string, object> PopulateFromDictionary(
            IDictionary<string, object> entries,
            out ErrorRecord error
        )
        {
            error = (ErrorRecord)null;
            Dictionary<string, object> psObject1 = new Dictionary<string, object>();
            string dup_keys = "Cannot convert the JSON string because a dictionary that was converted from the string contains the duplicated keys '{0}' and '{1}'.";
            foreach (KeyValuePair<string, object> entry in (IEnumerable<KeyValuePair<string, object>>)entries)
            {
                object value = null;
                try
                {
                    value = psObject1[entry.Key];
                }
                catch { }
                if (value != null)
                {
                    string message = string.Format((IFormatProvider)CultureInfo.InvariantCulture, dup_keys, (object)entry.Key, (object)entry.Key);
                    error = new ErrorRecord((Exception)new InvalidOperationException(message), "DuplicateKeysInJsonString", ErrorCategory.InvalidOperation, (object)null);
                    return (Dictionary<string, object>)null;
                }
                if (entry.Value is IDictionary<string, object>)
                {
                    Dictionary<string, object> psObject2 = JsonObject.PopulateFromDictionary(entry.Value as IDictionary<string, object>, out error);
                    if (error != null)
                    {
                        return (Dictionary<string, object>)null;
                    }
                    psObject1.Add(entry.Key, (object)psObject2);
                }
                else if (entry.Value is ICollection<object>)
                {
                    ICollection<object> objects = JsonObject.PopulateFromList(entry.Value as ICollection<object>, out error);
                    if (error != null)
                    {
                        return (Dictionary<string, object>)null;
                    }
                    psObject1.Add(entry.Key, (object)objects);
                }
                else
                {
                    psObject1.Add(entry.Key, entry.Value);
                }
            }
            return psObject1;
        }
        public JsonObject()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
        }
        public object ConvertFromJson(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            ErrorRecord error = (ErrorRecord)null;
            object obj = new JavaScriptSerializer((JavaScriptTypeResolver)new JsonObjectTypeResolver())
            {
                RecursionLimit = 1020,
                MaxJsonLength = int.MaxValue
            }.DeserializeObject(input);
            
            
            switch (obj)
            {
                case IDictionary<string, object> _:
                    obj = (object)JsonObject.PopulateFromDictionary(obj as IDictionary<string, object>, out error);
                    Dictionary<string,object> return_obj = (Dictionary<string,object>)obj;
                    return return_obj;
                    break;
                case ICollection<object> _:
                    obj = (object)JsonObject.PopulateFromList(obj as ICollection<object>, out error);
                    ICollection<object> return_obj = (ICollection<object>)obj;
                    return return_obj;
                    break;
            }
        }
    }
    public class Deserialize
    {
        public static object Convert(string inputJson)
        {
            JsonObject jo = new JsonObject();
            object deserialized = jo.ConvertFromJson(inputJson);
            return deserialized;
        }
    }
}
