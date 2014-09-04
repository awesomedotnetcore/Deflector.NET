using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleLibrary
{
    public class SampleClassWithProperties
    {
        public SampleClassWithProperties(int value)
        {
            Value = value;
        }

        public int Value { get; set; }
    }
}
