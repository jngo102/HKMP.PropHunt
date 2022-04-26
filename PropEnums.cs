using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropHunt
{
    internal enum PropState
    {
        Free,
        TranslateXY,
        TranslateZ,
        Rotate,
        Scale,
    }

    internal enum PropHuntTeam
    {
        Hunters,
        Props,
    }
}
