// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB.Tests.Ast
{
	[TestFixture]
	public class BaseReferenceExpressionTests
	{
		#region VB.NET
		[Test]
		public void VBNetBaseReferenceExpressionTest1()
		{
			MemberReferenceExpression fre = ParseUtil.ParseExpression<MemberReferenceExpression>("MyBase.myField");
			Assert.IsTrue(fre.TargetObject is BaseReferenceExpression);
		}
		#endregion
	}
}
