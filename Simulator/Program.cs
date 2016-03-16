using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Dit programma heeft maar 1 doel: controleren hoe objectief de stemwijzer is. 
    /// 
    /// Hoe het werkt?
    /// 
    /// - De data komt direct van www.stemwijzer.nl . De ruwe data is te vinden op: http://www.stemwijzer.nl/TK2012/js/data.js .
    /// - Het algoritme voor 'afstand' komt van de stemwijzer. Het algoritme is te vinden op: http://www.stemwijzer.nl/TK2012/js/app.js ; 
    ///   computeDistance heet de functie.
    /// - De berekening zelf is te vinden in dezelfde app.js, methode: "recalc()". 
    /// 
    /// Het programma maakt vervolgens samples. Een sample is het equivalent van als 1.000 mensen een willekeurig iets invullen op 
    /// de stemwijzer. Zolang we significante verschillen vinden hierin, gaan we hiermee door met een minimum van een steekproef 
    /// van 1.000 samples (= 1.000.000 stemmen). 
    /// 
    /// Als de berekening is geconvergeerd, berekenen we wie de verkiezingen gaan winnen op basis van willekeurige stemmingen in de 
    /// stemwijzer. Deze getallen leggen we naast de werkelijke getallen van de verkiezingsuitslag en schrijven we op het scherm.
    /// 
    /// Een paar kanttekeningen:
    /// 
    /// - Er wordt geen rekening gehouden met of mensen bepaalde onderwerpen belangrijker of minder belangrijk vinden. 
    /// - Er wordt alleen gekeken naar het nummer 1 advies. De rest wordt niet meegenomen.
    /// </summary>
    class Program
    {
        public class Resultaat
        {
            public string Naam;
            public double Verwacht;
            public double Werkelijk;
            public double Stdev;
        }

        static void Main(string[] args)
        {
            // Initializeer de data van de stemwijzer. Bron: http://www.stemwijzer.nl/TK2012/js/data.js
            Data d = new Data();

            // Initializeer een random number generator.
            Random rnd = new Random();

            // Begin van de simulatie:
            Console.WriteLine("Simulatie van een verkiezing:");

            List<double[]> samples = new List<double[]>();

            const double limit = 0.00001;

            double[] prevstdev = new double[d.numberParties];
            double conversion = 1;

            // Zolang we significante verschillen in de standaarddeviatie vinden, willen we een aselecte steekproef nemen.
            // Het minimum aantal steekproeven is op 1000 gezet, hoewel in de meeste gevallen 50-200 al genoeg is. Vrij 
            // vertaalt betekent dit dat we het liever "safe" spelen.
            while (conversion > limit || samples.Count < 1000)
            {
                // Maak een nieuwe sample van 1000 stemmen:
                samples.Add(CreateSample(d, rnd));

                // Om te weten of we significante vooruitgang maken, moeten we kijken naar het verschil in standaarddeviatie. 
                // Als dit gelijk blijft, constateren we dat ons algoritme geconvergeerd is. 
                //
                // We berekenen de standaarddeviatie voor iedere partij en controleren vervolgens de maximale waarde hierin.
                double[] stdev = CalculateStdev(d, samples);

                conversion = 0;
                for (int i = 0; i < d.numberParties; ++i)
                {
                    double localConversion = Math.Abs(stdev[i] - prevstdev[i]);
                    if (localConversion > conversion)
                    {
                        conversion = localConversion;
                    }
                }
                prevstdev = stdev;

                Console.Write("Samples: {0},conversion: {1:0.0000}%             \r", samples.Count, 100.0 * conversion);
            }

            double maxstdev = prevstdev.Max();
            Console.WriteLine();
            Console.WriteLine("Converged. Verwachte fout: (3 σ ~ 99,73% accuracy) = {0:0.00}%", 3 * maxstdev * 100.0);

            // Bereken het resultaat van de steekproef (normaliseer op 100%):
            double total = 0;
            double[] avg = new double[d.numberParties];
            foreach (var item in samples)
            {
                for (int i = 0; i < d.numberParties; ++i)
                {
                    avg[i] += item[i];
                    total += item[i];
                }
            }

            for (int i = 0; i < d.numberParties; ++i)
            {
                avg[i] /= total;
            }


            // Wat is de werkelijkheid?
            var w = new Werkelijk();
            int totaalAantalZetels = w.uitslag.Sum((a) => a.Value);

            // Bereken de verwachte uitslag op basis van de kieswijzer
            Resultaat[] resultaten = new Resultaat[d.numberParties];
            for (int i = 0; i < d.numberParties; ++i)
            {
                string naam = d.partijen[i];

                resultaten[i] = new Resultaat()
                {
                    Naam = naam,
                    Stdev = prevstdev[i],
                    Verwacht = avg[i],
                    Werkelijk = (double)w.uitslag[naam] / (double)totaalAantalZetels
                };
            }

            Console.WriteLine();
            Console.WriteLine("Wie gaat de verkiezingen winnen?");
            Console.WriteLine();

            foreach (var item in resultaten.OrderByDescending((a) => a.Verwacht))
            {
                double real = 100.0 * item.Werkelijk;
                double expected = 100.0 * item.Verwacht;
                double error = Math.Abs(expected - real);

                string naam = item.Naam + new string(' ', 30 - item.Naam.Length);

                Console.WriteLine("- {0} verwacht: {1:0.00}%\twerkelijk: {2:0.00}%\tfout: {3:0.00}%", naam, expected, real, error);
            }
            Console.ReadLine();
        }

        private static double[] CalculateStdev(Data d, List<double[]> samples)
        {
            // Bereken eerst het gemiddelde:

            double[] avg = new double[d.numberParties];
            foreach (var sample in samples)
            {
                for (int i = 0; i < d.numberParties; ++i)
                {
                    avg[i] += sample[i];
                }
            }
            for (int i = 0; i < d.numberParties; ++i)
            {
                avg[i] /= samples.Count;
            }

            // Bereken ∑ (E(x)-Ex)^2 :

            double[] stdev = new double[d.numberParties];
            foreach (var sample in samples)
            {
                for (int i = 0; i < d.numberParties; ++i)
                {
                    stdev[i] += (avg[i] - sample[i]) * (avg[i] - sample[i]);
                }
            }

            // Bereken de standaarddeviatie

            for (int i = 0; i < d.numberParties; ++i)
            {
                stdev[i] = Math.Sqrt(stdev[i] / samples.Count);
            }

            return stdev;
        }

        static double[] CreateSample(Data d, Random rnd)
        {
            // Maak een sample: het gemiddelde van wat mensen als nummer 1 advies zouden moeten stemmen in 
            // 1000 willekeurige steekproeven:

            double[] freq = new double[d.numberParties];

            // Doe 1000 steekproeven:
            for (int i = 0; i < 1000; ++i)
            {
                int[] dist = new int[d.numberParties];

                // Voor ieder willekeurig topic...
                for (int j = 0; j < Data.numberTopics; ++j)
                {
                    // Breng een willekeurige stem uit (-1, 0 of 1):
                    int vote = rnd.Next(-1, 2);

                    for (int k = 0; k < d.numberParties; ++k)
                    {
                        // De data 'zegt' waar partijen voor staan. Als dit overeenkomt, geven we een score van 1, anders 0. 
                        // Dit is hoe de stemwijzer zijn score berekent. Bron: app.js (zie bovenin)

                        int partyvote = d.values[j][k];

                        dist[k] += (vote == partyvote) ? 1 : 0;
                    }
                }

                KeyValuePair<int, int>[] tmp = new KeyValuePair<int, int>[d.numberParties];
                for (int j = 0; j < d.numberParties; ++j)
                {
                    tmp[j] = new KeyValuePair<int, int>(j, dist[j]);
                }

                // De stemwijzer sorteert vervolgens de resultaten op score. De partij die de hoogste score heeft, komt 
                // bovenaan het lijstje. De partij daarna daar net onder, etc.
                //
                // We kijken alleen naar het nummer 1 advies:
                int argmax = 0;
                int argval = dist[0];
                for (int j = 1; j < dist.Length; ++j)
                {
                    if (dist[j] > argval)
                    {
                        argmax = j;
                        argval = dist[j];
                    }
                }

                // Update de frequentie voor het nummer 1 advies:
                freq[argmax] += 1.0;
            }

            // Normalizeer de resultaten:
            double[] dfreq = new double[d.numberParties];

            for (int i = 0; i < d.numberParties; ++i)
            {
                dfreq[i] = (double)freq[i] / (double)1000;
            }

            return dfreq;
        }
    }
}
