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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	public class TextTokenWriter : TokenWriter
	{
		readonly ITextOutput output;
		readonly DecompilerSettings settings;
		readonly IDecompilerTypeSystem typeSystem;
		readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
		int braceLevelWithinType = -1;
		bool inDocumentationComment = false;
		bool firstUsingDeclaration;
		bool lastUsingDeclaration;
		
		public bool FoldBraces = false;
		
		public TextTokenWriter(ITextOutput output, DecompilerSettings settings, IDecompilerTypeSystem typeSystem)
		{
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			if (typeSystem == null)
				throw new ArgumentNullException(nameof(typeSystem));
			this.output = output;
			this.settings = settings;
			this.typeSystem = typeSystem;
		}
		
		public override void WriteIdentifier(Identifier identifier)
		{
			if (identifier.IsVerbatim || CSharpOutputVisitor.IsKeyword(identifier.Name, identifier)) {
				output.Write('@');
			}
			
			var definition = GetCurrentDefinition();
			if (definition != null) {
				MemberReference cecil = SymbolToCecil(definition);
				if (cecil != null) {
					output.WriteDefinition(identifier.Name, definition, false);
					return;
				}
			}
			
			var member = GetCurrentMemberReference();
			if (member != null) {
				MemberReference cecil = SymbolToCecil(member);
				if (cecil != null) {
					output.WriteReference(identifier.Name, cecil);
					return;
				}
			}

			var localDefinition = GetCurrentLocalDefinition();
			if (localDefinition != null) {
				output.WriteDefinition(identifier.Name, localDefinition);
				return;
			}

			var localRef = GetCurrentLocalReference();
			if (localRef != null) {
				output.WriteReference(identifier.Name, localRef, true);
				return;
			}

			if (firstUsingDeclaration) {
				output.MarkFoldStart(defaultCollapsed: true);
				firstUsingDeclaration = false;
			}

			output.Write(identifier.Name);
		}

		MemberReference SymbolToCecil(ISymbol symbol)
		{
			if (symbol is IType type) {
				return typeSystem.GetCecil(type.GetDefinition());
			} else if (symbol is IMember member) {
				return typeSystem.GetCecil(member);
			} else {
				return null;
			}
		}

		ISymbol GetCurrentMemberReference()
		{
			AstNode node = nodeStack.Peek();
			var symbol = node.GetSymbol();
			if (symbol == null && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression) {
				symbol = node.Parent.GetSymbol();
			}
			if (symbol != null && node.Role == Roles.Type && node.Parent is ObjectCreateExpression) {
				symbol = node.Parent.GetSymbol();
			}
			if (node is IdentifierExpression && node.Role == Roles.TargetExpression && node.Parent is InvocationExpression && symbol is IMember member) {
				var declaringType = member.DeclaringType;
				if (declaringType != null && declaringType.Kind == TypeKind.Delegate)
					return null;
			}
			return FilterMember(symbol);
		}

		ISymbol FilterMember(ISymbol symbol)
		{
			if (symbol == null)
				return null;

			//if (settings.AutomaticEvents && member is FieldDefinition) {
			//	var field = (FieldDefinition)member;
			//	return field.DeclaringType.Events.FirstOrDefault(ev => ev.Name == field.Name) ?? member;
			//}

			return symbol;
		}

		object GetCurrentLocalReference()
		{
			AstNode node = nodeStack.Peek();
			ILVariable variable = node.Annotation<ILVariableResolveResult>()?.Variable;
			if (variable != null)
				return variable;

			var gotoStatement = node as GotoStatement;
			if (gotoStatement != null)
			{
				var method = nodeStack.Select(nd => nd.GetSymbol() as IMethod).FirstOrDefault(mr => mr != null);
				if (method != null)
					return method + gotoStatement.Label;
			}

			return null;
		}

		object GetCurrentLocalDefinition()
		{
			AstNode node = nodeStack.Peek();
			if (node is Identifier && node.Parent != null)
				node = node.Parent;

			if (node is ParameterDeclaration || node is VariableInitializer || node is CatchClause || node is ForeachStatement) {
				var variable = node.Annotation<ILVariableResolveResult>()?.Variable;
				if (variable != null)
					return variable;
			}

			var label = node as LabelStatement;
			if (label != null) {
				var method = nodeStack.Select(nd => nd.GetSymbol() as IMethod).FirstOrDefault(mr => mr != null);
				if (method != null)
					return method + label.Label;
			}

			return null;
		}
		
		ISymbol GetCurrentDefinition()
		{
			if (nodeStack == null || nodeStack.Count == 0)
				return null;
			
			var node = nodeStack.Peek();
			if (node is Identifier)
				node = node.Parent;
			if (IsDefinition(ref node))
				return node.GetSymbol();
			
			return null;
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			//To make reference for 'this' and 'base' keywords in the ClassName():this() expression
			if (role == ConstructorInitializer.ThisKeywordRole || role == ConstructorInitializer.BaseKeywordRole) {
				if (nodeStack.Peek() is ConstructorInitializer initializer && initializer.GetSymbol() is IMember member) {
					var cecil = typeSystem.GetCecil(member);
					if (cecil != null) {
						output.WriteReference(keyword, cecil);
						return;
					}
				}
			}
			output.Write(keyword);
		}
		
		public override void WriteToken(Role role, string token)
		{
			switch (token) {
				case "{":
					if (braceLevelWithinType >= 0 || nodeStack.Peek() is TypeDeclaration)
						braceLevelWithinType++;
					if (nodeStack.OfType<BlockStatement>().Count() <= 1 || FoldBraces) {
						output.MarkFoldStart(defaultCollapsed: braceLevelWithinType == 1);
					}
					output.Write("{");
					break;
				case "}":
					output.Write('}');
					if (nodeStack.OfType<BlockStatement>().Count() <= 1 || FoldBraces)
						output.MarkFoldEnd();
					if (braceLevelWithinType >= 0)
						braceLevelWithinType--;
					break;
				default:
					// Attach member reference to token only if there's no identifier in the current node.
					var member = SymbolToCecil(GetCurrentMemberReference());
					var node = nodeStack.Peek();
					if (member != null && node.GetChildByRole(Roles.Identifier).IsNull)
						output.WriteReference(token, member);
					else
						output.Write(token);
					break;
			}
		}
		
		public override void Space()
		{
			output.Write(' ');
		}
		
		public override void Indent()
		{
			output.Indent();
		}
		
		public override void Unindent()
		{
			output.Unindent();
		}
		
		public override void NewLine()
		{
			if (lastUsingDeclaration) {
				output.MarkFoldEnd();
				lastUsingDeclaration = false;
			}
//			lastEndOfLine = output.Location;
			output.WriteLine();
		}
		
		public override void WriteComment(CommentType commentType, string content)
		{
			switch (commentType) {
				case CommentType.SingleLine:
					output.Write("//");
					output.WriteLine(content);
					break;
				case CommentType.MultiLine:
					output.Write("/*");
					output.Write(content);
					output.Write("*/");
					break;
				case CommentType.Documentation:
					bool isLastLine = !(nodeStack.Peek().NextSibling is Comment);
					if (!inDocumentationComment && !isLastLine) {
						inDocumentationComment = true;
						output.MarkFoldStart("///" + content, true);
					}
					output.Write("///");
					output.Write(content);
					if (inDocumentationComment && isLastLine) {
						inDocumentationComment = false;
						output.MarkFoldEnd();
					}
					output.WriteLine();
					break;
				default:
					output.Write(content);
					break;
			}
		}
		
		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			// pre-processor directive must start on its own line
			output.Write('#');
			output.Write(type.ToString().ToLowerInvariant());
			if (!string.IsNullOrEmpty(argument)) {
				output.Write(' ');
				output.Write(argument);
			}
			output.WriteLine();
		}
		
		public override void WritePrimitiveValue(object value, string literalValue = null)
		{
			new TextWriterTokenWriter(new TextOutputWriter(output)).WritePrimitiveValue(value, literalValue);
		}
		
		public override void WritePrimitiveType(string type)
		{
			switch (type) {
				case "new":
					output.Write(type);
					output.Write("()");
					break;
				case "bool":
				case "byte":
				case "sbyte":
				case "short":
				case "ushort":
				case "int":
				case "uint":
				case "long":
				case "ulong":
				case "float":
				case "double":
				case "decimal":
				case "char":
				case "string":
				case "object":
					var node = nodeStack.Peek();
					ISymbol symbol;
					if (node.Role == Roles.Type && node.Parent is ObjectCreateExpression) {
						symbol = node.Parent.GetSymbol();
					} else {
						symbol = nodeStack.Peek().GetSymbol();
					}
					if (symbol == null) goto default;
					output.WriteReference(type, SymbolToCecil(symbol));
					break;
				default:
					output.Write(type);
					break;
			}
		}
		
