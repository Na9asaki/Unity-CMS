using System;

namespace CMS.Loaders
{
    public static class CMSLoaderReflection
    {
        /// <summary>
        /// Возвращает тип CMSRootData (T) из CMSLoader&lt;T&gt;
        /// </summary>
        public static Type GetDataTypeFromLoader(Type loaderType)
        {
            if (loaderType == null)
                return null;

            var current = loaderType;

            while (current != null)
            {
                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition() == typeof(CMSLoader<>))
                {
                    return current.GetGenericArguments()[0];
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}