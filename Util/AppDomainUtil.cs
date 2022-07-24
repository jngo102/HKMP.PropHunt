using System;
using System.Linq;
using System.Reflection;

namespace PropHunt.Util
{
    internal static class AppDomainUtil
    {
        public static Assembly GetAssemblyByName(this AppDomain appDomain, string assemblyName)
        {
            return appDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
        }
    }
}