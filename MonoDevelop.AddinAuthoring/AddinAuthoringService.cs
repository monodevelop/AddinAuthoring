// AddinAuthoringService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using Mono.Addins;
using Mono.Addins.Description;
using Mono.Addins.Setup;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Xml.Formatting;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.AddinAuthoring
{
	public static class AddinAuthoringService
	{
		static AddinAuthoringServiceConfig config;
		static string configFile;
		
		public static event EventHandler<RegistryEventArgs> RegistryChanged;
		
		static AddinAuthoringService ()
		{
			if (IdeApp.IsInitialized) {
				//IdeApp.ProjectOperations.EndBuild += OnEndBuild;
			}
			
			configFile = Path.Combine (PropertyService.Locations.Config, "AddinAuthoring.config");
			if (File.Exists (configFile)) {
				try {
					XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
					StreamReader sr = new StreamReader (configFile);
					using (sr) {
						config = (AddinAuthoringServiceConfig) ser.Deserialize (new XmlTextReader (sr), typeof(AddinAuthoringServiceConfig));
					}
				}
				catch (Exception ex) {
					LoggingService.LogError ("Could not load add-in authoring service configuration", ex);
				}
			}
			if (config == null)
				config = new AddinAuthoringServiceConfig ();
		}

		static Document browserDocument;
		
		public static void ShowExtensionModelBrowser ()
		{
			if (browserDocument != null)
				browserDocument.Select ();
			else {
				ExtensionModelBrowser browser = new ExtensionModelBrowser ();
				browserDocument = IdeApp.Workbench.OpenDocument (browser, true);
				browserDocument.Closed += delegate {
					browserDocument = null;
				};
			}
		}
		
		static void SaveConfig ()
		{
			try {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				StreamWriter sw = new StreamWriter (configFile);
				using (sw) {
					ser.Serialize (new XmlTextWriter (sw), config, typeof(AddinAuthoringServiceConfig));
				}
			}
			catch (Exception ex) {
				LoggingService.LogError ("Could not save add-in authoring service configuration", ex);
			}
		}
		
		internal static void Init ()
		{
			// Do nothing. Will be initialized in the static constructor.
		}
		
		static void OnEndBuild (object s, BuildEventArgs args)
		{
			if (args.Success && IdeApp.Workspace.IsOpen) {
				Dictionary<string, AddinRegistry> regs = new Dictionary<string, AddinRegistry> ();
				foreach (DotNetProject p in IdeApp.Workspace.GetAllSolutionItems<DotNetProject> ()) {
					AddinData data = AddinData.GetAddinData (p);
					if (data != null) {
						if (!regs.ContainsKey (data.AddinRegistry.RegistryPath))
							regs [data.AddinRegistry.RegistryPath] = data.AddinRegistry;
					}
				}
				if (regs.Count > 0) {
					args.ProgressMonitor.BeginTask (AddinManager.CurrentLocalizer.GetString ("Updating add-in registry"), regs.Count);
					foreach (AddinRegistry reg in regs.Values) {
						reg.Update (new ProgressStatusMonitor (args.ProgressMonitor, 2));
						args.ProgressMonitor.Step (1);
					}
					args.ProgressMonitor.EndTask ();
				}
			}
		}
		
		public static string GetRegistryName (string regPath)
		{
			foreach (RegistryInfo node in GetRegistries ()) {
				if (Path.GetFullPath (node.RegistryPath) == Path.GetFullPath (regPath))
					return node.ApplicationName;
			}
			return regPath;
		}
		
		public static IEnumerable<RegistryInfo> GetRegistries ()
		{
			foreach (RegistryInfo node in AddinManager.GetExtensionNodes ("MonoDevelop/AddinAuthoring/AddinRegistries"))
				yield return node;
			foreach (RegistryInfo node in config.Registries)
				yield return node;
			foreach (Application app in SetupService.GetExtensibleApplications ())
				yield return new RegistryInfo (app);
		}
		
		public static void AddCustomRegistry (RegistryInfo reg)
		{
			config.Registries.Add (reg);
			SaveConfig ();
		}
		
		public static void RemoveCustomRegistry (RegistryInfo reg)
		{
			config.Registries.Remove (reg);
			SaveConfig ();
		}
		
		internal static string NormalizeUserPath (string path)
		{
			if (path.StartsWith ("~")) {
				string absRegistryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				return Path.Combine (absRegistryPath, path.Substring (2));
			}
			else
				return path;
		}
		
		internal static string NormalizeRegistryPath (string path)
		{
			FilePath fp = Path.GetFullPath (path);
			foreach (Environment.SpecialFolder sf in Enum.GetValues (typeof(Environment.SpecialFolder))) {
				FilePath folderPath = Environment.GetFolderPath (sf);
				if (folderPath.IsNullOrEmpty)
					continue;
				if (fp.IsChildPathOf (folderPath))
					return "[" + sf.ToString () + "]" + Path.DirectorySeparatorChar + fp.ToRelative (folderPath);
			}
			return fp;
		}
		
		internal static void AddReferences (AddinData data, object[] addins)
		{
			AddinDescription desc = data.CachedAddinManifest;
			foreach (Addin ad in addins)
				AddReference (desc, ad);
			
			data.SaveAddinManifest ();
			data.NotifyChanged (false);
		}
		
		internal static void RemoveReferences (AddinData data, string[] fullIds)
		{
			AddinDescription desc = data.CachedAddinManifest;
			foreach (string ad in fullIds)
				RemoveReference (desc, ad);
			
			data.SaveAddinManifest ();
			data.NotifyChanged (false);
		}
		
		static void AddReference (AddinDescription desc, Addin addin)
		{
			foreach (AddinDependency adep in desc.MainModule.Dependencies) {
				if (adep.FullAddinId == addin.Id)
					return;
			}
			if (addin.Namespace == desc.Namespace)
				desc.MainModule.Dependencies.Add (new AddinDependency (addin.LocalId, addin.Version));
			else
				desc.MainModule.Dependencies.Add (new AddinDependency (addin.Id));
		}
				
		static void RemoveReference (AddinDescription desc, string addinId)
		{
			foreach (AddinDependency adep in desc.MainModule.Dependencies) {
				if (adep.FullAddinId == addinId) {
					desc.MainModule.Dependencies.Remove (adep);
					break;
				}
			}
		}
		
		public static AddinData GetAddinData (this DotNetProject p)
		{
			return AddinData.GetAddinData (p);
		}
		
		public static SolutionAddinData GetAddinData (this Solution sol)
		{
			SolutionAddinData data = sol.ExtendedProperties ["MonoDevelop.AddinAuthoring"] as SolutionAddinData;
			if (data == null) {
				data = new SolutionAddinData (sol);
				sol.ExtendedProperties ["MonoDevelop.AddinAuthoring"] = data;
			}
			return data;
		}
		
		public static AddinRegistry GetAddinRegistry (this Solution sol)
		{
			return sol.GetAddinData ().Registry;
		}
		
		public static bool HasAddinRoot (this Solution sol)
		{
			foreach (DotNetProject dnp in sol.GetAllSolutionItems<DotNetProject> ()) {
				AddinData data = AddinData.GetAddinData (dnp);
				if (data != null && data.IsRoot)
					return true;
			}
			return false;
		}
		
		public static string GetAddinApplication (this Solution sol)
		{
			foreach (DotNetProject dnp in sol.GetAllSolutionItems<DotNetProject> ()) {
				AddinData data = AddinData.GetAddinData (dnp);
				if (data != null && data.ApplicationName != null)
					return data.ApplicationName;
			}
			return null;
		}
		
		public static bool IsProjectIncludedByAddin (DotNetProject project, ProjectReference pref)
		{
			// Checks if the provided reference is implicitly included by an add-in reference in the project.
			foreach (ProjectReference p in project.References) {
				if (p.ReferenceType != ReferenceType.Project && p != pref)
					continue;
				DotNetProject rp = project.ParentSolution.FindProjectByName (p.Reference) as DotNetProject;
				if (rp != null && rp.GetAddinData () != null) {
					foreach (ProjectReference aref in rp.References) {
						if (aref.ReferenceType == ReferenceType.Project && aref.Reference == pref.Reference)
							return true;
					}
				}
			}
			return false;
		}
		
		public static void SaveFormatted (PolicyContainer policies, AddinDescription adesc)
		{
			File.WriteAllText (adesc.FileName, SaveFormattedXml (policies, adesc));
		}
		
		public static string SaveFormattedXml (PolicyContainer policies, AddinDescription adesc)
		{
			XmlDocument doc = adesc.SaveToXml ();
			
			TextStylePolicy textPolicy = policies.Get<TextStylePolicy> (DesktopService.GetMimeTypeInheritanceChain ("application/x-addin+xml"));
			XmlFormattingPolicy xmlPolicy = policies.Get<XmlFormattingPolicy> (DesktopService.GetMimeTypeInheritanceChain ("application/x-addin+xml"));
			
			return XmlFormatter.FormatXml (textPolicy, xmlPolicy, doc.OuterXml);
		}
		
		public static void NotifyRegistryChanged (AddinRegistry reg)
		{
			if (RegistryChanged != null)
				RegistryChanged (null, new RegistryEventArgs () { Registry = reg });
		}
	}
	
	class AddinAuthoringServiceConfig
	{
		List<RegistryInfo> registries = new List<RegistryInfo> ();
		
		[ItemProperty]
		public List<RegistryInfo> Registries {
			get { return registries; }
		}
	}
	
	[Extension]
	public class MonoAddinsMSBuildImportProvider: IMSBuildImportProvider
	{
		const string import = @"$(MSBuildExtensionsPath)\Mono.Addins.targets";
		
		public void UpdateImports (SolutionEntityItem item, List<string> imports)
		{
			DotNetProject project = item as DotNetProject;
			if (project != null && project.GetAddinData () != null)
				imports.Add (import);
			else
				imports.Remove (import);
		}
	}
}
