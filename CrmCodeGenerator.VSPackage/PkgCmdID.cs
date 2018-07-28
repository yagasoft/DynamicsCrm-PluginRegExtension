// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace CrmPluginRegExt.VSPackage
{
    static class PkgCmdIDList
    {
        public const uint cmdidPluginRegExt = 0x960;
		public const uint cmdidRegisterModifyPlugin = 0x970;
		public const uint cmdidUpdatePlugin = 0x980;
		public const uint cmdidMultiRegisterModifyPlugin = 0x940;
		public const uint cmdidMultiUpdatePlugin = 0x950;
		public const uint cmdidCopyPluginSettings = 0x930;
		public const uint cmdidDeletePlugin = 0x990;
    };
}