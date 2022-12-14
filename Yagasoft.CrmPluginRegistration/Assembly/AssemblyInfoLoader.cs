#region Imports

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace CrmPluginRegExt.AssemblyInfoLoader
{
	public class AssemblyInfoLoader
	{
		public AssemblyInfo GetAssemblyInfo(string assemblyPath, string loaderAssemblyPath,
			params string[] fullBaseClassName)
		{
			AssemblyInfo assemblyInfo = null;

			var setup =
				new AppDomainSetup
				{
					ApplicationBase = Path.GetDirectoryName(assemblyPath),
					ShadowCopyFiles = "true"
				};

			// create a temporary app domain
			var tempDomain = AppDomain.CreateDomain("TempDomain", null, setup);

			// create proxy instance in temporary domain
			var asmLoader = (AssemblyLoader)tempDomain.CreateInstanceFromAndUnwrap(
				loaderAssemblyPath, typeof(AssemblyLoader).FullName);

			// load assembly in other domain
			assemblyInfo = asmLoader.LoadAssemblyInfo(assemblyPath, fullBaseClassName);

			// unload temporary domain and free assembly resources
			AppDomain.Unload(tempDomain);

			return assemblyInfo;
		}
	}

	[Serializable]
	public class AssemblyInfo
	{
		public string Name { get; set; }
		public string Version { get; set; }
		public string RuntimeVersion { get; set; }
		public string Platform { get; set; }
		public CultureInfo CultureInfo { get; set; }
		public byte[] PublicKeyToken { get; set; }
		public string PublicKeyTokenString { get; set; }
		public ClassInfo[] Classes { get; set; }

		public AssemblyInfo()
		{ }

		public AssemblyInfo(AssemblyName assemblyName)
		{
			Name = assemblyName.Name;
			Version = assemblyName.Version.ToString();
			RuntimeVersion = "";
			Platform = assemblyName.ProcessorArchitecture.ToString();
			CultureInfo = assemblyName.CultureInfo;

			PublicKeyToken = assemblyName.GetPublicKeyToken();

			if (PublicKeyToken == null || PublicKeyToken.Length == 0)
			{
				PublicKeyTokenString = null;
			}
			else
			{
				PublicKeyTokenString = string.Join(string.Empty, PublicKeyToken.Select(b => b.ToString("X2")));
			}
		}

		public AssemblyInfo(Assembly assembly, params string[] classNames)
			: this(assembly.GetName())
		{
			RuntimeVersion = assembly.ImageRuntimeVersion;
			var types = assembly.GetTypes();
			Classes = classNames.SelectMany(c => types
				.Where(type => InheritsFrom(type, c) && !type.IsAbstract && type.IsPublic)
				.Select(type => new ClassInfo(c, type.FullName))).ToArray();
		}

		// credit: http://stackoverflow.com/a/18375526/1919456
		public static bool InheritsFrom(Type type, string baseType)
		{
			while (true)
			{
				// null does not have base type
				if (type == null)
				{
					return false;
				}

				// only interface can have null base type
				if (baseType == null)
				{
					return type.IsInterface;
				}

				// check base type
				if ((type.BaseType != null && type.BaseType.FullName == baseType)
					|| (type.GetInterfaces().Any(interfaceType => interfaceType.FullName == baseType)
						|| type.GetInterfaces().Any(interfaceType => InheritsFrom(interfaceType, baseType))))
				{
					return true;
				}

				type = type.BaseType;
			}
		}
	}

	[Serializable]
	public class ClassInfo
	{
		public string BaseType { get; set; }
		public string Type { get; set; }

		public ClassInfo(string baseType, string type)
		{
			BaseType = baseType;
			Type = type;
		}
	}

	public class AssemblyLoader : MarshalByRefObject
	{
		public AssemblyInfo LoadAssemblyInfo(string assemblyPath, string[] fullBaseClassName)
		{
			AppDomain.CurrentDomain.AssemblyResolve += Domain_AssemblyResolve;

			var assembly = Assembly.LoadFrom(assemblyPath);
			var info = new AssemblyInfo(assembly, fullBaseClassName);

			AppDomain.CurrentDomain.AssemblyResolve -= Domain_AssemblyResolve;

			return info;
		}

		public static Assembly Domain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			try
			{
				var assembly = Assembly.Load(args.Name);

				if (assembly != null)
				{
					return assembly;
				}
			}
			catch
			{
				// ignore load error
			}

			// *** Try to load by filename - split out the filename of the full assembly name
			// *** and append the base path of the original assembly (ie. look in the same dir)
			// *** NOTE: this doesn't account for special search paths but then that never
			//           worked before either.
			var parts = args.Name.Split(',');
			var file = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + parts[0].Trim() + ".dll";

			return Assembly.LoadFrom(file);
		}
	}
}
