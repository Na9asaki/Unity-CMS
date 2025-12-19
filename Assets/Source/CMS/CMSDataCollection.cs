using System;
using System.Collections.Generic;
using Source.CMS.CMSData;

namespace CMS
{
    public class CMSDataCollection
    {
        private Dictionary<Type, CMSRootData> _data = new Dictionary<Type, CMSRootData>();

        public void AddTo<T>(CMSRootData data)
        {
            _data[typeof(T)] = data;
        }

        public void AddTo(Type type, CMSRootData data)
        {
            _data[type] = data;
        }

        public T GetBy<T>() where T : CMSRootData
        {
            return _data[typeof(T)] as T;
        }
    }
}