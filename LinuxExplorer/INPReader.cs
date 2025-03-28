using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LinuxExplorer
{
    //public enum ElementDOF
    //{
    //    None,
    //    B31 = 6,
    //    S4R = 6,
    //    S4 = 6,
    //    S3 = 6,
    //    S3R = 6,
    //    S4RS = 6,
    //    S4RSW = 6,
    //    SAX1 = 6,
    //    SAX2 = 6,
    //    SAX2T = 6,
    //    SC6R = 6,
    //    S8R = 6,
    //    S8RT = 6,
    //    STRI3 = 6,
    //    S4R5 = 6,
    //    STRI65 = 6,
    //    S8R5 = 6,
    //    S9R5 = 6,
    //    SAXA1n = 6,
    //    SAXA2n = 6,
    //    SC8R = 6,
    //    R3D4 = 3,
    //    C3D6 = 3,
    //    C3D8R = 3,
    //    C3D8 = 3,
    //    C3D8I = 3,
    //    C3D10 = 3,
    //    C3D10M = 3,
    //    C3D20 = 3,
    //    C3D20R = 3,
    //    CPS4R = 3,
    //    CAX4R = 3,
    //    CPS6M = 3,
    //    CAX6M = 3,
    //    C3D27R = 3,
    //    C3D27RH = 3,
    //}
    public class Part
    {
        public string Name { get; set; }
        public int NumNode { get; set; }
        public string ElementType { get; set; }
        public int NumDof { get; set; }
        public Part() { }
    }

    public class Instance
    {
        public string Name { get; set; }
        public Part Part { get; set; }
    }

    public class INPReader
    {
        public static readonly Dictionary<string, int> DOFMap = new Dictionary<string, int>()
        {
        { "B31", 6 },
        { "S4R", 6 },
        { "S4", 6 },
        { "S3", 6 },
        { "S3R", 6 },
        { "S4RS", 6 },
        { "S4RSW", 6 },
        { "SAX1", 6 },
        { "SAX2", 6 },
        { "SAX2T", 6 },
        { "SC6R", 6 },
        { "S8R", 6 },
        { "S8RT", 6 },
        { "STRI3", 6 },
        { "S4R5", 6 },
        { "STRI65", 6 },
        { "S8R5", 6 },
        { "S9R5", 6 },
        { "SAXA1n", 6 },
        { "SAXA2n", 6 },
        { "SC8R", 6 },
        { "R3D4", 3 },
        { "C3D6", 3 },
        { "C3D8R", 3 },
        { "C3D8", 3 },
        { "C3D8I", 3 },
        { "C3D10", 3 },
        { "C3D10M", 3 },
        { "C3D20", 3 },
        { "C3D20R", 3 },
        { "CPS4R", 3 },
        { "CAX4R", 3 },
        { "CPS6M", 3 },
        { "CAX6M", 3 },
        { "C3D27R", 3 },
        { "C3D27RH", 3 }
        };

        private Dictionary<string, Part> _part;
        public Dictionary<string, Part> Part { get => _part; set => _part = value; }

        private Dictionary<string, Instance> _instance;
        public Dictionary<string, Instance> Instance { get => _instance; set => _instance = value; }

        public INPReader(string content) 
        {
            //_part = new Dictionary<string, Part>();
            //_instance = new Dictionary<string, Instance>();

            //Regex partRegex = new Regex(@"\*Part, name=(?<PartName>""?[^""\n]+?""?)\s+\*Node\s+(?:\s*\d+,[^*\n]+\n)+\s*(?<LastNode>\d+),[^*\n]+\n\*Element, type=(?<ElementType>[\w\d]+)", RegexOptions.Compiled | RegexOptions.Singleline);
            //MatchCollection partMatches = partRegex.Matches(content);
            //foreach (Match match0 in partMatches)
            //{
            //    Part part = new Part
            //    {
            //        Name = match0.Groups["PartName"].Success ? match0.Groups["PartName"].Value.Trim('"') : "Unknown",
            //        NumNode = match0.Groups["LastNode"].Success ? int.Parse(match0.Groups["LastNode"].Value) : 0,
            //        ElementType = match0.Groups["ElementType"].Success ? match0.Groups["ElementType"].Value.Trim('"') : ""
            //    };
            //    if (DOFMap.TryGetValue(part.ElementType, out int dof))
            //    {
            //        part.NumDof = part.NumNode * dof;
            //    }
            //    else
            //    {
            //        part.NumDof = 0;
            //    }
            //    _part[part.Name] = part;
            //}

            //Regex instanceRegex = new Regex(@"\*Instance, name=""?(?<InstanceName>[^"",\n]+)""?, part=""?(?<PartName>[^"",\n]+)""?([\s\S]*?)\*End Instance", RegexOptions.Compiled);
            //MatchCollection instanceMatches = instanceRegex.Matches(content);
            //foreach (Match match1 in instanceMatches)
            //{
            //    //if (match1.Groups["InstanceName"].Value == "" || match1.Groups["PartName"].Value == "") continue;
            //    Instance instance = new Instance
            //    {
            //        Name = match1.Groups["InstanceName"].Value.Trim('"'),
            //        Part = _part[match1.Groups["PartName"].Value]
            //    };
            //    _instance.Add(instance.Name, instance);
            //}
            _part = new Dictionary<string, Part>();
            _instance = new Dictionary<string, Instance>();

            // Improved Part Regex
            Regex partRegex = new Regex(@"\*Part, name=""?(?<PartName>[^""\n]+)""?\s+\*Node\s+(?:\s*\d+,[^*\n]+\n)+\s*(?<LastNode>\d+),[^*\n]+\n\*Element, type=(?<ElementType>[\w\d]+)",
                RegexOptions.Compiled | RegexOptions.Singleline);

            MatchCollection partMatches = partRegex.Matches(content);
            foreach (Match match0 in partMatches)
            {
                string partName = match0.Groups["PartName"].Value.Trim('"');
                Part part = new Part
                {
                    Name = partName,
                    NumNode = match0.Groups["LastNode"].Success ? int.Parse(match0.Groups["LastNode"].Value) : 0,
                    ElementType = match0.Groups["ElementType"].Success ? match0.Groups["ElementType"].Value.Trim('"') : ""
                };
                if (DOFMap.TryGetValue(part.ElementType, out int dof))
                {
                    part.NumDof = part.NumNode * dof;
                }
                else
                {
                    part.NumDof = 0;
                }
                _part[partName] = part;
            }

            Regex instanceRegex = new Regex(@"\*Instance, name=""?(?<InstanceName>[^"",\n]+)""?,\s*part=""?(?<PartName>[^"",\n]+)""?\s*([\s\S]*?)\*End Instance", RegexOptions.Compiled | RegexOptions.Singleline);

            MatchCollection instanceMatches = instanceRegex.Matches(content);
            foreach (Match match1 in instanceMatches)
            {
                string instanceName = match1.Groups["InstanceName"].Value.Trim('"');
                string partName = match1.Groups["PartName"].Value.Trim('"');
                Console.WriteLine($"Processing Instance: {instanceName}, Part: {partName}");
                if (!_part.TryGetValue(partName, out Part part))
                {
                    Console.WriteLine($"Warning: Part '{partName}' not found for Instance '{instanceName}'");
                    continue;
                }
                Instance instance = new Instance
                {
                    Name = instanceName,
                    Part = part
                };
                _instance[instance.Name] = instance;
            }
        }
        
        public int GetTotalNode()
        {
            int totalNode = 0;
            foreach (var item in Instance)
            {
                totalNode += item.Value.Part.NumNode;
            }
            return totalNode;
        }

        public int GetTotalDof()
        {
            int totalNode = 0;
            foreach (var item in Instance)
            {
                totalNode += item.Value.Part.NumNode;
            }
            return totalNode;
        }
    }
}
