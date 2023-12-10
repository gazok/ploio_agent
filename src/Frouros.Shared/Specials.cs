//    Copyright 2023 Yeong-won Seo
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace Frouros.Shared;

public static class Specials
{
    static Specials()
    {
        if (OperatingSystem.IsWindows())
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

            PipePath   = "frouros";
            ConfigPath = Path.Combine(root, "Frouros/appsettings.json");
            ModulePath = Path.Combine(root, "Frouros/Modules");
        }
        else
        {
            PipePath   = "/opt/frouros.d/hc.sock";
            ConfigPath = "/opt/frouros.d/appsettings.json";
            ModulePath = "/opt/frouros.d/mods";
        }
    }

    public static string ConfigPath { get; }
    public static string PipePath   { get; }
    public static string ModulePath { get; }

    public static Uri CentralServer { get; } = new("http://plo.io/agents");
}