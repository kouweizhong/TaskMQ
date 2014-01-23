﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SourceControl.BuildServers
{
    public class BuildArtifact
    {
        public byte[] Data;

        public string Name { get; set; }
        public string FileName { get; set; }
        public bool IsAssembly { get; set; }
        public string Version { get; set; }
        public string HashCode { get; set; }

        public static BuildArtifact Get(byte[] data)
        {
            BuildArtifact art = new BuildArtifact();
            using (MemoryStream ms = new MemoryStream(data))
            {
                try
                {
                    Mono.Cecil.AssemblyDefinition def = Mono.Cecil.AssemblyDefinition.ReadAssembly(ms);
                    Mono.Cecil.AssemblyNameReference defn = Mono.Cecil.AssemblyNameDefinition.Parse(def.FullName);

                    art.Version = defn.Version.ToString();
                    art.Name = defn.Name;
                    art.IsAssembly = true;
                }
                catch
                {

                }
            }

            art.Data = data;

            return art;
        }
    }
    /// <summary>
    /// built assemblies with meta information and with artifacts and dependencies
    /// </summary>
    public class BuildArtifacts
    {
        public BuildArtifacts() { Artefacts = new List<BuildArtifact>(); }
        public string VersionTag { get; set; }
        public string AssemblyArtefactName { get; set; }
        public DateTime AddedAt { get; set; }

        public List<BuildArtifact> Artefacts;

        public void AddArtefact(string name, byte[] relatedFile)
        {
            BuildArtifact dllInfo = BuildArtifact.Get(relatedFile);
            dllInfo.FileName = name;
            if (!dllInfo.IsAssembly)
                dllInfo.Name = name;
            Artefacts.Add(dllInfo);
        }

        public static BuildArtifacts FromZipArchive(string AssemblyArtefactName, string versionTag, byte[] data)
        {
            BuildArtifacts result = new BuildArtifacts();
            FileContentArchive.ZipStream zipArch = new FileContentArchive.ZipStream(data);
            FileContentArchive.FileStorageEntry[] entrys = zipArch.GetAllEntrys();

            bool artefactAssemblyFound = false;
            for (int i = 0; i < entrys.Length; i++)
            {
                if (entrys[i].IsDir)
                    continue;

                if (entrys[i].Location == AssemblyArtefactName)
                    artefactAssemblyFound = true;

                result.AddArtefact(entrys[i].Location, zipArch.GetContentRaw(entrys[i].Location));
            }

            zipArch.Close();

            result.AssemblyArtefactName = AssemblyArtefactName;

            result.AddedAt = DateTime.UtcNow;
            result.VersionTag = versionTag;

            if (!artefactAssemblyFound)
                Console.WriteLine(" from build artifact not found assembly: {0}", AssemblyArtefactName);

            return result;
        }
        public static BuildArtifacts FromDirectory(string AssemblyArtefactAbsPath, string versionTag)
        {
            BuildArtifacts result = new BuildArtifacts();
            string Dir = System.IO.Path.GetDirectoryName(AssemblyArtefactAbsPath);
            string assemblyAbsSym = pathWithoutExtension(AssemblyArtefactAbsPath);

            if (File.Exists(assemblyAbsSym + ".pdb"))
            {
                assemblyAbsSym = assemblyAbsSym + ".pdb";
            }
            else assemblyAbsSym = null;
            string[] Files = System.IO.Directory.GetFiles(Dir);

            bool artefactAssemblyFound = false;
            for (int i = 0; i < Files.Length; i++)
            {
                if (Files[i] == AssemblyArtefactAbsPath)
                    artefactAssemblyFound = true;

                result.AddArtefact(System.IO.Path.GetFileName(Files[i]), File.ReadAllBytes(Files[i]));
            }

            result.AssemblyArtefactName = System.IO.Path.GetFileName(AssemblyArtefactAbsPath);

            result.AddedAt = DateTime.UtcNow;
            result.VersionTag = versionTag;

            if (!artefactAssemblyFound)
                Console.WriteLine(" from build artifact not found assembly: {0}", AssemblyArtefactAbsPath);

            return result;
        }

        static string pathWithoutExtension(string path)
        {
            int index = path.LastIndexOf('.');
            if (index >= 0)
            {
                return path.Remove(index, path.Length - index);
            }
            return path;
        }
    }

}
