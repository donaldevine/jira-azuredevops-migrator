using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Migration.Common
{
    public class BaseMapper<TRevision> where TRevision : ISourceRevision
    {
        private string _domainMapping;
        public BaseMapper(string userMappingPath, string domainMapping)
        {
            _domainMapping = domainMapping;
            ParseUserMappings(userMappingPath);
        }

        protected Dictionary<string, string> UserMapping { get; private set; } = new Dictionary<string, string>();
        protected void ParseUserMappings(string userMappingPath)
        {
            if (!File.Exists(userMappingPath))
                return;

            string[] userMappings = File.ReadAllLines(userMappingPath);
            foreach (var userMapping in userMappings.Select(um => um.Split('=')))
            {
                string jiraUser = userMapping[0].Trim();
                string wiUser = userMapping[1].Trim();

                UserMapping.Add(jiraUser, wiUser);
            }
        }

        protected virtual string MapUser(string sourceUser)
        {
            if (sourceUser == null)
                return sourceUser;

            if (UserMapping.TryGetValue(sourceUser, out string wiUser))
            {
                return wiUser;
            }
            else if (UserMapping.TryGetValue("*", out string defaultUser))
            {
                Logger.Log(LogLevel.Debug, $"Could not find user {sourceUser} identity. Setting default identity.");
                return defaultUser;
            }
            else
            {                
                if (!string.IsNullOrEmpty(_domainMapping))
                {
                    Logger.Log(LogLevel.Debug, $"Domain Mapping for {sourceUser} identity.");
                    var domainMap = _domainMapping.Split('=');

                    if (domainMap.Length == 2 && domainMap[0].Length > 0 && domainMap[1].Length > 0)
                    {
                        string sourceDomain = domainMap[0].Trim();
                        string destDomain = domainMap[1].Trim();

                        var destUser = sourceUser.Replace(sourceDomain, destDomain);
                        
                        Logger.Log(LogLevel.Debug, $"Identity Domain Mapped for {sourceUser} to {destUser}.");

                        UserMapping.Add(sourceUser, destUser);

                        return destUser;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, $"Could not find user {sourceUser} identity. Using original identity.");
                        UserMapping.Add(sourceUser, sourceUser);
                        return sourceUser;
                    }

                }
                else
                {
                    Logger.Log(LogLevel.Debug, $"Could not find user {sourceUser} identity. Using original identity.");
                    UserMapping.Add(sourceUser, sourceUser);
                    return sourceUser;
                }                
            }
        }

        protected string ReadEmbeddedFile(string resourceName)
        {
            var assembly = Assembly.GetEntryAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        protected FieldMapping<TRevision> MergeMapping(params FieldMapping<TRevision>[] mappings)
        {
            var merged = new FieldMapping<TRevision>();
            foreach (var mapping in mappings)
            {
                foreach (var m in mapping)
                    if (!merged.ContainsKey(m.Key))
                        merged[m.Key] = m.Value;
            }
            return merged;
        }

        protected string Crop(string value, int maxSize)
        {
            var max = Math.Min(value.Length, maxSize);
            return value.Substring(0, max);
        }
    }
}
