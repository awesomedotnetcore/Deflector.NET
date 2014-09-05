using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public static class MethodCallMapRegistry
    {
        public static IMethodCallMap CreateMap(MethodBase method)
        {
            return new MethodCallMap();
        }
    }
}
