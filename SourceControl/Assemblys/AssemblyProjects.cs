﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceControl.BuildServers
{
    public class AssemblyProjects
    {
        public string DiretoryContainer { get; set; }
        public List<SourceController> hostedProjects;
        public Register artifacts;

        public AssemblyProjects(string path)
        {
            DiretoryContainer = path;
            CheckDirectory();

            hostedProjects = new List<SourceController>();
            artifacts = new Register();
        }
        public void Add(string name, string buildServerType, Dictionary<string, object> parameters)
        {
            BuildServers.IBuildServer bs = artifacts.GetNewInstance(buildServerType);
            bs.SetParameters(parameters);

            hostedProjects.Add(new SourceController(DiretoryContainer, name, bs));
        }
        public IEnumerable<SourceController> TakeLoadTime()
        {
            foreach (SourceController p in hostedProjects.OrderBy(o => o.Versions.LastPackagedDate))
            {
                yield return p;
            }
        }
        private void CheckDirectory()
        {
            if (!System.IO.Directory.Exists(DiretoryContainer))
            {
                System.IO.Directory.CreateDirectory(DiretoryContainer);
            }
        }
    }
}
