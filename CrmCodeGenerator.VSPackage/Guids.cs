// Guids.cs
// MUST match guids.h
using System;

namespace CrmPluginRegExt.VSPackage
{
    static class GuidList
    {
        public const string guidPluginRegExt_VSPackagePkgString = "d57cc223-384b-42cc-b5d0-814a71ce8d61";
        public const string guidPluginRegExt_VSPackageCmdSetString = "275315bd-3cf8-4fa5-b6fb-9c03e9699b82";
        public const string guidPluginRegExt_SimpleGenerator = "BB66ADDB-6AB5-4E29-B263-F918D86D1CC0";

        public static readonly Guid guidPluginRegExt_VSPackageCmdSet = new Guid(guidPluginRegExt_VSPackageCmdSetString);
    };
}