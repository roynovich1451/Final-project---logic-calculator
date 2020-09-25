using System;
using System.Collections.Generic;

namespace LogicCalculator
{
    class AndRules
    {
        public AndRules(List<string> statementList,string statement,int startLine)
        {
            statementList.Add("R");
            Console.WriteLine(statementList[0]);
        }
    }
}
