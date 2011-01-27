// RegistryExtensionNode.cs
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
using Mono.Addins;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AddinAuthoring
{
	[DataItem("AddinRegistry")]
	public class RegistryInfo : ExtensionNode
	{
		[ItemProperty()]
		[NodeAttribute("name", Required = true)]
		string name;

		[ItemProperty()]
		[NodeAttribute("appPath", Required = true)]
		string appPath;

		[ItemProperty()]
		[NodeAttribute("regPath", Required = true)]
		string regPath;

		[ItemProperty()]
		[NodeAttribute("description")]
		string description;

		[ItemProperty()]
		[NodeAttribute("testCommand")]
		string testCommand;
		
		internal AddinRegistry CachedRegistry { get; set; }

		public RegistryInfo ()
		{
		}

		public RegistryInfo (Mono.Addins.Setup.Application app)
		{
			name = app.Name;
			description = app.Description;
			regPath = app.Registry.RegistryPath;
			appPath = app.StartupPath;
			testCommand = app.TestCommand;
		}

		public string ApplicationName {
			get { return name; }
			set { name = value; }
		}

		public string Description {
			get { return description; }
			set { description = value; }
		}

		public string ApplicationPath {
			get { return appPath; }
			set { appPath = !string.IsNullOrEmpty (value) ? AddinAuthoringService.NormalizeUserPath (value) : null; }
		}

		public string RegistryPath {
			get { return regPath; }
			set { regPath = !string.IsNullOrEmpty (value) ? AddinAuthoringService.NormalizeRegistryPath (value) : null; }
		}

		public string TestCommand {
			get { return testCommand; }
			set { testCommand = value; }
		}
	}
}
