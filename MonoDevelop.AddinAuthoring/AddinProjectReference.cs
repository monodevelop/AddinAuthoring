// AddinProjectReference.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AddinAuthoring
{
	[DataItem ("AddinReference")]
	public class AddinProjectReference: ProjectReference 
	{
		[ItemProperty ("Include")]
		internal string Include {
			get { return Reference; }
			set { InitCustomReference (value); }
		}
		
		public AddinProjectReference ()
		{
			LocalCopy = false;
		}
		
		public AddinProjectReference (string addinId): base (ReferenceType.Custom, EncodeId (addinId))
		{
			LocalCopy = false;
		}
		
		public string AddinId {
			get { return DecodeId (Reference); }
		}
		
		static string EncodeId (string addinId)
		{
			int i = addinId.LastIndexOf (',');
			if (i == -1)
				throw new ArgumentException ("Invalid add-in id");
			return addinId.Substring (0, i) + ":" + addinId.Substring (i+1);
		}
		
		static string DecodeId (string reference)
		{
			int i = reference.LastIndexOf (':');
			if (i == -1)
				throw new ArgumentException ("Invalid add-in reference");
			return reference.Substring (0, i) + "," + reference.Substring (i+1);
		}
		
		public override string[] GetReferencedFileNames (ConfigurationSelector configuration)
		{
			if (OwnerProject != null) {
				string aid = AddinId;
				AddinData data = AddinData.GetAddinData ((DotNetProject)OwnerProject);
				if (data != null) {
					Addin addin = data.AddinRegistry.GetAddin (aid);
					if (addin != null) {
						List<string> list = new List<string> ();
						foreach (string asm in addin.Description.MainModule.Assemblies) {
							string afile = Path.Combine (Path.GetDirectoryName (addin.Description.AddinFile), asm);
							list.Add (afile);
						}
						return list.ToArray ();
					}
				}
			}
			return new string [0];
		}
	}
}
