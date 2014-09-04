using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleLibrary
{
    public class SampleClassThatCallsAnInstanceMethod
    {
        public void DoSomething()
        {
            var self = new SampleClassThatCallsAnInstanceMethod();
            self.DoSomethingElse();
        }

        public void DoSomethingElse()
        {
            Console.WriteLine("DoSomethingElse called");
        }
    }
}
