using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// De werkelijke uitslag van de verkiezingen. Bron: https://nl.wikipedia.org/wiki/Tweede_Kamerverkiezingen_2012 .
    /// </summary>
    public class Werkelijk
    {
        public Werkelijk()
        {
            AddUitslag("VVD", 31);
            AddUitslag("PvdA", 30);
            AddUitslag("PVV", 24);
            AddUitslag("CDA", 21);
            AddUitslag("SP", 15);
            AddUitslag("D66", 10);
            AddUitslag("GroenLinks", 10);
            AddUitslag("ChristenUnie", 5);
            AddUitslag("SGP", 2);
            AddUitslag("Partij voor de Dieren", 2);
        }

        public Dictionary<string, int> uitslag = new Dictionary<string, int>();

        private void AddUitslag(string name, int zetels)
        {
            uitslag.Add(name, zetels);
        }
    }
}
