using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TravelDistance
{
    class DirectionStep
    {
        public int Index { get; set; }
        public string Description { get; set; }
        public int DistanceValue { get; set; }
        public string DistanceMI { get; set; }
        public string DistanceKM { get; set; }
        public string Status { get; set; }
    }

    class GEOCodeStep
    {
        public int Index { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string Status { get; set; }
    }
}
