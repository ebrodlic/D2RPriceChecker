using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Pipelines
{
    public class Component
    {
        public int Id { get; init; }
        public int Area { get; set; }
        public int MinX { get; set; }
        public int MinY { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
    }
}
