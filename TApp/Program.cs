﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace TApp
{
    class Program
    {
        //netsh http add urlacl url=http://+:82/ user=User
        static void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo("TApp.exe") { Verb = "runas" };
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        class zModel : TaskQueue.Providers.TaskMessage
        {
            public zModel() : base("z") { }
            public string SomeProperty { get; set; }
        }
        static void Main(string[] args)
        {
            var prefix = QueueService.ModProducer.ListeningOn;
            var username = Environment.GetEnvironmentVariable("USERNAME");
            var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
            Console.WriteLine("  netsh http add urlacl url={0} user={1}\\{2} listen=yes",
                    prefix, userdomain, username);


            TaskQueue.QueueItemModel tm = new TaskQueue.QueueItemModel(typeof(zModel));
            MSSQLQueue.SqlTable t = new MSSQLQueue.SqlTable(tm, "Z");
            string rst = MSSQLQueue.SqlScript.ForTableGen(t);
            //
            TaskBroker.Broker b = new TaskBroker.Broker();

            //
            b.RegistarateChannel(new TaskBroker.MessageType()
                {
                    QueueName = "InMemoryQueue",
                    Collection = "Z",
                    UniqueName = "z",
                    Model = new TaskQueue.QueueItemModel(typeof(zModel))
                });
            //
            TaskBroker.ModMod m = new TaskBroker.ModMod()
            {
                InitialiseEntry = QueueService.ModProducer.Initialise,
                ExitEntry = QueueService.ModProducer.Exit,
                ModAssembly = typeof(QueueService.ModProducer).Assembly,
            };
            b.RegistrateModule(m);

            Console.Read();
        }
    }
}
