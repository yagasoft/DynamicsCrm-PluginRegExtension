#region File header

// Project / File: CrmPluginRegExt.VSPackage / Helper.cs
//          Authors / Contributors:
//                      Ahmed el-Sawalhy (LINK Development - MBS)
//        Created: 2015 / 06 / 12
//       Modified: 2015 / 06 / 19

#endregion

#region Imports

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Xml;
using CrmPluginRegExt.VSPackage.Model;
using Microsoft.Xrm.Sdk.Metadata;

#endregion

namespace CrmPluginRegExt.VSPackage.Helpers
{
	public class Naming
	{
		public static string Clean(string p)
		{
			var result = "";

			if (string.IsNullOrEmpty(p))
			{
				return result;
			}

			p = p.Trim();
			p = Normalize(p);

			if (!string.IsNullOrEmpty(p))
			{
				var sb = new StringBuilder();
				if (!char.IsLetter(p[0]))
				{
					sb.Append("_");
				}

				foreach (var character in p)
				{
					if ((char.IsDigit(character) || char.IsLetter(character) || character == '_') &&
					    !string.IsNullOrEmpty(character.ToString()))
					{
						sb.Append(character);
					}
				}

				result = sb.ToString();
			}

			result = ReplaceKeywords(result);

			var arabicChars = new[]
			                  {
				                  "ذ", "ض", "ص", "ث", "ق", "ف", "غ", "ع", "ه", "خ", "ح", "ج"
				                  , "ش", "س", "ي", "ب", "ل", "ا", "ت", "ن", "م", "ك", "ط"
				                  , "ئ", "ء", "ؤ", "ر", "لا", "ى", "ة", "و", "ز", "ظ"
				                  , "لإ", "إ", "أ", "لأ", "لآ", "آ"
			                  };
			var correspondingEnglishChars = new[]
			                                {
				                                "z", "d", "s", "s", "k", "f", "gh", "a", "h", "kh", "h", "g"
				                                , "sh", "s", "y", "b", "l", "a", "t", "n", "m", "k", "t"
				                                , "ea", "a", "oa", "r", "la", "y", "t", "o", "th", "z"
				                                , "la", "e", "a", "la", "la", "a"
			                                };

			result = Regex.Replace(result, "[^A-Za-z0-9_]"
				, match =>
				  {
					  return match.Success
						         ? match.Value.Select(character =>
						                              {
							                              var index = Array.IndexOf(arabicChars, character.ToString());
							                              return index < 0 ? "_" : correspondingEnglishChars[index];
						                              })
							           .Aggregate((char1, char2) => char1 + char2)
						         : "";
				  });

			result = Regex.Replace(result, "[^A-Za-z0-9_]", "_");

			if (!char.IsLetter(result[0]) && result[0] != '_')
			{
				result = "_" + result;
			}

			//if (char.IsLower(result[0]))
			//{
			//	result = result.Remove(0, 1).Insert(0, result.Substring(0, 1).ToUpper());
			//}

			return result;
		}

		private static string Normalize(string regularString)
		{
			var normalizedString = regularString.Normalize(NormalizationForm.FormD);

			var sb = new StringBuilder(normalizedString);

			for (var i = 0; i < sb.Length; i++)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(sb[i]) == UnicodeCategory.NonSpacingMark)
				{
					sb.Remove(i, 1);
				}
			}
			regularString = sb.ToString();

