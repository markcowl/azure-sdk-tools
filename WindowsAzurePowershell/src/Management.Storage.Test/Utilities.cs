// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------
//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Management.ServiceManagement.Test.Properties;

namespace Microsoft.WindowsAzure.Management.Storage.Test
{
    public class Utilities 
    {
        public const string CopyAzureStorageBlobCmdletName = "Copy-AzureStorageBlob";

        public static string GetUniqueShortName(string prefix = "", int length = 6, string suffix = "")
        {
            return string.Format("{0}{1}{2}", prefix, Guid.NewGuid().ToString("N").Substring(0, length), suffix);
        }
    }
}
