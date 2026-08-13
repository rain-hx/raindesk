using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace rainspace.Util
{
    public static class DeepCopyUtil
    {
        // PERF: Optimized settings for deep copy:
        // - Formatting.None: no pretty-printing, faster serialization
        // - NullValueHandling.Ignore: skip null fields, smaller output
        // - No PreserveReferencesHandling: deep copy needs independent objects,
        //   and skipping $ref tracking is significantly faster
        private static readonly JsonSerializerSettings _settings = new()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Deep copy via JSON serialization.
        /// PERF: Uses Formatting.None and skips reference tracking for faster serialization.
        /// Uses StreamWriter/StreamReader to reduce string allocations for large objects.
        /// </summary>
        public static T DeepCopy<T>(T obj)
        {
            if (obj == null) return default;

            using var sw = new StringWriter();
            using (var writer = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var serializer = JsonSerializer.Create(_settings);
                serializer.Serialize(writer, obj);
            }

            string json = sw.ToString();
            using var sr = new StringReader(json);
            using var reader = new JsonTextReader(sr);
            var deserializer = JsonSerializer.Create(_settings);
            return deserializer.Deserialize<T>(reader);
        }

        /// <summary>
        /// Deep copy for collections.
        /// </summary>
        public static List<T> DeepCopyList<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return new List<T>();

            using var sw = new StringWriter();
            using (var writer = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var serializer = JsonSerializer.Create(_settings);
                serializer.Serialize(writer, list);
            }

            string json = sw.ToString();
            using var sr = new StringReader(json);
            using var reader = new JsonTextReader(sr);
            var deserializer = JsonSerializer.Create(_settings);
            return deserializer.Deserialize<List<T>>(reader) ?? new List<T>();
        }
    }
}
