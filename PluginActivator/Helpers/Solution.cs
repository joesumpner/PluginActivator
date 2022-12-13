using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginActivator.Helpers
{
    internal class Solution
    {
        public string SolutionUniqueName { get; }
        public bool EnablePluginSteps { get; }

        public Solution(string solutionUniqueName, bool enablePluginSteps)
        {
            SolutionUniqueName = solutionUniqueName;
            EnablePluginSteps = enablePluginSteps;
        }
    }
}
