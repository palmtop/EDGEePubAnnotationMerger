using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnnotationToEPub
{
    class Program
    {
        class annotation
        {
            public int RowId;
            public String Text;
            public String Color;
            public String Position;
            public Position objPosition;
            public bool processed = false;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Reading annotation file");
            var annotations = readAnnotations(@"C:\EPUB\ProcessAnnonation_1.csv");

            Console.WriteLine("Checking position strings");
            Position p;
            List<annotation> invalid = new List<annotation>();
            foreach (var annotation in annotations)
            {
                p = new Position(annotation.Position);
                if (p.document == 0)
                {
                    invalid.Add(annotation);
                    Console.WriteLine(annotation.RowId + " : " + annotation.Position);
                }
                else
                {
                    annotation.objPosition = p;
                }
            }
            foreach (var annotation in invalid)
            {
                annotations.Remove(annotation);
            }

            Console.WriteLine("Soring annotations");
            annotations.Sort(ComparePositions);

            Dictionary<int, String> documentToString = new Dictionary<int, string>();

            string[] files = Directory.GetFiles(@"C:\EPUB\", "*.html");
            string fileContents;
            int[] foundPosition;
            List<int> checkedDocs;
            Console.WriteLine("Finding Files");
            foreach (var file in files)
            {
                fileContents = File.ReadAllText(file);
                checkedDocs = new List<int>();
                foreach (var annotation in annotations)
                {
                    if (checkedDocs.Contains(annotation.objPosition.document)) continue;
                    if (documentToString.ContainsKey(annotation.objPosition.document)) continue;
                    foundPosition = GetTextFromHtml(fileContents, annotation.Position);
                    if (foundPosition[0] < 0) continue;
                    if (fileContents.Substring(foundPosition[0], foundPosition[1] - foundPosition[0]) == annotation.Text)
                    {
                        //Console.WriteLine("Found: " + annotation.Position);
                        documentToString[annotation.objPosition.document] = file;
                        break;
                    }
                    checkedDocs.Add(annotation.objPosition.document);
                }
            }
            Console.WriteLine(documentToString.Count + " Files Found");

            Console.WriteLine("Go trough all files and all annotations in that files");

            foreach (var file in documentToString)
            {
                fileContents = File.ReadAllText(file.Value);
                Console.WriteLine("Pathcing: " + file.Value);
                var query = annotations.Where(a => (a.objPosition.document == file.Key));
                foreach (var annotation in query)
                {
                    foundPosition = GetTextFromHtml(fileContents, annotation.Position);
                    //Skip if not found
                    if (foundPosition[0] < 0) continue;
                    //Skip if it has tags inside
                    if (fileContents.Substring(foundPosition[0], foundPosition[1] - foundPosition[0]).Contains("<")) continue;
                    fileContents=fileContents.Insert(foundPosition[1], "</span>");
                    fileContents=fileContents.Insert(foundPosition[0], "<span class=note_" + annotation.Color + " >");
                    annotation.processed = true;

                }
                var path = Path.GetDirectoryName(file.Value) + @"\new_"+Path.GetFileName(file.Value);
                File.WriteAllText(path, fileContents);
            }

            Console.WriteLine("Non processed annotations:");

            foreach (var annotation in annotations.Where(a => (!a.processed)))
            {
                Console.WriteLine(annotation.RowId+" : "+  annotation.Position);
            }


            Console.WriteLine("Ready :-)");
            Console.ReadKey();
        }

        private static int ComparePositions(annotation x, annotation y)
        {
            Int64 multi = 1000;
            Int64 xWeight = (((x.objPosition.document * multi) + x.objPosition.paragraph) * multi + x.objPosition.startGroup) * multi + x.objPosition.startPos;
            Int64 yWeight = (((y.objPosition.document * multi) + y.objPosition.paragraph) * multi + y.objPosition.startGroup) * multi + y.objPosition.startPos;
            return yWeight.CompareTo(xWeight);
        }


        private static List<annotation> readAnnotations(string fileName)
        {
            List<annotation> result = new List<annotation>();


            string line;
            string[] lineParts;
            annotation anno;

            StreamReader file = new StreamReader(fileName);
            //drop first header row
            file.ReadLine();
            while ((line = file.ReadLine()) != null)
            {
                lineParts = line.Split(';');
                anno = new annotation(); ;
                anno.RowId = Int32.Parse(lineParts[0]);
                anno.Text = lineParts[1];
                anno.Color = lineParts[2];
                anno.Position = lineParts[3];
                result.Add(anno);
            }

            file.Close();
            return result;
        }

        static int[] GetTextFromHtml(String html, String position)
        {
            var pos = Regex.Match(position, @"\/6\/(\d+)\!\/4\/(\d+)(.+)");
            if (!pos.Success)
            {
                Console.WriteLine("Position pattern did not match: " + position);
                return new int[] { -1 };
            }
            int paragraph = Int32.Parse(pos.Groups[2].Value);

            String fileContents = html;
            int filepos = 0;
            int carretPos;
            String foundParragraph = "";
            while ((carretPos = fileContents.IndexOf("<", filepos)) > -1)
            {
                if ((carretPos + 10) > fileContents.Length) break;
                String debugString = fileContents.Substring(carretPos, 10);
                filepos = carretPos + 1;
                if (Regex.Match(fileContents.Substring(filepos, 2), @"h\d").Success || (fileContents[filepos] == 'p'))
                {
                    //this is a paragraph start
                    paragraph -= 2;
                    if (paragraph == 0)
                    {
                        //this is the needed parragraph
                        var endPos = fileContents.IndexOf("</p>", carretPos);
                        foundParragraph = fileContents.Substring(carretPos, (endPos - carretPos));
                        break;
                    }
                }
            }
            if (foundParragraph == "")
            {
                //Console.WriteLine("Parragraph not found. " + position);
                return new int[] { -1 };
            }
            var charPos = Regex.Match(pos.Groups[3].Value, @"(.*),(.+),(.+)");
            if (!charPos.Success)
            {
                Console.WriteLine("Character position pattern did not match: " + position);
                return new int[] { -1 };
            }
            int startPos = getPosition(foundParragraph, charPos.Groups[1].Value + charPos.Groups[2].Value);
            if (startPos < 0)
            {
                //Console.WriteLine("Start position not found: " + position);
                return new int[] { -1 };
            }
            int endPosi = getPosition(foundParragraph, charPos.Groups[1].Value + charPos.Groups[3].Value);
            if (endPosi < 0)
            {
                //Console.WriteLine("End position not found: " + position);
                return new int[] { -1 };
            }
            return new int[] { carretPos + startPos, carretPos + endPosi };
        }

        private static int getPosition(string foundParragraph, string pos)
        {
            //returns the numeric position in string based on string pos
            //cut out the first tag belonging to the paragraph
            int parStart = foundParragraph.IndexOf(">") + 1;
            String par = foundParragraph.Substring(parStart);
            var parts = Regex.Matches(par, @"([^<]+)|(<.+?>.+?<.+?>)");
            //process the position "/5:439" 5th text part, position 439
            var posReg = Regex.Match(pos, @"\/(\d+):(\d+)");
            if (!posReg.Success)
            {
                Console.WriteLine("Position string mismatch: " + pos);
                return -1;
            }
            int posGroup = Int16.Parse(posReg.Groups[1].Value);
            int posPos = Int16.Parse(posReg.Groups[2].Value);
            //if the parts contain at least as many parts as needed return the position
            if (parts.Count >= posGroup)
            {
                if (parts[posGroup - 1].Length >= posPos)
                {
                    return (parStart + parts[posGroup - 1].Index + posPos);
                }
            }
            return -1;
        }
    }
}
