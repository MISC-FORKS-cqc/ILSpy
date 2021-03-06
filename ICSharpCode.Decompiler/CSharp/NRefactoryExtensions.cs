// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Syntax.PatternMatching;
using ICSharpCode.Decompiler.IL;

namespace ICSharpCode.Decompiler.CSharp
{
	static class NRefactoryExtensions
	{
		[Obsolete("Use the more concrete With...()-methods in AnnotationExtensions, so that we maintain a centralised list of the possible annotation types")]
		public static T WithAnnotation<T>(this T node, object annotation) where T : AstNode
		{
			if (annotation != null)
				node.AddAnnotation(annotation);
			return node;
		}

		public static T CopyAnnotationsFrom<T>(this T node, AstNode other) where T : AstNode
		{
			foreach (object annotation in other.Annotations) {
				node.AddAnnotation(annotation);
			}
			return node;
		}

		public static T CopyInstructionsFrom<T>(this T node, AstNode other) where T : AstNode
		{
			foreach (object annotation in other.Annotations.OfType<ILInstruction>()) {
				node.AddAnnotation(annotation);
			}
			return node;
		}

		public static T Detach<T>(this T node) where T : AstNode
		{
			node.Remove();
			return node;
		}
		
		public static Expression WithName(this Expression node, string patternGroupName)
		{
			return new NamedNode(patternGroupName, node);
		}
		
		public static Statement WithName(this Statement node, string patternGroupName)
		{
			return new NamedNode(patternGroupName, node);
		}
		
		public static void AddNamedArgument(this Syntax.Attribute attribute, string name, Expression argument)
		{
			attribute.Arguments.Add(new AssignmentExpression(new IdentifierExpression(name), argument));
		}
		
		public static AstType ToType(this Pattern pattern)
		{
			return pattern;
		}
		
		public static Expression ToExpression(this Pattern pattern)
		{
			return pattern;
		}
		
		public static Statement ToStatement(this Pattern pattern)
		{
			return pattern;
		}
		
		public static Statement GetNextStatement(this Statement statement)
		{
			AstNode next = statement.NextSibling;
			while (next != null && !(next is Statement))
				next = next.NextSibling;
			return (Statement)next;
		}
		
		public static bool IsArgList(this AstType type)
		{
			var simpleType = type as SimpleType;
			return simpleType != null && simpleType.Identifier == "__arglist";
		}

		/// <summary>
		/// Returns true if <paramref name="operatorType"/> is bitwise and, bitwise or, or exclusive or.
		/// </summary>
		public static bool IsBitwise(this BinaryOperatorType operatorType)
		{
			return operatorType == BinaryOperatorType.BitwiseAnd
				|| operatorType == BinaryOperatorType.BitwiseOr
				|| operatorType == BinaryOperatorType.ExclusiveOr;
		}
	}
}
