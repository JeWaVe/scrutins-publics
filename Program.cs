using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ScrutinsPublics
{
    class Program
    {
        const string numero_scrutin = "2707";
        const string urlFormat = "http://www2.assemblee-nationale.fr/scrutins/detail/(legislature)/15/(num)/{0}";

        class Depute
        {
            private string _prenom;
            private string _nom;
            private string _groupe;
            public string Prenom
            {
                get { return _prenom; }
                set { _prenom = Decode(value).Trim(); }
            }
            public string Nom
            {
                get { return _nom; }
                set { _nom = Decode(value).Trim(); }
            }
            public string Groupe
            {
                get { return _groupe; }
                set { _groupe = Decode(value).Trim(); }
            }
        }

        class GroupResults
        {
            public string Nom { get; set; }
            public List<Depute> Pour { get; set; }
            public List<Depute> Contre { get; set; }
            public List<Depute> NonVotants { get; set; }
            public List<Depute> Abstention { get; set; }

            public GroupResults(string name)
            {
                Nom = name;
                Pour = new List<Depute>();
                Contre = new List<Depute>();
                NonVotants = new List<Depute>();
                Abstention = new List<Depute>();
            }

        }

        static void Main(string[] args)
        {
            string content = new WebClient().DownloadString(String.Format(urlFormat, numero_scrutin));
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            var resultsByGroup = htmlDoc.DocumentNode.SelectNodes("//div").Where(node => node.HasClass("TTgroupe") && node.HasClass("topmargin-lg"));
            Dictionary<string, GroupResults> results = new Dictionary<string, GroupResults>();

            foreach(var group in resultsByGroup)
            {
                string groupName = Decode(group.ChildNodes.Where(c => c.HasClass("agroupe")).First().GetAttributeValue("name", "unknown")).Replace("Groupe de la ", "").Replace("Groupe du ", "").Replace("Groupe ", "");
                if(!results.ContainsKey(groupName))
                {
                    var groupe = new GroupResults(groupName);
                    results.Add(groupName, groupe);
                    groupe.Contre = ExtractDeputes(group, groupName, "Contre");
                    groupe.NonVotants = ExtractDeputes(group, groupName, "Non-votants");
                    groupe.Pour = ExtractDeputes(group, groupName, "Pour");
                    groupe.Abstention = ExtractDeputes(group, groupName, "Abstention");
                }
            }

            string json = JsonConvert.SerializeObject(results.Values);
        }

        private static List<Depute> ExtractDeputes(HtmlNode group, string groupName, string status)
        {
            List<Depute> deputes = new List<Depute>();
            var subResults = group.ChildNodes.Where(c => c.HasClass(status) && c.HasClass("clearfix"));
            if (subResults.Any())
            {
                var list = subResults.First().Descendants("ul").First();
                var items = list.Descendants("li");
                foreach (var depute in items)
                {
                    var prenom = Decode(depute.FirstChild.InnerText);
                    var nom = Decode(depute.Descendants("b").First().InnerText);
                    deputes.Add(new Depute { Prenom = prenom, Nom = nom, Groupe = groupName });
                }
                if (!items.Any())
                {
                    foreach (var bold in list.Descendants("b"))
                    {
                        var prenom = Decode(bold.PreviousSibling.InnerText.Split(new string[] { "&nbsp;" }, StringSplitOptions.RemoveEmptyEntries).Last());
                        var nom = Decode(bold.InnerText);
                        deputes.Add(new Depute { Groupe = groupName, Nom = nom, Prenom = prenom });
                    }
                }
            }

            return deputes;
        }

        static string Decode(string input)
        {
            return input.Replace("Ã©", "é").Replace("&nbsp;", "");
        }
    }
}
