// Copyright (c) 2010-2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace ICSharpCode.Decompiler.CSharp.Resolver
{
	/// <summary>
	/// Arguments for the callback of <see cref="FindReferences.RenameReferencesInFile"/>.
	/// </summary>
	public class RenameCallbackArguments
	{
		public AstNode NodeToReplace { get; private set; }
		public AstNode NewNode { get; private set; }
		
		public RenameCallbackArguments(AstNode nodeToReplace, AstNode newNode)
		{
			if (nodeToReplace == null)
				throw new ArgumentNullException("nodeToReplace");
			if (newNode == null)
				throw new ArgumentNullException("newNode");
			this.NodeToReplace = nodeToReplace;
			this.NewNode = newNode;
		}
	}
}
