using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnnotationToEPub
{
    class Position
    {
        public int document=0;
        public int paragraph = 0;
        public int startGroup = 0;
        public int endGroup = 0;
        public int startPos = 0;
        public int endPos = 0;
        public string position = "";

        public Position(string positionString)
        {
            position = positionString;
            var pos = Regex.Match(position, @"\/6\/(\d+)\!\/4\/(\d+)(.+)");
            if (!pos.Success)
            {
                //Console.WriteLine("Position pattern did not match: " + position);
                return;
            }
            var charPos = Regex.Match(pos.Groups[3].Value, @"(.*),(.+),(.+)");
            if (!charPos.Success)
            {
                //Console.WriteLine("Character position pattern did not match: " + position);
                return;
            }
            var posReg = Regex.Match(charPos.Groups[1].Value+ charPos.Groups[3].Value, @"\/(\d+):(\d+)");
            if (!posReg.Success)
            {
                //Console.WriteLine("Second position string mismatch: " + position);
                return;
            }
            posReg = Regex.Match(charPos.Groups[1].Value + charPos.Groups[2].Value, @"\/(\d+):(\d+)");
            if (!posReg.Success)
            {
                //Console.WriteLine("First position string mismatch: " + position);
                return;
            }
            document = Int32.Parse(pos.Groups[1].Value);
            paragraph = Int32.Parse(pos.Groups[2].Value);
            startGroup = Int32.Parse(posReg.Groups[1].Value);
            startPos = Int32.Parse(posReg.Groups[2].Value);
            posReg = Regex.Match(charPos.Groups[1].Value + charPos.Groups[3].Value, @"\/(\d+):(\d+)");
            endGroup = Int32.Parse(posReg.Groups[1].Value);
            endPos = Int32.Parse(posReg.Groups[2].Value);
        }

    }
}
