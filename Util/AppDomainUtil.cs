using System;
using System.Linq;
using System.Reflection;

namespace PropHunt.Util
{
    internal static class AppDomainUtil
    {
        /// <summary>
        /// Retrieve an assembly from an app domain by its name.
        /// </summary>
        /// <param name="appDomain">The app domain to retrieve the assembly from</param>
        /// <param name="assemblyName">The name of the assembly to retrieve</param>
        /// <returns>The assembly with a matching name</returns>
        public static Assembly GetAssemblyByName(this AppDomain appDomain, string assemblyName)
        {
            return appDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName);
        }
    }
}