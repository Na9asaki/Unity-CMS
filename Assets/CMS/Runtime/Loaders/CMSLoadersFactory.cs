using System;
using System.Linq;
using UnityEngine;
using CMS;

namespace CMS.Loaders
{
    public static class CMSLoadersFactory
    {
        public static CMSBaseLoader Instance(Type loaderType)
        {
            var newLoader = Activator.CreateInstance(loaderType) as CMSBaseLoader;
            return newLoader;
        }

        public static CMSBaseLoader Instance(string loaderType)
        {
            var stringToType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(x => x.FullName.Equals(loaderType));
            if (stringToType == null) throw new Exception(loaderType + " not found");
            var newLoader = Activator.CreateInstance(stringToType) as CMSBaseLoader;
            return newLoader;
        }
    }
}