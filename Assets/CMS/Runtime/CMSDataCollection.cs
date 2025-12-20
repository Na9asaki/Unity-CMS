using System;
using System.Collections.Generic;
using System.Linq;
using CMS.CMSData;
using CMS;

namespace CMS
{
    public class CMSDataCollection
    {
        private Dictionary<Type, Dictionary<string, CMSRootData>> _data = new();

        public void AddTo<T>(CMSRootData data)
        {
            AddTo(typeof(T), data);
        }

        public void AddTo(Type type, CMSRootData data)
        {
            if (!_data.TryGetValue(type, out var dict))
            {
                dict = new Dictionary<string, CMSRootData>();
                _data[type] = dict;
            }
            else if (dict.ContainsKey(data.Id))
            {
                throw new Exception("Id " + data.Id + " already exists");
            }

            dict.Add(data.Id, data);
        }

        public T GetFirstBy<T>() where T : CMSRootData
        {
            if (!_data.ContainsKey(typeof(T))) throw new ArgumentException($"{typeof(T)} is not registered");
            return _data[typeof(T)].Values.FirstOrDefault()?.As<T>();
        }

        [Obsolete("This method is obsolete, please use GetFirstBy<T>() instead")]
        public T GetBy<T>() where T : CMSRootData
        {
            return GetFirstBy<T>()?.As<T>();
        }

        public T GetBy<T>(string id) where T : CMSRootData
        {
            if (!_data.ContainsKey(typeof(T))) throw new ArgumentException($"{typeof(T)} is not registered");
            return _data[typeof(T)][id].As<T>();
        }

        public IEnumerable<T> GetAllBy<T>() where T : CMSRootData
        {
            if (!_data.ContainsKey(typeof(T))) throw new ArgumentException($"{typeof(T)} is not registered");
            return _data[typeof(T)].Values.Select(x => x.As<T>());
        }
    }
}