//		Stack<TextLocation> startLocations = new Stack<TextLocation>();
//		Stack<MethodDebugSymbols> symbolsStack = new Stack<MethodDebugSymbols>();
		
		public override void StartNode(AstNode node)
		{
			if (nodeStack.Count == 0) {
				if (IsUsingDeclaration(node)) {
					firstUsingDeclaration = !IsUsingDeclaration(node.PrevSibling);
					lastUsingDeclaration = !IsUsingDeclaration(node.NextSibling);
				} else {
					firstUsingDeclaration = false;
					lastUsingDeclaration = false;
				}
			}
			nodeStack.Push(node);
//			startLocations.Push(output.Location);
			
//			if (node is EntityDeclaration && node.GetSymbol() != null && node.GetChildByRole(Roles.Identifier).IsNull)
//				output.WriteDefinition("", node.GetSymbol(), false);

//			if (node.Annotation<MethodDebugSymbols>() != null) {
//				symbolsStack.Push(node.Annotation<MethodDebugSymbols>());
//				symbolsStack.Peek().StartLocation = startLocations.Peek();
//			}
		}
		
		private bool IsUsingDeclaration(AstNode node)
		{
			return node is UsingDeclaration || node is UsingAliasDeclaration;
		}

		public override void EndNode(AstNode node)
		{
			if (nodeStack.Pop() != node)
				throw new InvalidOperationException();
			
//			var startLocation = startLocations.Pop();
//			
//			// code mappings
//			var ranges = node.Annotation<List<ILRange>>();
//			if (symbolsStack.Count > 0 && ranges != null && ranges.Count > 0) {
//				// Ignore the newline which was printed at the end of the statement
//				TextLocation endLocation = (node is Statement) ? (lastEndOfLine ?? output.Location) : output.Location;
//				symbolsStack.Peek().SequencePoints.Add(
//					new SequencePoint() {
//						ILRanges = ILRange.OrderAndJoin(ranges).ToArray(),
//						StartLocation = startLocation,
//						EndLocation = endLocation
//					});
//			}
//			
//			if (node.Annotation<MethodDebugSymbols>() != null) {
//				symbolsStack.Peek().EndLocation = output.Location;
//				output.AddDebugSymbols(symbolsStack.Pop());
//			}
		}
		
		static bool IsDefinition(ref AstNode node)
		{
			if (node is EntityDeclaration)
				return true;
			if (node is VariableInitializer && node.Parent is FieldDeclaration) {
				node = node.Parent;
				return true;
			}
			if (node is FixedVariableInitializer)
				return true;
			return false;
		}
	}
}
