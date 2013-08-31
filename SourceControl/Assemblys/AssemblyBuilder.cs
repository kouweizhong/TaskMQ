﻿using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SourceControl.Assemblys
{
    public class BuildResultFile
    {
        public string Name;
        public byte[] Data;
    }
    public class AssemblyBuilder
    {
        public class bLogger : Microsoft.Build.Framework.ILogger, IDisposable
        {
            StringBuilder log = new StringBuilder();
            public void Initialize(Microsoft.Build.Framework.IEventSource eventSource)
            {
                //eventSource.AnyEventRaised += eventSource_AnyEventRaised;
                eventSource.ErrorRaised += eventSource_ErrorRaised;
                eventSource.MessageRaised += eventSource_MessageRaised;
                eventSource.BuildFinished += eventSource_BuildFinished;
            }

            void eventSource_BuildFinished(object sender, Microsoft.Build.Framework.BuildFinishedEventArgs e)
            {
                log.AppendFormat("[{0}] {1}: {2} \r\n", e.Timestamp, e.SenderName, e.Message);
            }

            void eventSource_MessageRaised(object sender, Microsoft.Build.Framework.BuildMessageEventArgs e)
            {
                log.AppendFormat("[{0}] {1}: {2} \r\n", e.Timestamp, e.SenderName, e.Message);
                //throw new NotImplementedException();
            }

            void eventSource_ErrorRaised(object sender, Microsoft.Build.Framework.BuildErrorEventArgs e)
            {
                Console.WriteLine("\t[{0}] {1}: {2} in {3} at {4}", e.Timestamp, e.SenderName, e.Message, e.File, e.LineNumber);
                log.AppendFormat("\t[{0}] {1}: {2} in {3} at {4}", e.Timestamp, e.SenderName, e.Message, e.File, e.LineNumber);
            }

            void eventSource_AnyEventRaised(object sender, Microsoft.Build.Framework.BuildEventArgs e)
            {
                log.AppendFormat("[{0}] {1}: {2} \r\n", e.Timestamp, e.SenderName, e.Message);
                //Console.WriteLine("el: [{0}] {1}: {2}", e.Timestamp, e.SenderName, e.Message);
            }

            public string Parameters
            {
                get
                {
                    return "";
                }
                set
                {
                }
            }

            public void Shutdown()
            {
            }

            public Microsoft.Build.Framework.LoggerVerbosity Verbosity
            {
                get
                {
                    return Microsoft.Build.Framework.LoggerVerbosity.Minimal;
                }
                set
                {
                }
            }
            public string Result()
            {
                return log.ToString();
            }

            public void Dispose()
            {
                log = null;
            }
        }

        public string ProjectLocation { get; set; }
        public string Log;
        public string BuildResultDll;
        public string BuildResultSymbols;
        public string[] BuildResultAssets;

        public AssemblyBuilder(string projectPath)
        {
            ProjectLocation = projectPath;// @"C:\Users\USER\test\EmailConsumer\EmailConsumer.csproj";
        }
        public bool BuildProject()
        {
            Log = "";
            bool buildResultOK = false;
            BuildResultDll = BuildResultSymbols = "";
            BuildResultAssets = null;
            try
            {
                using (bLogger logger = new bLogger())
                {
                    Project p = new Project(ProjectLocation);
                    string path = p.GetPropertyValue("TargetPath");
                    string pdb = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + ".pdb");

                    Console.WriteLine("building project: {0}", ProjectLocation);

                    buildResultOK = p.Build(logger);
                    Log = logger.Result();

                    if (buildResultOK)
                    {
                        BuildResultDll = path;
                        BuildResultSymbols = pdb;
                        List<string> files = new List<string>();
                        foreach (string F in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(path)))// not recursive! not now...
                        {
                            if (F != BuildResultDll && F != BuildResultSymbols)
                            {
                                files.Add(F);
                            }
                        }
                        BuildResultAssets = files.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Log += string.Format("major exception while build: {0}", e.Message);
            }
            return buildResultOK;
        }
    }
}