using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace DataBuildSync.Models {
    public static class XmlHandler {
        public static void CreateDatabase() {
            if (!File.Exists("Settings.xml")) {
                var doc = new XDocument(new XElement("body",

                    // Configuration
                    new XElement("Configuration", new XElement("ParallelTransfer", "false"), new XElement("LoggingLevel", "Standard"), new XElement("DefaultProjectFolder", @"C:\Projects"), new XElement("DefaultDestinationFolder", @"C:\Destination")),

                    // Representatives -> Rep
                    new XElement("Representatives", new XElement("Rep", new XElement("Initials", "JG"))),

                    // ProjectLinks -> Link
                    new XElement("ProjectLinks")));
                doc.Save("Settings.xml");
            }
        }

        public static Configuration GetConfig() {
            try {
                var doc = XDocument.Load("Settings.xml");

                var ele = doc.Descendants("Configuration").First();

                return new Configuration {
                    DefaultProjectFolder = ele.Descendants("DefaultProjectFolder").First().Value,
                    DefaultDestinationFolder = ele.Descendants("DefaultDestinationFolder").First().Value,
                    ParallelTransfer = ele.Descendants("ParallelTransfer").First().Value == "true",
                    LoggingLevel = ele.Descendants("LoggingLevel").First().Value
                };
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        public static void UpdateConfig(Configuration config) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var ele = doc.Descendants("Configuration").First();

                ele.Descendants("DefaultProjectFolder").First().Value = config.DefaultProjectFolder;
                ele.Descendants("DefaultDestinationFolder").First().Value = config.DefaultDestinationFolder;
                ele.Descendants("ParallelTransfer").First().Value = config.ParallelTransfer ? "true" : "false";
                ele.Descendants("LoggingLevel").First().Value = config.LoggingLevel;

                doc.Save("Settings.xml");
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static List<Rep> GetReps() {
            try {
                var doc = XDocument.Load("Settings.xml");

                var eles = doc.Descendants("Rep");
                var list = new List<Rep>();
                foreach (var ele in eles) {
                    list.Add(new Rep {Initial = ele.Descendants("Initials").First().Value});
                }

                return list;
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        public static Rep GetRep(string initial) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var ele = doc.Descendants("Rep").SingleOrDefault(d => d.Descendants("Initials").First().Value == initial);

                return new Rep {Initial = ele.Descendants("Initials").First().Value};
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        public static void CreateRep(Rep model) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var ele = doc.Descendants("Representatives").First();

                var newRep = new XElement("Rep", new XElement("Initials", model.Initial));

                ele.Add(newRep);

                doc.Save("Settings.xml");
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static void RemoveRep(Rep rep) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var repEle = doc.Descendants("Representatives").First();

                var singleOrDefault = repEle.Descendants("Rep").SingleOrDefault(d => d.Descendants("Initials").First().Value == rep.Initial);
                singleOrDefault?.Remove();

                var projectLinksEle = doc.Descendants("ProjectLinks");

                var repProjectLinks = projectLinksEle.Descendants("Link").Where(d => d.Descendants("RepInitials").First().Value == rep.Initial);

                foreach (var link in repProjectLinks) {
                    link.Remove();
                }

                doc.Save("Settings.xml");
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static List<ProjectLink> GetRepProjects(string initials) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var eles = doc.Descendants("Link").Where(d => d.Descendants("RepInitials").First().Value == initials);
                var list = new List<ProjectLink>();

                foreach (var ele in eles) {
                    list.Add(new ProjectLink {
                        Backup = ele.Descendants("Backup").First().Value == "true",
                        ProjectName = ele.Descendants("ProjectName").First().Value,
                        ProjectPath = ele.Descendants("ProjectPath").First().Value,
                        RepInitials = ele.Descendants("RepInitials").First().Value,
                        ProjectCode = ele.Descendants("ProjectCode").First().Value
                    });
                }

                return list;
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        public static void CreateProjectLink(ProjectLink model) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var ele = doc.Descendants("ProjectLinks").First();

                var newLink = new XElement("Link", new XElement("ProjectCode", model.ProjectCode), new XElement("RepInitials", model.RepInitials), new XElement("ProjectPath", model.ProjectPath), new XElement("ProjectName", model.ProjectName),
                    new XElement("Backup", model.Backup));

                ele.Add(newLink);

                doc.Save("Settings.xml");
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static void UpdateProjectLink(ProjectLink link) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var projectLinksEle = doc.Descendants("ProjectLinks").First();

                var singleOrDefault = projectLinksEle.Descendants("Link").SingleOrDefault(d => d.Descendants("RepInitials").First().Value == link.RepInitials && d.Descendants("ProjectName").First().Value == link.ProjectName);

                if (singleOrDefault != null) {
                    singleOrDefault.Descendants("Backup").First().Value = link.Backup.ToString().ToLower();

                    doc.Save("Settings.xml");
                }
                else {
                    MessageBox.Show("Could not find project");
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        public static void RemoveProjectLink(ProjectLink link) {
            try {
                var doc = XDocument.Load("Settings.xml");

                var projectLinksEle = doc.Descendants("ProjectLinks").First();

                var singleOrDefault = projectLinksEle.Descendants("Link").SingleOrDefault(d => d.Descendants("RepInitials").First().Value == link.RepInitials && d.Descendants("ProjectName").First().Value == link.ProjectName);
                singleOrDefault?.Remove();
                doc.Save("Settings.xml");
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }
    }
}