﻿using Orbis.Internals;
using System;
using System.IO;

namespace Orbis
{

    public static class Program
    {
        public static void Main(string[] args)
        {
            Util.PrepareAsemblies();
#if ORBIS
            var Display = new Entrypoint();
            Display.Run();
#endif
        }
    }

}