﻿using SourceControl.Ref;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskQueue.Providers;

namespace SourceControl.BuildServers
{
    public interface BuildServer
    {
        string Name { get; }
        string Description { get; }

        TItemModel GetParametersModel();
        void SetParameters(TItemModel parameters);

        BuildArtifacts GetArtifactsZip();
        bool CheckParameters(out string explanation);

        SCMRevision GetVersion();
    }
}