			return regularString.Replace("æ", "");
		}


		private static string ReplaceKeywords(string p)
		{
			if (p.Equals("public", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("private", StringComparison.InvariantCultureIgnoreCase)
			    // || p.Equals("event", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("single", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("new", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("partial", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("to", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("error", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("readonly", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("case", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("object", StringComparison.InvariantCultureIgnoreCase)
			    || p.Equals("global", StringComparison.InvariantCultureIgnoreCase)
				// || p.Equals("namespace", StringComparison.InvariantCultureIgnoreCase)
				// || p.Equals("abstract", StringComparison.InvariantCultureIgnoreCase)
				)
			{
				return "__" + p;
			}

			return p;
		}


		public static string CapitalizeWord(string p)
		{
			if (string.IsNullOrWhiteSpace(p))
			{
				return "";
			}

			return p.Substring(0, 1).ToUpper() + p.Substring(1);
		}

		private static string DecapitalizeWord(string p)
		{
			if (string.IsNullOrWhiteSpace(p))
			{
				return "";
			}

			return p.Substring(0, 1).ToLower() + p.Substring(1);
		}

		public static string Capitalize(string p, bool capitalizeFirstWord)
		{
			var parts = p.Split(' ', '_');

			for (var i = 0; i < parts.Count(); i++)
			{
				parts[i] = i != 0 || capitalizeFirstWord ? CapitalizeWord(parts[i]) : DecapitalizeWord(parts[i]);
			}

			return string.Join("_", parts);
		}

		public static string GetProperEntityName(string entityName)
		{
			return Clean(Capitalize(entityName, true));
		}

		public static string GetProperHybridName(string displayName, string logicalName)
		{
			if (logicalName.Contains("_"))
			{
				Console.WriteLine(displayName + " " + logicalName);
				return displayName;
			}
			else
			{
				return Clean(Capitalize(displayName, true));
			}
		}

		public static string GetProperHybridFieldName(string displayName, CrmPropertyAttribute attribute)
		{
			if (attribute != null && attribute.LogicalName.Contains("_"))
			{
				return attribute.LogicalName;
			}
			else
			{
				return displayName;
			}
		}

		public static string GetProperVariableName(AttributeMetadata attribute)
		{
			// Normally we want to use the SchemaName as it has the capitalized names (Which is what CrmSvcUtil.exe does).  
			// HOWEVER, If you look at the 'annual' attributes on the annualfiscalcalendar you see it has schema name of Period1  
			// So if the logicalname & schema name don't match use the logical name and try to capitalize it 
			// EXCEPT,  when it's RequiredAttendees/From/To/Cc/Bcc/SecondHalf/FirstHalf  (i have no idea how CrmSvcUtil knows to make those upper case)
			if (attribute.LogicalName == "requiredattendees")
			{
				return "RequiredAttendees";
			}
			if (attribute.LogicalName == "from")
			{
				return "From";
			}
			if (attribute.LogicalName == "to")
			{
				return "To";
			}
			if (attribute.LogicalName == "cc")
			{
				return "Cc";
			}
			if (attribute.LogicalName == "bcc")
			{
				return "Bcc";
			}
			if (attribute.LogicalName == "firsthalf")
			{
				return "FirstHalf";
			}
			if (attribute.LogicalName == "secondhalf")
			{
				return "SecondHalf";
			}
			if (attribute.LogicalName == "firsthalf_base")
			{
				return "FirstHalf_Base";
			}
			if (attribute.LogicalName == "secondhalf_base")
			{
				return "SecondHalf_Base";
			}
			if (attribute.LogicalName == "attributes")
			{
				return "Attributes1";
			}

			if (attribute.LogicalName.Equals(attribute.SchemaName, StringComparison.InvariantCultureIgnoreCase))
			{
				return Clean(attribute.SchemaName);
			}

			return Clean(Capitalize(attribute.LogicalName, true));
		}

		public static string GetProperVariableName(string p)
		{
			if (string.IsNullOrWhiteSpace(p))
			{
				return "Empty";
			}
			if (p == "Closed (deprecated)") //Invoice
			{
				return "Closed";
			}
			//return Clean(Capitalize(p, true));
			return Clean(p);
		}

		public static string GetPluralName(string p)
		{
			if (p.EndsWith("y"))
			{
				return p.Substring(0, p.Length - 1) + "ies";
			}

			if (p.EndsWith("s"))
			{
				return p;
			}

			return p + "s";
		}

		public static string GetEntityPropertyPrivateName(string p)
		{
			return "_" + Clean(Capitalize(p, false));
		}

		public static string XmlEscape(string unescaped)
		{
			var doc = new XmlDocument();
			XmlNode node = doc.CreateElement("root");
			node.InnerText = unescaped;
			return node.InnerXml;
		}
	}
	/// <summary>
	///     Reference Article http://www.codeproject.com/KB/tips/SerializedObjectCloner.aspx
	///     Provides a method for performing a deep copy of an object.
	///     Binary Serialization is used to perform the copy.
	/// </summary>
	public static class ObjectCopier
	{
		/// <summary>
		///     Perform a deep Copy of the object.
		/// </summary>
		/// <typeparam name="T">The type of object being copied.</typeparam>
		/// <param name="source">The object instance to copy.</param>
		/// <returns>The copied object.</returns>
		public static T Clone<T>(T source)
		{
			if (!typeof(T).IsSerializable)
			{
				throw new ArgumentException("The type must be serializable.", "source");
			}

			// Don't serialize a null object, simply return the default for that object
			if (ReferenceEquals(source, null))
			{
				return default(T);
			}

			IFormatter formatter = new BinaryFormatter { Binder = new Binder() };
			Stream stream = new MemoryStream();
			using (stream)
			{
				formatter.Serialize(stream, source);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)formatter.Deserialize(stream);
			}
		}
	}

	public class AssemblyHelpers
	{
		// credit: http://blog.slaks.net/2013-12-25/redirecting-assembly-loads-at-runtime/
		public static void RedirectAssembly(string shortName, Version targetVersion, string publicKeyToken)
		{
			Assembly Handler(object sender, ResolveEventArgs args)
			{
				// Use latest strong name & version when trying to load SDK assemblies
				var requestedAssembly = new AssemblyName(args.Name);

				if (requestedAssembly.Name != shortName && !requestedAssembly.FullName.Contains(shortName + ","))
				{
					return null;
				}

				Debug.WriteLine("Redirecting assembly load of " + args.Name + ",\tloaded by " + (args.RequestingAssembly == null ? "(unknown)" : args.RequestingAssembly.FullName));

				requestedAssembly.Version = targetVersion;
				requestedAssembly.SetPublicKeyToken(new AssemblyName("x, PublicKeyToken=" + publicKeyToken).GetPublicKeyToken());
				requestedAssembly.CultureInfo = CultureInfo.InvariantCulture;

				AppDomain.CurrentDomain.AssemblyResolve -= Handler;

				var loadedAssembly = Assembly.Load(requestedAssembly);
				return loadedAssembly;
			}

			AppDomain.CurrentDomain.AssemblyResolve += Handler;
		}
	}

	public class Binder : SerializationBinder
	{
		// credit: http://stackoverflow.com/a/18856352/1919456
		public override Type BindToType(string assemblyName, string typeName)
		{
			var currentAssemblyInfo = Assembly.GetExecutingAssembly().FullName;

			var currentAssemblyName = currentAssemblyInfo.Split(',')[0];

			if (assemblyName.StartsWith(currentAssemblyName))
			{
				assemblyName = currentAssemblyInfo;
			}

			// backward compatibility
			if (assemblyName.Contains("6.0.0.0") && assemblyName.Contains("Microsoft.Xrm.Sdk"))
			{
				assemblyName = assemblyName.Split(',')[0];
			}

			return Type.GetType($"{typeName}, {assemblyName}");
		}
	}
}
